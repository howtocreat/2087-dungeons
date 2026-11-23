using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Dungeons2087
{
    [RequireComponent(typeof(DungeonHud), typeof(AdventureCombat), typeof(AudioSource))]
    [RequireComponent(typeof(DungeonSessionMenu))]
    [DefaultExecutionOrder(-60)]
    public sealed partial class DungeonRuntime : MonoBehaviour
    {
        private const float Speed = 5.2f;
        private const float DashTime = 0.17f;
        private const float DashRecovery = 0.75f;
        private static readonly Vector3 ViewOffset = new(0f, 18f, -10.7f);
        private static readonly Plane Floor = new(Vector3.up, Vector3.zero);

        [SerializeField] private Camera dungeonCamera;
        [SerializeField] private AdventureRoom[] rooms;
        [SerializeField] private Transform hero;

        private DungeonGame game;
        private DungeonHud hud;
        private AdventureCombat combat;
        private DungeonSound sound;
        private CharacterController controller;
        private Transform rig;
        private Transform leftLeg;
        private Transform rightLeg;
        private Vector3 restRigPosition;
        private Quaternion restRigRotation;
        private Quaternion restLeftLeg;
        private Quaternion restRightLeg;
        private AdventureRoom currentRoom;
        private Vector3 lookAt;
        private Vector3 dashDirection;
        private float dashTime;
        private float dashRecovery;
        private float interactRecovery;
        private float walkCycle;
        private float motionAmount;
        private float recoil;
        private int lastEffectSerial;
        private bool inEncounter;

        public void Configure(Camera camera, AdventureRoom[] adventureRooms, Transform player)
        {
            dungeonCamera = camera;
            rooms = adventureRooms;
            hero = player;
        }

        private void Start()
        {
            Application.targetFrameRate = 60;
            game = new DungeonGame();
            hud = GetComponent<DungeonHud>();
            combat = GetComponent<AdventureCombat>();
            sound = new DungeonSound(GetComponent<AudioSource>());
            controller = hero.GetComponent<CharacterController>();
            rig = hero.Find("Rig");
            if (rig != null)
            {
                restRigPosition = rig.localPosition;
                restRigRotation = rig.localRotation;
                leftLeg = rig.Find("LeftLeg");
                rightLeg = rig.Find("RightLeg");
                if (leftLeg != null) restLeftLeg = leftLeg.localRotation;
                if (rightLeg != null) restRightLeg = rightLeg.localRotation;
            }

            dungeonCamera.orthographic = true;
            dungeonCamera.orthographicSize = 6.65f;
            hud.Initialize(game);
            game.Changed += React;
            ShowArea();
            InitializeSaves();
        }

        private void Update()
        {
            if (game == null) return;
            if (HandleSessionInput(Input.GetKeyDown(KeyCode.Escape))) return;

            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            game.Tick(Time.unscaledDeltaTime);
            ShowArea();
            bool encounter = game.IsRealTimeCombat;
            if (inEncounter != encounter)
            {
                inEncounter = encounter;
                if (encounter)
                {
                    combat.Begin(hero, currentRoom.EnemySpawns, game);
                }
                else
                {
                    combat.Stop();
                }
            }

            bool blocked = hud.BlocksMovement;
            interactRecovery = Mathf.Max(0f, interactRecovery - dt);
            Vector3 aim = AimAtFloor();
            MoveHero(dt, aim, blocked, ReadMotion(), Input.GetKeyDown(KeyCode.Space));

            AdventureSpot nearby = !blocked ? SelectSpot() : null;
            if (nearby != null)
            {
                bool keyPressed = Input.GetKeyDown(KeyCode.E);
                bool heldToMine = nearby.ActionId == "mine" && Input.GetKey(KeyCode.E);
                if ((keyPressed || heldToMine) && interactRecovery <= 0f)
                {
                    if (UseSpot(nearby))
                    {
                        nearby = null;
                    }
                }
            }

            if (inEncounter && game.IsRealTimeCombat)
            {
                bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
                combat.Step(dt, aim, !blocked && !overUi && Input.GetMouseButton(0), dashTime > 0f);
                ShowArea();
            }

            TrackCamera(dt);
            hud.PresentWorld(dungeonCamera, hero.position, nearby, currentRoom.Spots ?? Array.Empty<AdventureSpot>());
            hud.PresentCombat(combat.Wave, combat.MaxWaves, combat.Enemies, 1f - dashRecovery / DashRecovery);
            TickAutosave();
        }

        private bool ShowArea()
        {
            Area mapArea = game.Area switch
            {
                Area.Robot or Area.Bank or Area.Armory or Area.Food => Area.Market,
                Area.Dead => currentRoom != null ? currentRoom.Area : Area.City,
                _ => game.Area
            };
            if (currentRoom != null && currentRoom.Area == mapArea) return false;

            AdventureRoom next = Array.Find(rooms, room => room.Area == mapArea);
            if (next == null && mapArea == Area.Title)
            {
                next = Array.Find(rooms, room => room.Area == Area.City);
                if (currentRoom == next) return false;
            }
            if (next == null) return false;

            Vector3 spawn = Entrance(next, currentRoom);
            foreach (AdventureRoom room in rooms)
            {
                room.gameObject.SetActive(room == next);
            }
            currentRoom = next;
            controller.enabled = false;
            hero.SetPositionAndRotation(spawn, Quaternion.identity);
            controller.enabled = true;
            dashTime = 0f;
            dashRecovery = 0f;
            lookAt = CameraFocus();
            ApplyCamera();
            RenderSettings.ambientLight = next.Ambient;
            RenderSettings.fogColor = next.Fog;
            dungeonCamera.backgroundColor = next.Fog;
            return true;
        }

        private static Vector3 Entrance(AdventureRoom next, AdventureRoom from)
        {
            Vector3 spawn = next.Spawn.position;
            if (from == null || next.Area != Area.City || next.Spots == null) return spawn;

            string doorway = from.Area switch
            {
                Area.Mine => "mine-area",
                Area.Market => "market",
                Area.Combat => "combat",
                _ => ""
            };
            AdventureSpot portal = Array.Find(next.Spots, spot => spot.ActionId == doorway);
            if (portal == null) return spawn;

            Vector3 intoRoom = spawn - portal.transform.position;
            intoRoom.y = 0f;
            Vector3 entrance = portal.transform.position + intoRoom.normalized * (portal.Radius + 0.22f);
            entrance.y = spawn.y;
            return entrance;
        }

        private Vector3 AimAtFloor()
        {
            Ray ray = dungeonCamera.ScreenPointToRay(Input.mousePosition);
            return Floor.Raycast(ray, out float distance) ? ray.GetPoint(distance) : hero.position + hero.forward * 4f;
        }

        private bool UseSpot(AdventureSpot nearby)
        {
            string action = nearby.ActionId;
            interactRecovery = action == "mine" ? 0.34f : 0.13f;
            DungeonSnapshot arenaEntrance = action == "combat" && activeSave != null ? CaptureCurrentSave() : null;
            game.Execute(action);
            if (game.IsRealTimeCombat && arenaEntrance != null)
            {
                combatCheckpoint = arenaEntrance;
                WriteActiveSave(showFeedback: false);
            }
            return ShowArea() || hud.BlocksMovement;
        }

        private void MoveHero(float dt, Vector3 aim, bool blocked, Vector3 input, bool dashPressed)
        {
            Vector3 motion = blocked ? Vector3.zero : input;
            dashRecovery = Mathf.Max(0f, dashRecovery - dt);
            dashTime = Mathf.Max(0f, dashTime - dt);
            if (blocked) dashTime = 0f;
            if (!blocked && dashPressed && dashRecovery <= 0f)
            {
                dashDirection = motion.sqrMagnitude > 0f ? motion : hero.forward;
                dashTime = DashTime;
                dashRecovery = DashRecovery;
                sound.Play("dodge");
            }

            bool dashing = dashTime > 0f;
            if (dashing) motion = dashDirection;
            Vector3 facing = game.IsRealTimeCombat && !dashing ? aim - hero.position : motion;
            facing.y = 0f;
            if (!blocked && facing.sqrMagnitude > 0.01f)
            {
                hero.rotation = Quaternion.RotateTowards(hero.rotation, Quaternion.LookRotation(facing), 1080f * dt);
            }

            Vector3 before = hero.position;
            if (!blocked)
            {
                controller.Move((motion * (dashing ? 14.4f : Speed) + Vector3.down * 1.2f) * dt);
            }
            if (rig == null) return;

            float moved = (hero.position - before).magnitude;
            bool walking = moved > 0.005f && motion.sqrMagnitude > 0f;
            motionAmount = Mathf.MoveTowards(motionAmount, walking ? 1f : 0f, dt * 10f);
            walkCycle += dt * (dashing ? 27f : 12f);
            recoil = Mathf.MoveTowards(recoil, 0f, dt * 5.1f);
            rig.localPosition = restRigPosition + Vector3.up * (Mathf.Abs(Mathf.Sin(walkCycle)) * 0.10f * motionAmount);
            rig.localRotation = restRigRotation * Quaternion.Euler(
                (dashing ? -18f : -6f * motionAmount) + Mathf.Sin(recoil * Mathf.PI) * 18f,
                0f, Mathf.Sin(walkCycle) * 4.5f * motionAmount);
            float stride = Mathf.Sin(walkCycle) * (dashing ? 44f : 26f) * motionAmount;
            if (leftLeg != null) leftLeg.localRotation = restLeftLeg * Quaternion.Euler(stride, 0f, 0f);
            if (rightLeg != null) rightLeg.localRotation = restRightLeg * Quaternion.Euler(-stride, 0f, 0f);
        }

        private AdventureSpot SelectSpot()
        {
            AdventureSpot best = null;
            float bestScore = float.PositiveInfinity;
            if (currentRoom.Spots == null) return null;

            foreach (AdventureSpot spot in currentRoom.Spots)
            {
                Vector3 offset = spot.transform.position - hero.position;
                offset.y = 0f;
                float distanceSquared = offset.sqrMagnitude;
                if (distanceSquared > spot.Radius * spot.Radius || distanceSquared >= bestScore || !HasAction(spot.ActionId))
                    continue;

                best = spot;
                bestScore = distanceSquared;
            }
            return best;
        }

        private bool HasAction(string actionId)
        {
            foreach (GameAction action in game.Actions)
            {
                if (action.Id == actionId) return true;
            }
            return false;
        }

        private void React()
        {
            QueueAutosave();
            if (lastEffectSerial == game.EffectSerial) return;

            lastEffectSerial = game.EffectSerial;
            string cue = game.LastEffect;
            if (!cue.StartsWith("mine-", StringComparison.Ordinal) || game.Area == Area.Mine)
                sound.Play(cue);
            if (cue.StartsWith("mine-", StringComparison.Ordinal) && !game.IsMining || cue == "hit")
                recoil = 1f;
        }

        private static bool Down(KeyCode first, KeyCode second) => Input.GetKey(first) || Input.GetKey(second);

        private static Vector3 ReadMotion()
        {
            float horizontal = (Down(KeyCode.D, KeyCode.RightArrow) ? 1f : 0f)
                - (Down(KeyCode.A, KeyCode.LeftArrow) ? 1f : 0f);
            float vertical = (Down(KeyCode.W, KeyCode.UpArrow) ? 1f : 0f)
                - (Down(KeyCode.S, KeyCode.DownArrow) ? 1f : 0f);
            return new Vector3(horizontal, 0f, vertical).normalized;
        }

        private Vector3 CameraFocus()
        {
            Vector3 position = hero.position;
            if (game.Area == Area.Title)
                return currentRoom.Spawn.position + Vector3.forward * 1.8f;

            float visibleHalfWidth = dungeonCamera.orthographicSize * dungeonCamera.aspect;
            float cameraXLimit = Mathf.Max(0f, currentRoom.FloorSize.x * 0.5f - visibleHalfWidth + 0.95f);
            float cameraZLimit = Mathf.Max(0.8f, currentRoom.FloorSize.y * 0.5f - dungeonCamera.orthographicSize / 0.86f + 0.85f);
            position.x = Mathf.Clamp(position.x * 0.62f, -cameraXLimit, cameraXLimit);
            position.y = 0f;
            position.z = Mathf.Clamp(position.z * 0.66f, -cameraZLimit, cameraZLimit);
            return position;
        }

        private void TrackCamera(float dt)
        {
            lookAt = Vector3.Lerp(lookAt, CameraFocus(), 1f - Mathf.Exp(-dt * 7.5f));
            ApplyCamera();
        }

        private void ApplyCamera()
        {
            dungeonCamera.transform.SetPositionAndRotation(lookAt + ViewOffset, Quaternion.LookRotation(-ViewOffset));
        }

        private void OnDestroy()
        {
            if (game != null) game.Changed -= React;
            sound?.Dispose();
        }
    }
}
