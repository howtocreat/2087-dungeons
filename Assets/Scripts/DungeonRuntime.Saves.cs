using System;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Dungeons2087
{
    public sealed partial class DungeonRuntime
    {
        private const float AutosaveDelay = 1.2f;
        private const float PositionAutosaveInterval = 15f;
        private DungeonSaveStore saveStore;
        private DungeonSessionMenu sessionMenu;
        private DungeonSaveEntry activeSave;
        private DungeonSnapshot combatCheckpoint;
        private bool savePending;
        private float saveDue;
        private float lastSaveTime;
        private AudioSource sessionAudio;

        private bool CanSave => activeSave != null && game.Area is not (Area.Title or Area.Dead);

        private void InitializeSaves()
        {
            saveStore = new DungeonSaveStore(Path.Combine(Application.persistentDataPath, "Saves"));
            sessionAudio = GetComponent<AudioSource>();
            sessionMenu = GetComponent<DungeonSessionMenu>();
            sessionMenu.Initialize();
            sessionMenu.CreateRequested += NewSlot;
            sessionMenu.LoadRequested += LoadSlot;
            sessionMenu.ImportRequested += ImportSlot;
            sessionMenu.SaveRequested += SaveFromMenu;
            sessionMenu.TitleRequested += SaveAndReturn;
            sessionMenu.ResumeRequested += ResumeGame;
            sessionMenu.OpenSavesFolderRequested += OpenSavesFolder;
            hud.MenuRequested += OpenPause;
            ShowFileLibrary();
        }

        private bool HandleSessionInput(bool escapePressed)
        {
            if (escapePressed)
            {
                if (game.Area == Area.Title)
                    sessionMenu.CloseSubpage();
                else if (sessionMenu.IsOpen)
                    ResumeGame();
                else
                    OpenPause();
                return true;
            }

            if (game.Area == Area.Title && !sessionMenu.IsOpen)
                ShowFileLibrary();
            return sessionMenu.IsOpen;
        }

        private void ShowFileLibrary(string message = "")
        {
            activeSave = null;
            combatCheckpoint = null;
            savePending = false;
            inEncounter = false;
            combat.Stop();
            sessionAudio.UnPause();
            if (game.Area != Area.Title) game.ReturnToTitle();
            ShowArea();
            hud.SetSessionOverlay(true);
            try
            {
                sessionMenu.ShowTitle(saveStore.List(), message);
            }
            catch (Exception ex) when (IsFileError(ex))
            {
                sessionMenu.ShowTitle(Array.Empty<DungeonSaveEntry>(), "暂时无法读取存档。请检查存档文件夹权限。");
                Debug.LogWarning($"Read save library: {ex.Message}");
            }
        }

        private void NewSlot(string name)
        {
            try
            {
                game.Execute("new");
                DungeonSaveEntry created = saveStore.Create(name, game.CaptureSnapshot());
                ActivateSave(created);
            }
            catch (Exception ex) when (IsFileError(ex))
            {
                game.ReturnToTitle();
                ReportFileError("新建存档", ex);
            }
        }

        private void LoadSlot(string id)
        {
            try
            {
                ActivateSave(saveStore.Load(id));
            }
            catch (Exception ex) when (IsFileError(ex))
            {
                ReportFileError("读取存档", ex);
            }
        }

        private void ImportSlot(string sourcePath)
        {
            try
            {
                ActivateSave(saveStore.Import(sourcePath));
            }
            catch (Exception ex) when (IsFileError(ex))
            {
                ReportFileError("导入存档", ex);
            }
        }

        private void ActivateSave(DungeonSaveEntry slot)
        {
            game.RestoreSnapshot(slot.Snapshot);
            activeSave = slot;
            combatCheckpoint = null;
            savePending = false;
            lastSaveTime = Time.unscaledTime;
            ShowArea();
            RestorePosition(slot.Snapshot);
            sessionMenu.Hide();
            hud.SetSessionOverlay(false);
            sessionAudio.UnPause();
            interactRecovery = 0.2f;
        }

        private void RestorePosition(DungeonSnapshot snapshot)
        {
            if (!snapshot.HasPosition || currentRoom == null) return;

            Vector3 arrival = currentRoom.Spawn.position;
            Vector3 position = new(snapshot.X, arrival.y, snapshot.Z);
            Vector2 half = currentRoom.FloorSize * 0.5f;
            float margin = controller.radius + 0.5f;
            Vector3 relative = position - currentRoom.transform.position;
            if (Mathf.Abs(relative.x) > half.x - margin || Mathf.Abs(relative.z) > half.y - margin) return;

            controller.enabled = false;
            Physics.SyncTransforms();
            Vector3 center = position + controller.center + Vector3.up * 0.07f;
            float straight = Mathf.Max(0f, controller.height / 2f - controller.radius);
            if (!Physics.CheckCapsule(center - Vector3.up * straight, center + Vector3.up * straight,
                controller.radius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                hero.SetPositionAndRotation(position, Quaternion.Euler(0f, snapshot.Yaw, 0f));
                lookAt = CameraFocus();
                ApplyCamera();
            }
            controller.enabled = true;
        }

        private void OpenPause()
        {
            if (game.Area == Area.Title || sessionMenu.IsOpen) return;

            EventSystem.current?.SetSelectedGameObject(null);
            hud.SetSessionOverlay(true);
            sessionAudio.Pause();
            sessionMenu.ShowPause(activeSave?.Name ?? "当前进度", game.IsRealTimeCombat, CanSave);
        }

        private void ResumeGame()
        {
            if (game.Area == Area.Title) return;

            EventSystem.current?.SetSelectedGameObject(null);
            sessionMenu.Hide();
            hud.SetSessionOverlay(false);
            sessionAudio.UnPause();
        }

        private void SaveFromMenu() => WriteActiveSave(showFeedback: true);

        private void SaveAndReturn()
        {
            if (!CanSave || WriteActiveSave(showFeedback: true))
                ShowFileLibrary();
        }

        private DungeonSnapshot CaptureCurrentSave()
        {
            DungeonSnapshot snapshot = game.CaptureSnapshot();
            if (currentRoom != null && currentRoom.Area.ToString() == snapshot.Area)
            {
                snapshot.HasPosition = true;
                snapshot.X = hero.position.x;
                snapshot.Z = hero.position.z;
                snapshot.Yaw = hero.eulerAngles.y;
            }
            return snapshot;
        }

        private void QueueAutosave()
        {
            if (!CanSave || game.IsRealTimeCombat || savePending) return;

            savePending = true;
            saveDue = Time.unscaledTime + AutosaveDelay;
        }

        private void TickAutosave()
        {
            if (!CanSave || game.IsRealTimeCombat) return;

            DungeonSnapshot previous = activeSave.Snapshot;
            bool moved = Time.unscaledTime - lastSaveTime >= PositionAutosaveInterval
                && (previous.HasPosition
                    ? (new Vector2(hero.position.x - previous.X, hero.position.z - previous.Z)).sqrMagnitude >= 2.25f
                    : currentRoom != null);
            if ((savePending && Time.unscaledTime >= saveDue) || moved && !savePending)
                WriteActiveSave(showFeedback: false);
        }

        private bool WriteActiveSave(bool showFeedback)
        {
            if (!CanSave) return false;

            try
            {
                DungeonSnapshot snapshot = game.IsRealTimeCombat ? combatCheckpoint : CaptureCurrentSave();
                if (snapshot == null)
                {
                    if (showFeedback) sessionMenu.SetMessage("战前检查点暂时无法保存。");
                    return false;
                }
                activeSave = saveStore.Save(activeSave.Id, snapshot);
                lastSaveTime = Time.unscaledTime;
                savePending = false;
                if (showFeedback)
                    sessionMenu.SetMessage(game.IsRealTimeCombat ? "已保存战前检查点" : "已保存");
                return true;
            }
            catch (Exception ex) when (IsFileError(ex))
            {
                if (showFeedback) ReportFileError("保存", ex);
                else Debug.LogWarning($"Autosave: {ex.Message}");
                savePending = true;
                saveDue = Time.unscaledTime + 10f;
                return false;
            }
        }

        private void OpenSavesFolder()
        {
            try
            {
                Directory.CreateDirectory(saveStore.Folder);
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(saveStore.Folder) { UseShellExecute = true });
#else
                Application.OpenURL(new Uri(saveStore.Folder).AbsoluteUri);
#endif
            }
            catch (Exception ex) when (IsFileError(ex))
            {
                ReportFileError("打开存档文件夹", ex);
            }
        }

        private static bool IsFileError(Exception ex) => ex is IOException or UnauthorizedAccessException or InvalidDataException
            or ArgumentException or NotSupportedException or System.Security.SecurityException or System.ComponentModel.Win32Exception;

        private void ReportFileError(string operation, Exception error)
        {
            sessionMenu.SetMessage(error is InvalidDataException ? error.Message : $"{operation}失败。请检查文件或文件夹权限。");
            Debug.LogWarning($"{operation}: {error.Message}");
        }

        private void OnApplicationQuit()
        {
            if (game != null && CanSave) WriteActiveSave(showFeedback: false);
        }
    }
}
