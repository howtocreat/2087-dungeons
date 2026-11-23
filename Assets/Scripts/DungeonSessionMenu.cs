using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Dungeons2087
{
    public sealed class DungeonSessionMenu : MonoBehaviour
    {
        private enum View { Closed, Title, NewSave, Import, Pause }

        private static readonly Vector2 Center = new(0.5f, 0.5f);
        private static readonly Vector2 UpperLeft = new(0f, 1f);
        private static readonly Vector2 UpperRight = Vector2.one;
        private static readonly Color Ink = new Color32(13, 22, 32, 251);
        private static readonly Color Panel = new Color32(21, 36, 48, 254);
        private static readonly Color Card = new Color32(30, 53, 65, 255);
        private static readonly Color Rule = new Color32(47, 76, 90, 255);
        private static readonly Color White = new Color32(240, 251, 248, 255);
        private static readonly Color Soft = new Color32(181, 205, 212, 255);
        private static readonly Color Mint = new Color32(108, 238, 203, 255);
        private static readonly Color Amber = new Color32(255, 204, 113, 255);

        private readonly List<Button> slots = new();
        private Font chinese;
        private Font numbers;
        private Sprite rounded;
        private RectTransform canvas;
        private RectTransform title;
        private RectTransform form;
        private RectTransform pause;
        private RectTransform saveList;
        private ScrollRect scroll;
        private Scrollbar scrollbar;
        private Text emptyList;
        private Text titleMessage;
        private Text formCaption;
        private Text formInstructions;
        private Text formPlaceholder;
        private Text formConfirm;
        private Text formMessage;
        private Text pauseSubtitle;
        private Text pauseNote;
        private Text pauseMessage;
        private Text titleButtonText;
        private InputField input;
        private Button browse;
        private Button manualSave;
        private Button returnToTitle;
        private View view;
        private int submittedFrame = -1;

        public bool IsOpen => view != View.Closed;
        public event Action<string> CreateRequested;
        public event Action<string> LoadRequested;
        public event Action<string> ImportRequested;
        public event Action SaveRequested;
        public event Action TitleRequested;
        public event Action ResumeRequested;
        public event Action OpenSavesFolderRequested;

        public void Initialize()
        {
            if (canvas != null) return;

            chinese = FindFont("Microsoft YaHei UI", "Microsoft YaHei", "DengXian", "Noto Sans CJK SC", "SimHei");
            numbers = FindFont("Bahnschrift", "Segoe UI", "Arial");
            rounded = RoundedSprite();
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("Menu input", typeof(EventSystem), typeof(StandaloneInputModule));
                events.transform.SetParent(transform, false);
            }

            var root = new GameObject("Session menu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            canvas = (RectTransform)root.transform;
            Canvas overlay = root.GetComponent<Canvas>();
            overlay.renderMode = RenderMode.ScreenSpaceOverlay;
            overlay.sortingOrder = 40;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            Paint(Stretch(canvas, "Menu shade"), new Color32(5, 12, 20, 166), raycast: true);
            BuildTitle();
            BuildForm();
            BuildPause();
            Hide();
        }

        public void ShowTitle(IReadOnlyList<DungeonSaveEntry> saves, string message = "")
        {
            Initialize();
            Show(View.Title);
            RefreshLibrary(saves);
            titleMessage.text = message ?? "";
        }

        public void ShowPause(string slotName, bool combatCheckpoint, bool canSave, string message = "")
        {
            Initialize();
            Show(View.Pause);
            pauseSubtitle.text = string.IsNullOrWhiteSpace(slotName) ? "" : $"当前存档  ·  {slotName}";
            pauseNote.text = canSave && combatCheckpoint ? "继续游戏回到当前战斗。重新载入将从战前存档开始。" : "";
            manualSave.gameObject.SetActive(canSave);
            ((RectTransform)returnToTitle.transform).anchoredPosition = new Vector2(28, canSave ? -250 : -195);
            titleButtonText.text = canSave ? "保存并返回主界面" : "返回主界面";
            pauseMessage.text = message ?? "";
        }

        public void SetMessage(string message)
        {
            string text = message ?? "";
            switch (view)
            {
                case View.Title: titleMessage.text = text; break;
                case View.NewSave:
                case View.Import: formMessage.text = text; break;
                case View.Pause: pauseMessage.text = text; break;
            }
        }

        public bool CloseSubpage()
        {
            if (view is not (View.NewSave or View.Import)) return false;
            Show(View.Title);
            return true;
        }

        public void Hide()
        {
            if (canvas != null) Show(View.Closed);
        }

        private void Show(View next)
        {
            view = next;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            title.gameObject.SetActive(next == View.Title);
            form.gameObject.SetActive(next is View.NewSave or View.Import);
            pause.gameObject.SetActive(next == View.Pause);
            canvas.gameObject.SetActive(next != View.Closed);
        }

        private void BuildTitle()
        {
            title = Node(canvas, "Save library", Center, Vector2.zero, new Vector2(916, 470), Center);
            Paint(title, Ink, rounded: true);
            Paint(Node(title, "Brand line", UpperLeft, new Vector2(36, -35), new Vector2(42, 3)), Mint);
            Label(Node(title, "Year", UpperLeft, new Vector2(32, -46), new Vector2(292, 89)), "2087", 85, Mint, bold: true, numeric: true);
            Label(Node(title, "Game title", UpperLeft, new Vector2(35, -126), new Vector2(285, 66)), "地下城", 52, White, bold: true);
            Label(Node(title, "Game setting", UpperLeft, new Vector2(36, -196), new Vector2(281, 31)), "在地下避难所活下去", 18, Soft);
            MakeButton(title, "新建存档", new Vector2(35, -262), new Vector2(280, 50), Mint, () => OpenForm(View.NewSave));
            MakeButton(title, "导入存档", new Vector2(35, -323), new Vector2(280, 47), Card, () => OpenForm(View.Import));
            MakeButton(title, "打开存档文件夹", new Vector2(35, -402), new Vector2(161, 34), Panel, () => OpenSavesFolderRequested?.Invoke(), 14);

            Paint(Node(title, "Divider", UpperLeft, new Vector2(343, -33), new Vector2(1, 402)), Rule);
            Label(Node(title, "Library heading", UpperLeft, new Vector2(372, -30), new Vector2(460, 43)), "继续探索", 30, White, bold: true);

            RectTransform listRoot = Node(title, "Scroll list", UpperLeft, new Vector2(372, -89), new Vector2(510, 311));
            Paint(listRoot, Panel, rounded: true, raycast: true);
            RectTransform viewport = Inset(listRoot, "Viewport", 8, 15, 8, 8);
            viewport.gameObject.AddComponent<RectMask2D>();
            saveList = Node(viewport, "Save rows", UpperLeft, Vector2.zero, new Vector2(0, 0));
            saveList.anchorMax = UpperRight;
            scroll = listRoot.gameObject.AddComponent<ScrollRect>();
            scroll.content = saveList;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30;
            AddScrollbar(listRoot);
            emptyList = Label(Node(listRoot, "Empty message", Center, Vector2.zero, new Vector2(442, 83), Center),
                "还没有存档\n新建存档，从医务室开始探索。", 18, Soft, TextAnchor.MiddleCenter);
            titleMessage = Label(Node(title, "Title response", UpperLeft, new Vector2(372, -403), new Vector2(509, 39)), "", 15, Amber);
        }

        private void AddScrollbar(RectTransform parent)
        {
            RectTransform rail = Node(parent, "Scroll track", UpperRight, new Vector2(-4, -9), new Vector2(7, 294), UpperRight);
            Paint(rail, Rule, rounded: true, raycast: true);
            RectTransform range = Stretch(rail, "Scroll travel");
            RectTransform grip = Stretch(range, "Scrollbar handle");
            Image fill = Paint(grip, Mint, rounded: true, raycast: true);
            scrollbar = rail.gameObject.AddComponent<Scrollbar>();
            scrollbar.targetGraphic = fill;
            scrollbar.handleRect = grip;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        }

        private void RefreshLibrary(IReadOnlyList<DungeonSaveEntry> saves)
        {
            foreach (Button row in slots)
            {
                row.gameObject.SetActive(false);
                Destroy(row.gameObject);
            }
            slots.Clear();
            float y = 0f;
            if (saves != null)
            {
                foreach (DungeonSaveEntry save in saves)
                {
                    if (save?.Snapshot == null) continue;
                    AddSlot(save, y);
                    y -= 78f;
                }
            }
            emptyList.gameObject.SetActive(slots.Count == 0);
            saveList.sizeDelta = new Vector2(0f, Mathf.Max(0f, -y - 7f));
            scrollbar.gameObject.SetActive(slots.Count > 3);
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 1f;
        }

        private void AddSlot(DungeonSaveEntry save, float y)
        {
            RectTransform rect = Node(saveList, "Load save", UpperLeft, new Vector2(0, y), new Vector2(0, 71));
            rect.anchorMax = UpperRight;
            string id = save.Id;
            slots.Add(ButtonAt(rect, Card, () => LoadRequested?.Invoke(id)));
            Paint(Node(rect, "Slot accent", UpperLeft, new Vector2(0, -12), new Vector2(3, 47)), Mint);
            RowLabel(rect, "Save name", save.Name, new Vector2(15, -6), new Vector2(299, 29), 20, White, bold: true);
            DungeonSnapshot snapshot = save.Snapshot;
            string detail = $"{AreaName(snapshot.Area)}  ·  枪械 Lv.{snapshot.WeaponLevel}";
            if (snapshot.HasRobot) detail += $"  ·  机器人 Lv.{snapshot.RobotLevel}";
            RowLabel(rect, "Progress", detail, new Vector2(15, -35), new Vector2(274, 27), 14, Soft);
            RowLabel(rect, "Saved time", FormatTime(save.UpdatedUtc), new Vector2(-16, -8), new Vector2(157, 22), 13, Soft, right: true);
            RowLabel(rect, "Wallet", $"银 {snapshot.Silver}   金 {snapshot.Gold}   钻 {snapshot.Diamond}",
                new Vector2(-16, -37), new Vector2(186, 25), 14, Mint, right: true);
        }

        private void BuildForm()
        {
            form = Node(canvas, "Save form", Center, Vector2.zero, new Vector2(540, 278), Center);
            Paint(form, Ink, rounded: true);
            Paint(Node(form, "Heading accent", UpperLeft, new Vector2(27, -22), new Vector2(36, 3)), Mint);
            formCaption = Label(Node(form, "Heading", UpperLeft, new Vector2(26, -32), new Vector2(458, 45)), "", 31, White, bold: true);
            formInstructions = Label(Node(form, "Instruction", UpperLeft, new Vector2(27, -80), new Vector2(488, 34)), "", 16, Soft);

            RectTransform field = Node(form, "Editable field", UpperLeft, new Vector2(27, -123), new Vector2(486, 47));
            Image fieldBackground = Paint(field, Card, rounded: true, raycast: true);
            Text value = Label(Inset(field, "Value", 13, 13, 0, 0), "", 17, White);
            value.horizontalOverflow = HorizontalWrapMode.Overflow;
            value.resizeTextForBestFit = false;
            formPlaceholder = Label(Inset(field, "Placeholder", 13, 13, 0, 0), "", 17, Soft);
            input = field.gameObject.AddComponent<InputField>();
            input.targetGraphic = fieldBackground;
            input.textComponent = value;
            input.placeholder = formPlaceholder;
            input.lineType = InputField.LineType.SingleLine;
            input.caretColor = Mint;
            input.customCaretColor = true;
            input.onSubmit.AddListener(Submit);
            browse = MakeButton(form, "选择文件", new Vector2(408, -123), new Vector2(105, 47), Panel, Browse, 15);

            MakeButton(form, "返回", new Vector2(27, -212), new Vector2(127, 45), Panel, () => CloseSubpage());
            Button confirm = MakeButton(form, "", new Vector2(302, -212), new Vector2(211, 45), Mint, () => Submit(input.text));
            formConfirm = confirm.GetComponentInChildren<Text>();
            formMessage = Label(Node(form, "Form response", UpperLeft, new Vector2(27, -175), new Vector2(486, 30)), "", 15, Amber);
        }

        private void OpenForm(View next)
        {
            bool importing = next == View.Import;
            Show(next);
            formCaption.text = importing ? "导入存档" : "新建存档";
            formInstructions.text = importing ? "选择或粘贴已有 .json 存档文件的路径。" : "新档将从医务室剧情和新手教程开始。";
            formPlaceholder.text = importing ? "存档文件路径" : "存档名称（可留空）";
            formConfirm.text = importing ? "导入存档" : "开始探索";
            formMessage.text = "";
            browse.gameObject.SetActive(importing);
            ((RectTransform)input.transform).sizeDelta = new Vector2(importing ? 371f : 486f, 47f);
            input.characterLimit = importing ? 1024 : 32;
            input.SetTextWithoutNotify("");
            input.Select();
            input.ActivateInputField();
        }

        private void Submit(string value)
        {
            if (submittedFrame == Time.frameCount) return;
            submittedFrame = Time.frameCount;
            string text = value?.Trim() ?? "";
            if (view == View.NewSave)
            {
                CreateRequested?.Invoke(text);
            }
            else if (view == View.Import)
            {
                if (text.Length > 1 && text[0] == '"' && text[^1] == '"') text = text[1..^1];
                if (text.Length == 0)
                {
                    formMessage.text = "请选择要导入的存档文件。";
                    return;
                }
                ImportRequested?.Invoke(text);
            }
        }

        private void BuildPause()
        {
            pause = Node(canvas, "Pause menu", Center, Vector2.zero, new Vector2(458, 363), Center);
            Paint(pause, Ink, rounded: true);
            Paint(Node(pause, "Heading accent", UpperLeft, new Vector2(29, -24), new Vector2(37, 3)), Mint);
            Label(Node(pause, "Paused", UpperLeft, new Vector2(27, -33), new Vector2(400, 54)), "游戏暂停", 37, White, bold: true);
            pauseSubtitle = Label(Node(pause, "Current save", UpperLeft, new Vector2(28, -93), new Vector2(400, 29)), "", 17, Soft);
            MakeButton(pause, "继续游戏", new Vector2(28, -131), new Vector2(402, 51), Mint, () => ResumeRequested?.Invoke());
            manualSave = MakeButton(pause, "保存存档", new Vector2(28, -195), new Vector2(402, 45), Card, () => SaveRequested?.Invoke());
            returnToTitle = MakeButton(pause, "", new Vector2(28, -250), new Vector2(402, 45), Card, () => TitleRequested?.Invoke());
            titleButtonText = returnToTitle.GetComponentInChildren<Text>();
            pauseNote = Label(Node(pause, "Battle checkpoint", UpperLeft, new Vector2(28, -303), new Vector2(402, 25)), "", 14, Soft, TextAnchor.MiddleCenter);
            pauseMessage = Label(Node(pause, "Pause response", UpperLeft, new Vector2(28, -330), new Vector2(402, 25)), "", 15, Mint, TextAnchor.MiddleCenter);
        }

        private void Browse()
        {
            string path = PickJsonFile();
            if (path == null) return;
            input.SetTextWithoutNotify(path);
            formMessage.text = "";
        }

        private static string AreaName(string area) => area switch
        {
            "Infirmary" => "医务室",
            "City" => "主城区",
            "Mine" => "矿区",
            "Market" => "集市",
            _ => "避难所"
        };

        private static string FormatTime(string timestamp) =>
            DateTimeOffset.TryParse(timestamp, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset date)
                ? date.LocalDateTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) : "";

        private Button MakeButton(RectTransform parent, string label, Vector2 position, Vector2 size, Color background, Action onClick, int fontSize = 17)
        {
            RectTransform buttonRect = Node(parent, "Menu action", UpperLeft, position, size);
            Button button = ButtonAt(buttonRect, background, onClick);
            Label(Inset(buttonRect, "Label", 10, 10, 0, 0), label, fontSize, background == Mint ? Ink : White, TextAnchor.MiddleCenter, bold: true);
            return button;
        }

        private Button ButtonAt(RectTransform rect, Color background, Action onClick)
        {
            Image surface = Paint(rect, background, rounded: true, raycast: true);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = surface;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.highlightedColor = new Color(0.84f, 1f, 1f);
            colors.pressedColor = new Color(0.64f, 0.79f, 0.81f);
            colors.fadeDuration = 0.1f;
            button.colors = colors;
            button.onClick.AddListener(() => onClick());
            return button;
        }

        private Text RowLabel(RectTransform parent, string name, string text, Vector2 position, Vector2 size, int fontSize, Color color, bool bold = false, bool right = false)
        {
            Vector2 corner = right ? UpperRight : UpperLeft;
            RectTransform rect = Node(parent, name, corner, position, size, corner);
            return Label(rect, text, fontSize, color, right ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft, bold: bold);
        }

        private Text Label(RectTransform rect, string text, int fontSize, Color color, TextAnchor align = TextAnchor.MiddleLeft, bool bold = false, bool numeric = false)
        {
            Text label = rect.gameObject.AddComponent<Text>();
            label.font = numeric ? numbers : chinese;
            label.fontSize = fontSize;
            label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            label.alignment = align;
            label.color = color;
            label.text = text ?? "";
            label.raycastTarget = false;
            label.supportRichText = false;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = Mathf.Max(11, fontSize - 6);
            label.resizeTextMaxSize = fontSize;
            return label;
        }

        private Image Paint(RectTransform rect, Color color, bool rounded = false, bool raycast = false)
        {
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            if (rounded)
            {
                image.sprite = this.rounded;
                image.type = Image.Type.Sliced;
            }
            return image;
        }

        private static RectTransform Node(RectTransform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Vector2? pivot = null)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot ?? UpperLeft;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static RectTransform Stretch(RectTransform parent, string name) => Inset(parent, name, 0, 0, 0, 0);

        private static RectTransform Inset(RectTransform parent, string name, float left, float right, float top, float bottom)
        {
            RectTransform rect = Node(parent, name, Vector2.zero, Vector2.zero, Vector2.zero, Center);
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            return rect;
        }

        private static Font FindFont(params string[] families)
        {
            var installed = new HashSet<string>(Font.GetOSInstalledFontNames(), StringComparer.OrdinalIgnoreCase);
            foreach (string family in families)
            {
                if (!installed.Contains(family)) continue;
                Font font = Font.CreateDynamicFontFromOSFont(family, 28);
                if (font == null) continue;
                font.hideFlags = HideFlags.DontSave;
                return font;
            }
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private static Sprite RoundedSprite()
        {
            const int size = 24;
            const float radius = 6;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(0f, Mathf.Abs(x + 0.5f - size * 0.5f) - size * 0.5f + radius);
                float dy = Mathf.Max(0f, Mathf.Abs(y + 0.5f - size * 0.5f) - size * 0.5f + radius);
                byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(radius + 0.35f - Mathf.Sqrt(dx * dx + dy * dy)) * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, size, size), Center, 100, 0, SpriteMeshType.FullRect, new Vector4(8, 8, 8, 8));
        }

        private void OnDestroy()
        {
            if (chinese != null && chinese.hideFlags == HideFlags.DontSave) Destroy(chinese);
            if (numbers != null && numbers != chinese && numbers.hideFlags == HideFlags.DontSave) Destroy(numbers);
            if (rounded == null) return;
            Destroy(rounded.texture);
            Destroy(rounded);
        }

        private string PickJsonFile()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            const int capacity = 1024;
            const uint hideReadOnly = 0x00000004;
            const uint keepWorkingDirectory = 0x00000008;
            const uint pathMustExist = 0x00000800;
            const uint fileMustExist = 0x00001000;
            const uint explorerStyle = 0x00080000;
            const uint avoidRecentDocuments = 0x02000000;
            IntPtr filename = Marshal.AllocHGlobal(capacity * sizeof(char));
            IntPtr filter = Marshal.StringToHGlobalUni("地下城存档 (*.json)\0*.json\0\0");
            try
            {
                Marshal.WriteInt16(filename, 0);
                var dialog = new OpenFileName
                {
                    Size = Marshal.SizeOf<OpenFileName>(),
                    Owner = GetActiveWindow(),
                    Filter = filter,
                    FilterIndex = 1,
                    File = filename,
                    MaxFile = capacity,
                    Title = "选择地下城存档",
                    Flags = hideReadOnly | keepWorkingDirectory | pathMustExist | fileMustExist | explorerStyle | avoidRecentDocuments,
                    DefaultExtension = "json"
                };
                if (GetOpenFileName(ref dialog)) return Marshal.PtrToStringUni(filename);
                if (CommDlgExtendedError() != 0) formMessage.text = "可粘贴 .json 文件路径。";
            }
            catch (DllNotFoundException)
            {
                formMessage.text = "可粘贴 .json 文件路径。";
            }
            catch (EntryPointNotFoundException)
            {
                formMessage.text = "可粘贴 .json 文件路径。";
            }
            finally
            {
                Marshal.FreeHGlobal(filter);
                Marshal.FreeHGlobal(filename);
            }
#else
            formMessage.text = "请粘贴 .json 文件路径。";
#endif
            return null;
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct OpenFileName
        {
            public int Size;
            public IntPtr Owner;
            public IntPtr Instance;
            public IntPtr Filter;
            public IntPtr CustomFilter;
            public int MaxCustomFilter;
            public int FilterIndex;
            public IntPtr File;
            public int MaxFile;
            public IntPtr FileTitle;
            public int MaxFileTitle;
            public string InitialDirectory;
            public string Title;
            public uint Flags;
            public ushort FileOffset;
            public ushort FileExtension;
            public string DefaultExtension;
            public IntPtr CustomData;
            public IntPtr Hook;
            public string TemplateName;
            public IntPtr Reserved;
            public int ReservedCount;
            public int ExtendedFlags;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        [DllImport("comdlg32.dll", EntryPoint = "GetOpenFileNameW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetOpenFileName(ref OpenFileName dialog);

        [DllImport("comdlg32.dll")]
        private static extern uint CommDlgExtendedError();
#endif
    }
}
