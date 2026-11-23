using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Dungeons2087
{
    public sealed class DungeonHud : MonoBehaviour
    {
        private static readonly Color Ink = new Color32(13, 22, 32, 249);
        private static readonly Color Panel = new Color32(20, 35, 47, 246);
        private static readonly Color Card = new Color32(31, 51, 62, 255);
        private static readonly Color Line = new Color32(77, 118, 127, 255);
        private static readonly Color White = new Color32(240, 251, 248, 255);
        private static readonly Color Soft = new Color32(173, 198, 204, 255);
        private static readonly Color Mint = new Color32(108, 238, 203, 255);
        private static readonly Color Amber = new Color32(255, 204, 113, 255);
        private static readonly Color Red = new Color32(251, 117, 115, 255);
        private static readonly Color Silver = new Color32(211, 228, 240, 255);
        private static readonly Vector2 Center = new(0.5f, 0.5f);
        private static readonly Vector2 UpperLeft = new(0f, 1f);
        private static readonly Vector2 UpperRight = Vector2.one;
        private static readonly Vector2 LowerCenter = new(0.5f, 0f);
        private static readonly Vector2 UpperCenter = new(0.5f, 1f);

        private DungeonGame game;
        private RectTransform canvasRoot;
        private Font chinese;
        private Font latin;
        private Sprite rounding;
        private RectTransform exploration;
        private Text district;
        private Text silver;
        private Text gold;
        private Text diamond;
        private Meter life;
        private Meter food;
        private RectTransform vitals;
        private Button backpack;
        private Button menu;
        private RectTransform combat;
        private Text wave;
        private Text enemyCount;
        private Meter dodge;
        private Text controls;
        private RectTransform batch;
        private Text batchLabel;
        private Image batchFill;
        private RectTransform objective;
        private Text objectiveText;
        private RectTransform nearPrompt;
        private Text nearLabel;
        private Text nearKey;
        private Image nearEdge;
        private readonly List<Marker> markers = new();
        private RectTransform cursor;
        private RectTransform splash;
        private Text splashDescription;
        private RectTransform dialogue;
        private Text talker;
        private Text line;
        private RectTransform modalShade;
        private RectTransform modal;
        private Text modalTitle;
        private Text modalCaption;
        private Text modalFeedback;
        private Text contractReward;
        private Button closeModal;
        private RectTransform modalActions;
        private RectTransform batchPicker;
        private Text batchCount;
        private Text batchCost;
        private RectTransform inputBox;
        private InputField entry;
        private string currentPrompt = "";
        private readonly List<ActionRow> rows = new();
        private RectTransform toast;
        private CanvasGroup toastFade;
        private Text toastText;
        private int noticeSeen = -1;
        private int noticeAtModalEntry;
        private Area modalArea;
        private bool modalOpen;
        private bool modalBag;
        private bool sessionOverlay;
        private float toastUntil;
        private int submittedFrame = -1;
        private string easterKeys = "";
        private float easterUntil;

        private sealed class Meter
        {
            public Text Value;
            public Image Fill;
        }

        private sealed class Marker
        {
            public RectTransform Root;
            public Image Dot;
            public Text Label;
        }

        private sealed class ActionRow
        {
            public RectTransform Root;
            public Button Button;
            public Text Title;
            public Text Detail;
            public Image Swatch;
            public string Id;
        }

        public bool BlocksMovement => game == null || sessionOverlay || game.Area is Area.Title or Area.Dead || game.HasDialogue || HasModal;
        public event Action MenuRequested;

        private bool HasModal => game.InBag || game.InContracts || game.IsChoosingMining || !string.IsNullOrEmpty(game.InputPrompt)
            || game.Area is Area.Robot or Area.Bank or Area.Armory or Area.Food;

        public void Initialize(DungeonGame dungeon)
        {
            game = dungeon;
            chinese = FindFont("Microsoft YaHei UI", "Microsoft YaHei", "微软雅黑", "DengXian", "Noto Sans CJK SC", "SimHei");
            latin = FindFont("Bahnschrift", "Segoe UI", "Arial");
            rounding = RoundedSprite();
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("Interface events", typeof(EventSystem), typeof(StandaloneInputModule));
                events.transform.SetParent(transform, false);
            }

            var root = new GameObject("2087 HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            canvasRoot = root.GetComponent<RectTransform>();
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            CanvasScaler scale = root.GetComponent<CanvasScaler>();
            scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scale.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scale.referenceResolution = new Vector2(1280, 720);

            BuildExploration();
            BuildSplash();
            BuildDialogue();
            BuildModal();
            game.Changed += Refresh;
            Refresh();
        }

        public void SetSessionOverlay(bool visible)
        {
            sessionOverlay = visible;
            canvasRoot.gameObject.SetActive(!visible);
            if (!visible)
            {
                Refresh();
                if (!string.IsNullOrEmpty(currentPrompt))
                {
                    entry.Select();
                    entry.ActivateInputField();
                }
            }
        }

        private void OnDestroy()
        {
            if (game != null) game.Changed -= Refresh;
            if (chinese != null && chinese.hideFlags == HideFlags.DontSave) Destroy(chinese);
            if (latin != null && latin != chinese && latin.hideFlags == HideFlags.DontSave) Destroy(latin);
            if (rounding != null)
            {
                Destroy(rounding.texture);
                Destroy(rounding);
            }
        }

        public void PresentWorld(Camera camera, Vector3 playerPosition, AdventureSpot near, IReadOnlyList<AdventureSpot> spots)
        {
            if (game == null) return;

            bool visible = !BlocksMovement && camera != null && !game.IsRealTimeCombat;
            int used = 0;
            if (visible && spots != null)
            {
                foreach (AdventureSpot spot in spots)
                {
                    if (spot == null || spot == near) continue;
                    GameAction nearbyAction = Find(spot.ActionId);
                    if (nearbyAction == null) continue;
                    Vector3 anchor = SpotPosition(spot);
                    if (!ScreenPosition(camera, anchor, out Vector2 position)) continue;
                    if (IsHeaderOrFooter(position) || (anchor - playerPosition).sqrMagnitude > 225f) continue;
                    if (used == markers.Count) markers.Add(BuildMarker());
                    Marker marker = markers[used++];
                    marker.Label.text = string.IsNullOrEmpty(spot.Label) ? nearbyAction.Label : spot.Label;
                    marker.Dot.color = SpotColor(spot, nearbyAction);
                    marker.Root.sizeDelta = new Vector2(Mathf.Clamp(marker.Label.preferredWidth + 33f, 67f, 180f), 29f);
                    marker.Root.anchoredPosition = position;
                    marker.Root.gameObject.SetActive(true);
                }
            }

            for (int i = used; i < markers.Count; i++) markers[i].Root.gameObject.SetActive(false);
            GameAction action = visible && near != null ? Find(near.ActionId) : null;
            if (action != null && ScreenPosition(camera, SpotPosition(near), out Vector2 location))
            {
                string label = string.IsNullOrEmpty(near.Label) ? action.Label : near.Label;
                string gate = GateRequirement(near, action);
                nearLabel.text = string.IsNullOrEmpty(gate) ? label : gate;
                nearKey.text = string.IsNullOrEmpty(gate) ? "E" : "–";
                nearEdge.color = SpotColor(near, action);
                nearKey.color = nearEdge.color;
                nearPrompt.sizeDelta = new Vector2(Mathf.Clamp(nearLabel.preferredWidth + 72f, 128f, 310f), 43f);
                Rect bounds = canvasRoot.rect;
                location.x = Mathf.Clamp(location.x, bounds.xMin + nearPrompt.sizeDelta.x / 2f + 8f, bounds.xMax - nearPrompt.sizeDelta.x / 2f - 8f);
                location.y = Mathf.Clamp(location.y + 6f, bounds.yMin + 95f, bounds.yMax - 92f);
                nearPrompt.anchoredPosition = location;
                nearPrompt.gameObject.SetActive(true);
            }
            else
            {
                nearPrompt.gameObject.SetActive(false);
            }

            batch.gameObject.SetActive(!BlocksMovement && game.IsMining);
            if (game.IsMining)
            {
                batchLabel.text = $"机器人采矿 · 剩余 {game.MiningRemaining}";
                Fill(batchFill, game.MiningProgress);
            }

            cursor.gameObject.SetActive(!BlocksMovement && game.IsRealTimeCombat);
            if (cursor.gameObject.activeSelf && RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRoot, Input.mousePosition, null, out Vector2 aim))
                cursor.anchoredPosition = aim;

            float left = toastUntil - Time.unscaledTime;
            bool showToast = left > 0f && !BlocksMovement;
            toast.gameObject.SetActive(showToast);
            if (showToast) toastFade.alpha = Mathf.Clamp01(left / 0.28f);
            toast.anchoredPosition = new Vector2(0, game.IsMining || objective.gameObject.activeSelf ? 97f : 49f);
        }

        public void PresentCombat(int currentWave, int maxWaves, int enemies, float dodgeCharge)
        {
            bool show = game != null && game.IsRealTimeCombat && !BlocksMovement;
            combat.gameObject.SetActive(show);
            if (!show) return;

            wave.text = currentWave > 0 ? $"第 {currentWave} / {maxWaves} 波" : "整备中";
            enemyCount.text = $"感染者 {enemies}";
            dodge.Value.text = dodgeCharge >= 0.999f ? "就绪" : "冷却";
            dodge.Fill.color = dodgeCharge >= 0.999f ? Mint : Amber;
            Fill(dodge.Fill, dodgeCharge);
        }

        private void BuildExploration()
        {
            exploration = Stretch(canvasRoot, "Exploration");
            vitals = Box(exploration, "Survival stats", UpperLeft, new Vector2(16, -16), new Vector2(216, 76), Panel, UpperLeft);
            Paint(Node(vitals, "Status accent", UpperLeft, new Vector2(0, -13), new Vector2(3, 50), UpperLeft), Mint);
            life = BuildMeter(vitals, "生命", Red, new Vector2(13, -8), 190);
            food = BuildMeter(vitals, "饱食度", Amber, new Vector2(13, -41), 190);

            RectTransform location = Box(exploration, "Location", UpperCenter, new Vector2(0, -16), new Vector2(160, 36), Panel, UpperCenter);
            RectTransform pin = Node(location, "Location mark", UpperLeft, new Vector2(12, -13), new Vector2(9, 9), UpperLeft);
            pin.localRotation = Quaternion.Euler(0, 0, 45);
            Paint(pin, Mint, true);
            district = Label(Inset(location, 22, 7, 0, 1), "", 18, White, bold: true);

            RectTransform wallet = Box(exploration, "Resources", UpperRight, new Vector2(-16, -16), new Vector2(264, 49), Panel, UpperRight);
            silver = Resource(wallet, "银", 12, Silver);
            gold = Resource(wallet, "金", 97, Amber);
            diamond = Resource(wallet, "钻石", 179, Mint);
            backpack = MakeButton(exploration, "背包", UpperRight, new Vector2(-16, -72), new Vector2(96, 31), Panel, () => Perform("bag"), UpperRight);
            Label(Inset((RectTransform)backpack.transform, 24, 4, 0, 0), "背包", 15, White);
            RectTransform bKey = Box((RectTransform)backpack.transform, "Bag shortcut", UpperLeft, new Vector2(7, -5), new Vector2(25, 21), Ink, UpperLeft);
            Label(Stretch(bKey, "B"), "B", 15, Mint, numeric: true, bold: true);
            menu = MakeButton(exploration, "暂停菜单", UpperRight, new Vector2(-119, -72), new Vector2(105, 31),
                Panel, () => MenuRequested?.Invoke(), UpperRight);
            Label(Inset((RectTransform)menu.transform, 8, 8, 0, 0), "Esc  菜单", 15, White);

            combat = Box(exploration, "Combat status", UpperCenter, new Vector2(0, -61), new Vector2(309, 49), Panel, UpperCenter);
            Paint(Node(combat, "Combat edge", UpperLeft, Vector2.zero, new Vector2(3, 49), UpperLeft), Red);
            wave = Label(Node(combat, "Wave", UpperLeft, new Vector2(15, -2), new Vector2(101, 28), UpperLeft), "", 17, White, TextAnchor.MiddleLeft, bold: true);
            enemyCount = Label(Node(combat, "Remaining", UpperLeft, new Vector2(15, -24), new Vector2(109, 21), UpperLeft), "", 14, Soft, TextAnchor.MiddleLeft);
            dodge = BuildMeter(combat, "闪避", Mint, new Vector2(143, -9), 150);

            RectTransform controlBox = Box(exploration, "Controls", LowerCenter, new Vector2(0, 12), new Vector2(358, 26), Ink, LowerCenter);
            controls = Label(Inset(controlBox, 5, 5, 0, 0), "", 14, Soft);
            objective = Box(exploration, "Current objective", LowerCenter, new Vector2(0, 48), new Vector2(344, 42), Panel, LowerCenter);
            Paint(Node(objective, "Objective accent", UpperLeft, new Vector2(0, -9), new Vector2(3, 24), UpperLeft), Mint);
            objectiveText = Label(Inset(objective, 14, 12, 0, 0), "", 18, White, bold: true);
            batch = Box(exploration, "Robot mining progress", LowerCenter, new Vector2(0, 47), new Vector2(230, 42), Panel, LowerCenter);
            batchLabel = Label(Node(batch, "Job", UpperLeft, new Vector2(13, -5), new Vector2(205, 24), UpperLeft), "", 15, White);
            RectTransform track = Box(batch, "Progress line", UpperLeft, new Vector2(12, -32), new Vector2(206, 4), Ink, UpperLeft);
            batchFill = Paint(Stretch(track, "Progress"), Mint);

            nearPrompt = Box(exploration, "Nearby interaction", Center, Vector2.zero, new Vector2(140, 43), Ink);
            nearEdge = Paint(Node(nearPrompt, "Interaction accent", UpperLeft, new Vector2(0, -8), new Vector2(3, 27), UpperLeft), Mint);
            RectTransform key = Box(nearPrompt, "Interaction shortcut", UpperLeft, new Vector2(10, -6), new Vector2(33, 31), Card, UpperLeft);
            nearKey = Label(Stretch(key, "E"), "E", 21, Mint, numeric: true, bold: true);
            nearLabel = Label(Inset(nearPrompt, 55, 13, 0, 0), "", 19, White, TextAnchor.MiddleLeft, bold: true);

            toast = Box(exploration, "Recent feedback", LowerCenter, new Vector2(0, 56), new Vector2(350, 36), Ink, LowerCenter);
            toastFade = toast.gameObject.AddComponent<CanvasGroup>();
            toastFade.blocksRaycasts = false;
            toastText = Label(Inset(toast, 15, 15, 0, 0), "", 17, White, bold: true);

            cursor = Node(exploration, "Aim indicator", Center, Vector2.zero, new Vector2(30, 30));
            Paint(Node(cursor, "Aim left", Center, new Vector2(-12, 0), new Vector2(5, 2)), Mint);
            Paint(Node(cursor, "Aim right", Center, new Vector2(12, 0), new Vector2(5, 2)), Mint);
            Paint(Node(cursor, "Aim top", Center, new Vector2(0, 12), new Vector2(2, 5)), Mint);
            Paint(Node(cursor, "Aim bottom", Center, new Vector2(0, -12), new Vector2(2, 5)), Mint);
        }

        private void BuildSplash()
        {
            splash = Stretch(canvasRoot, "End screen");
            Paint(splash, new Color32(6, 14, 23, 154));
            RectTransform body = Box(splash, "End panel", Center, Vector2.zero, new Vector2(426, 270), Ink);
            Paint(Node(body, "End accent", UpperCenter, new Vector2(0, -24), new Vector2(54, 3)), Red);
            Label(Node(body, "Title", UpperCenter, new Vector2(0, -72), new Vector2(365, 68)), "生存中断", 45, White, bold: true);
            splashDescription = Label(Node(body, "Description", UpperCenter, new Vector2(0, -129), new Vector2(345, 42)), "", 17, Soft);
            Button primary = MakeButton(body, "Return", UpperCenter, new Vector2(0, -204), new Vector2(326, 51), Mint,
                () => Perform("restart"));
            Label(Stretch((RectTransform)primary.transform, "Return label"), "返回主界面", 20, Ink, bold: true);
        }

        private void BuildDialogue()
        {
            dialogue = Box(canvasRoot, "Conversation", LowerCenter, new Vector2(0, 51), new Vector2(676, 110), Ink, LowerCenter);
            Paint(Node(dialogue, "Speaker accent", UpperLeft, new Vector2(0, -17), new Vector2(4, 76), UpperLeft), Mint);
            talker = Label(Node(dialogue, "Speaker", UpperLeft, new Vector2(22, -12), new Vector2(505, 27), UpperLeft), "", 17, Mint, TextAnchor.MiddleLeft, bold: true);
            line = Label(Node(dialogue, "Line", UpperLeft, new Vector2(22, -40), new Vector2(514, 59), UpperLeft), "", 23, White, TextAnchor.MiddleLeft);
            Button advance = MakeButton(dialogue, "Next", UpperRight, new Vector2(-13, -37), new Vector2(105, 37), Card, () => Perform("next"), UpperRight);
            Label(Stretch((RectTransform)advance.transform, "Next label"), "继续  ↵", 17, White, bold: true);
        }

        private void BuildModal()
        {
            modalShade = Stretch(canvasRoot, "Modal backdrop");
            Paint(modalShade, new Color32(4, 11, 19, 162), raycast: true);
            modal = Box(modalShade, "Active panel", Center, Vector2.zero, new Vector2(578, 360), Ink);
            Paint(Node(modal, "Modal light", UpperLeft, new Vector2(21, -11), new Vector2(35, 3), UpperLeft), Mint);
            modalTitle = Label(Node(modal, "Title", UpperLeft, new Vector2(22, -20), new Vector2(478, 43), UpperLeft), "", 30, White, TextAnchor.MiddleLeft, bold: true);
            modalCaption = Label(Node(modal, "Short detail", UpperLeft, new Vector2(24, -66), new Vector2(520, 24), UpperLeft), "", 15, Soft, TextAnchor.MiddleLeft);
            Paint(Node(modal, "Divider", UpperLeft, new Vector2(24, -98), new Vector2(530, 1), UpperLeft), Line);
            closeModal = MakeButton(modal, "Close", UpperRight, new Vector2(-16, -18), new Vector2(37, 37), Card, Close, UpperRight);
            Label(Stretch((RectTransform)closeModal.transform, "Close mark"), "×", 27, White, numeric: true);
            modalFeedback = Label(Node(modal, "Panel response", LowerCenter, new Vector2(0, 12), new Vector2(530, 25), LowerCenter), "", 15, Mint);
            contractReward = Label(Node(modal, "Contract reward", UpperLeft, new Vector2(25, -122), new Vector2(525, 42), UpperLeft),
                "", 17, Mint, TextAnchor.MiddleLeft);
            modalActions = Node(modal, "Rows", UpperLeft, new Vector2(24, -110), new Vector2(530, 220), UpperLeft);
            batchPicker = Node(modal, "Batch picker", UpperLeft, new Vector2(24, -112), new Vector2(530, 176), UpperLeft);
            batchCount = Label(Node(batchPicker, "Batch quantity", UpperCenter, new Vector2(0, -37), new Vector2(124, 58)), "", 43, White, numeric: true, bold: true);
            batchCost = Label(Node(batchPicker, "Batch cost", UpperCenter, new Vector2(0, -78), new Vector2(475, 25)), "", 15, Soft);
            PickerButton("−10", "mining-less-ten", -141);
            PickerButton("−", "mining-less", -87);
            PickerButton("+", "mining-more", 87);
            PickerButton("+10", "mining-more-ten", 141);
            Button start = MakeButton(batchPicker, "Start robot", UpperCenter, new Vector2(0, -127), new Vector2(320, 47), Mint, () => Perform("mining-start"));
            Label(Stretch((RectTransform)start.transform, "Start robot label"), "开始采矿", 18, Ink, bold: true);

            inputBox = Node(modal, "Text entry", UpperLeft, new Vector2(24, -117), new Vector2(530, 123), UpperLeft);
            RectTransform field = Box(inputBox, "Entry field", UpperCenter, new Vector2(0, -26), new Vector2(480, 49), Card);
            Text value = Label(Inset(field, 16, 16, 3, 3), "", 20, White, TextAnchor.MiddleLeft);
            Text placeholder = Label(Inset(field, 16, 16, 3, 3), "输入名称", 20, Soft, TextAnchor.MiddleLeft);
            value.horizontalOverflow = HorizontalWrapMode.Overflow;
            entry = field.gameObject.AddComponent<InputField>();
            entry.targetGraphic = field.GetComponent<Image>();
            entry.textComponent = value;
            entry.placeholder = placeholder;
            entry.lineType = InputField.LineType.SingleLine;
            entry.caretColor = Mint;
            entry.customCaretColor = true;
            entry.characterLimit = 20;
            entry.onSubmit.AddListener(Submit);
            field.GetComponent<Image>().raycastTarget = true;
            Button confirm = MakeButton(inputBox, "Confirm", UpperCenter, new Vector2(0, -85), new Vector2(240, 46), Mint, () => Submit(entry.text));
            Label(Stretch((RectTransform)confirm.transform, "Confirm label"), "确定", 18, Ink, bold: true);
        }

        private void PickerButton(string title, string actionId, float x)
        {
            Button button = MakeButton(batchPicker, title, UpperCenter, new Vector2(x, -37), new Vector2(46, 40), Card, () => Perform(actionId));
            Label(Stretch((RectTransform)button.transform, "Amount"), title, 18, White, numeric: true, bold: true);
        }

        private void Refresh()
        {
            bool mainMenu = game.Area is Area.Title or Area.Dead;
            bool hasModal = HasModal;
            if (hasModal && (!modalOpen || modalArea != game.Area || modalBag != game.InBag))
                noticeAtModalEntry = game.NoticeSerial;
            modalOpen = hasModal;
            modalArea = game.Area;
            modalBag = game.InBag;
            exploration.gameObject.SetActive(!mainMenu);
            splash.gameObject.SetActive(game.Area == Area.Dead);
            dialogue.gameObject.SetActive(!mainMenu && game.HasDialogue && !hasModal);
            modalShade.gameObject.SetActive(hasModal);

            if (mainMenu)
            {
                splashDescription.text = game.Message;
            }
            else
            {
                district.text = game.Area is Area.Robot or Area.Bank or Area.Armory or Area.Food ? "集市" : DistrictName(game.Area);
                silver.text = game.Silver.ToString();
                gold.text = game.Gold.ToString();
                diamond.text = game.Diamond.ToString();
                life.Value.text = $"{game.Health}/20";
                food.Value.text = $"{game.Food:0.#}/100";
                Fill(life.Fill, game.Health / 20f);
                Fill(food.Fill, game.Food / 100f);
                vitals.GetComponent<Image>().color = game.Health <= 5 ? new Color32(82, 36, 44, 251) : Panel;
                bool hasBag = Find("bag") != null;
                backpack.gameObject.SetActive(!hasModal && !game.HasDialogue && hasBag);
                menu.gameObject.SetActive(!hasModal);
                ((RectTransform)menu.transform).anchoredPosition = new Vector2(hasBag && !game.HasDialogue ? -119 : -16, -72);
                controls.text = game.IsRealTimeCombat ? "WASD 移动    ·    左键 射击    ·    空格 闪避"
                    : hasBag ? "WASD 移动    ·    E 交互    ·    B 背包" : "WASD 移动    ·    E 交互";
                controls.transform.parent.gameObject.SetActive(!game.HasDialogue && !hasModal);
                if (game.HasDialogue)
                {
                    talker.text = game.Speaker;
                    line.text = game.Message;
                }

                string hint = game.Hint;
                bool showObjective = !hasModal && !game.HasDialogue && !game.IsRealTimeCombat && !game.IsMining && !string.IsNullOrEmpty(hint);
                objective.gameObject.SetActive(showObjective);
                if (showObjective)
                {
                    objectiveText.text = hint;
                    objective.sizeDelta = new Vector2(Mathf.Clamp(objectiveText.preferredWidth + 34f, 177, 610), 42f);
                }
            }

            if (game.NoticeSerial != noticeSeen && !string.IsNullOrWhiteSpace(game.ShortNotice))
            {
                toastText.text = game.ShortNotice;
                toastText.color = game.Health <= 5 || game.Food <= 20 ? Amber : White;
                toast.sizeDelta = new Vector2(Mathf.Clamp(toastText.preferredWidth + 32f, 160f, 640f), 36f);
                toastUntil = Time.unscaledTime + 2.4f;
            }
            noticeSeen = game.NoticeSerial;

            if (hasModal) RefreshModal();
            else currentPrompt = "";
        }

        private void RefreshModal()
        {
            string prompt = game.InputPrompt;
            bool hasEntry = !string.IsNullOrEmpty(prompt);
            bool picker = game.IsChoosingMining && !hasEntry;
            modalTitle.text = hasEntry ? prompt : picker ? "机器人采矿" : game.InContracts ? "巡逻委托" : game.Title;
            modalCaption.text = hasEntry ? "" : ModalCaption();
            modalFeedback.text = game.NoticeSerial != noticeAtModalEntry && Time.unscaledTime < toastUntil ? game.ShortNotice : "";
            closeModal.gameObject.SetActive(Find("back") != null);
            batchPicker.gameObject.SetActive(picker);
            inputBox.gameObject.SetActive(hasEntry);
            modalActions.gameObject.SetActive(!hasEntry && !picker);
            contractReward.gameObject.SetActive(game.InContracts && !game.ContractReady);
            if (game.InContracts && !game.ContractReady)
                contractReward.text = $"奖励　{game.ContractReward}";

            if (hasEntry)
            {
                modal.sizeDelta = new Vector2(578, 260);
                if (currentPrompt != prompt)
                {
                    entry.SetTextWithoutNotify(game.InputDefault ?? "");
                    entry.Select();
                    entry.ActivateInputField();
                }
                currentPrompt = prompt;
                return;
            }

            currentPrompt = "";
            if (picker)
            {
                modal.sizeDelta = new Vector2(578, 300);
                batchCount.text = game.MiningCount.ToString();
                batchCost.text = $"最多 {game.MiningMax} 次  ·  消耗 {game.MiningCount * 0.5f:0.#} 饱食度";
                return;
            }

            int used = 0;
            int columns = game.Area == Area.Bank && !game.InBag ? 2 : 1;
            float rowWidth = (530f - (columns - 1) * 11f) / columns;
            foreach (GameAction action in game.Actions)
            {
                if (action.Id is "back" or "status" or "bag" or "equipment") continue;
                if (used == rows.Count) rows.Add(BuildActionRow());
                ActionRow row = rows[used];
                row.Root.anchoredPosition = new Vector2(used % columns * (rowWidth + 11f), -(used / columns * 64f));
                row.Root.sizeDelta = new Vector2(rowWidth, 56f);
                row.Root.gameObject.SetActive(true);
                row.Id = action.Id;
                row.Title.text = game.InBag ? $"{action.Key}   {action.Label}" : action.Label;
                row.Detail.text = action.Detail;
                row.Swatch.color = ActionColor(action.Id);
                row.Button.interactable = action.Id switch
                {
                    "eat" => game.Capsules > 0,
                    "heal" => game.BloodBottles > 0,
                    "buy-robot" => !game.HasRobot,
                    _ => true
                };
                used++;
            }
            for (int i = used; i < rows.Count; i++) rows[i].Root.gameObject.SetActive(false);
            int rowCount = Mathf.Max(1, Mathf.CeilToInt((float)used / columns));
            modal.sizeDelta = new Vector2(578, 153 + rowCount * 64);
        }

        private string ModalCaption()
        {
            if (game.InBag) return "按 B 收起背包";
            if (game.IsChoosingMining) return game.RobotName;
            if (game.InContracts) return $"{game.ContractObjective}　·　{game.ContractStatus}";
            return game.Area switch
            {
                Area.Bank => "每次兑换消耗 1 点饱食度",
                Area.Armory => $"枪械 Lv.{game.WeaponLevel}      护甲 Lv.{game.ArmorLevel}",
                Area.Robot => $"采矿 Lv.{game.MiningLevel}      机器人 Lv.{game.RobotLevel}",
                _ => ""
            };
        }

        private ActionRow BuildActionRow()
        {
            var row = new ActionRow();
            row.Button = MakeButton(modalActions, "Action", UpperLeft, Vector2.zero, new Vector2(530, 56), Card, () => Perform(row.Id), UpperLeft);
            row.Root = (RectTransform)row.Button.transform;
            row.Swatch = Paint(Node(row.Root, "Action color", UpperLeft, new Vector2(13, -12), new Vector2(3, 32), UpperLeft), Mint);
            row.Title = Label(Inset(row.Root, 25, 10, 4, 24), "", 18, White, TextAnchor.MiddleLeft, bold: true);
            row.Detail = Label(Inset(row.Root, 25, 10, 27, 3), "", 14, Soft, TextAnchor.MiddleLeft);
            return row;
        }

        private Marker BuildMarker()
        {
            RectTransform box = Box(exploration, "Room landmark", Center, Vector2.zero, new Vector2(84, 29), Ink);
            box.SetSiblingIndex(nearPrompt.GetSiblingIndex());
            RectTransform dot = Node(box, "Landmark mark", UpperLeft, new Vector2(11, -11), new Vector2(8, 8), UpperLeft);
            dot.localRotation = Quaternion.Euler(0, 0, 45);
            return new Marker
            {
                Root = box,
                Dot = Paint(dot, Mint),
                Label = Label(Inset(box, 25, 7, 1, 0), "", 15, White, TextAnchor.MiddleLeft)
            };
        }

        private Meter BuildMeter(RectTransform parent, string name, Color color, Vector2 position, float width)
        {
            RectTransform wrapper = Node(parent, name, UpperLeft, position, new Vector2(width, 30), UpperLeft);
            Label(Node(wrapper, "Name", UpperLeft, new Vector2(0, 1), new Vector2(86, 21), UpperLeft), name, 14, Soft, TextAnchor.MiddleLeft);
            Text value = Label(Node(wrapper, "Value", UpperRight, new Vector2(0, 1), new Vector2(96, 21), UpperRight), "", 15, White, TextAnchor.MiddleRight, bold: true);
            RectTransform track = Box(wrapper, "Track", UpperLeft, new Vector2(0, -21), new Vector2(width, 6), Ink, UpperLeft);
            return new Meter { Value = value, Fill = Paint(Stretch(track, "Fill"), color) };
        }

        private Text Resource(RectTransform wallet, string name, float x, Color hue)
        {
            RectTransform dot = Node(wallet, name + " mark", UpperLeft, new Vector2(x + 2, -18), new Vector2(10, 10), UpperLeft);
            dot.localRotation = Quaternion.Euler(0, 0, 45);
            Paint(dot, hue);
            Label(Node(wallet, name, UpperLeft, new Vector2(x + 18, -3), new Vector2(61, 19), UpperLeft), name, 12, Soft, TextAnchor.MiddleLeft);
            return Label(Node(wallet, name + " amount", UpperLeft, new Vector2(x + 18, -18), new Vector2(63, 24), UpperLeft), "", 18, hue, TextAnchor.MiddleLeft, numeric: true, bold: true);
        }

        private void Update()
        {
            if (game == null || sessionOverlay) return;
            if (HasModal && Time.unscaledTime > toastUntil && modalFeedback.text.Length > 0)
                modalFeedback.text = "";

            bool enter = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
            if (!string.IsNullOrEmpty(currentPrompt))
            {
                if (enter && Input.compositionString.Length == 0) Submit(entry.text);
                return;
            }

            if (game.Area is Area.Title or Area.Dead)
            {
                if (game.Area == Area.Dead && enter) Perform("restart");
                return;
            }

            if (game.HasDialogue)
            {
                if (enter) Perform("next");
                return;
            }

            if (game.IsChoosingMining)
            {
                if (enter) Perform("mining-start");
                else if (Input.GetKeyDown(KeyCode.KeypadPlus) || Input.GetKeyDown(KeyCode.Equals)) Perform("mining-more");
                else if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus)) Perform("mining-less");
                else if (Input.GetKeyDown(KeyCode.UpArrow)) Perform("mining-more-ten");
                else if (Input.GetKeyDown(KeyCode.DownArrow)) Perform("mining-less-ten");
                return;
            }

            if (game.Area == Area.City && !HasModal && ReadSecret()) return;
            if (Input.GetKeyDown(KeyCode.B))
            {
                if (game.InBag) Close();
                else if (!HasModal) Perform("bag");
            }
            else if (game.InBag)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) Perform("eat");
                else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) Perform("heal");
            }
        }

        private bool ReadSecret()
        {
            if (Time.unscaledTime >= easterUntil) easterKeys = "";
            if (Input.inputString.Length == 0) return false;
            foreach (char value in Input.inputString)
            {
                string attempt = easterKeys + char.ToLowerInvariant(value);
                if (!"hbzs".StartsWith(attempt, StringComparison.Ordinal))
                {
                    easterKeys = "";
                    return false;
                }
                easterKeys = attempt;
                easterUntil = Time.unscaledTime + 0.65f;
                if (easterKeys == "hbzs")
                {
                    easterKeys = "";
                    game.Execute("hbzs");
                }
            }
            return easterKeys.Length > 0;
        }

        private void Submit(string value)
        {
            if (string.IsNullOrEmpty(currentPrompt) || submittedFrame == Time.frameCount) return;
            submittedFrame = Time.frameCount;
            game.Submit(value);
            if (!string.IsNullOrEmpty(currentPrompt)) entry.ActivateInputField();
        }

        private void Close() => Perform("back");

        private void Perform(string id)
        {
            if (Find(id) == null || submittedFrame == Time.frameCount) return;
            submittedFrame = Time.frameCount;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            game.Execute(id);
        }

        private GameAction Find(string id)
        {
            foreach (GameAction action in game.Actions)
                if (action.Id == id) return action;
            return null;
        }

        private static string GateRequirement(AdventureSpot spot, GameAction action) => spot.ActionId == "combat" ? action.Detail : "";

        private static Color SpotColor(AdventureSpot spot, GameAction action) => spot.ActionId switch
        {
            "combat" => string.IsNullOrEmpty(GateRequirement(spot, action)) ? Red : Amber,
            "mine" or "bank" => Amber,
            "back" => Silver,
            _ => Mint
        };

        private static Color ActionColor(string id)
        {
            if (id.EndsWith("gold", StringComparison.Ordinal) || id.Contains("capsule")) return Amber;
            if (id is "heal" or "buy-bottle") return Red;
            return Mint;
        }

        private static string DistrictName(Area area) => area switch
        {
            Area.City => "主城区",
            Area.Mine => "矿区",
            Area.Market => "集市",
            Area.Infirmary => "医务室",
            Area.Combat => "战区",
            _ => "地下城"
        };

        private bool IsHeaderOrFooter(Vector2 point) => point.y > canvasRoot.rect.yMax - 63 || point.y < canvasRoot.rect.yMin + 49;

        private bool ScreenPosition(Camera camera, Vector3 worldPoint, out Vector2 local)
        {
            Vector3 projected = camera.WorldToScreenPoint(worldPoint);
            if (projected.z > 0 && projected.x > 20 && projected.x < Screen.width - 20 && projected.y > 18 && projected.y < Screen.height - 18)
                return RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRoot, projected, null, out local);
            local = default;
            return false;
        }

        private static Vector3 SpotPosition(AdventureSpot spot) => spot.PromptAnchor != null ? spot.PromptAnchor.position : spot.transform.position + Vector3.up * 1.65f;

        private static Font FindFont(params string[] names)
        {
            var installed = new HashSet<string>(Font.GetOSInstalledFontNames(), StringComparer.OrdinalIgnoreCase);
            foreach (string name in names)
            {
                if (!installed.Contains(name)) continue;
                Font font = Font.CreateDynamicFontFromOSFont(name, 28);
                if (font == null) continue;
                font.hideFlags = HideFlags.DontSave;
                return font;
            }
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private static Sprite RoundedSprite()
        {
            const int size = 32;
            const float radius = 7;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(0, Mathf.Abs(x + 0.5f - size / 2f) - size / 2f + radius);
                float dy = Mathf.Max(0, Mathf.Abs(y + 0.5f - size / 2f) - size / 2f + radius);
                byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(radius + 0.35f - Mathf.Sqrt(dx * dx + dy * dy)) * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, size, size), Center, 100f, 0, SpriteMeshType.FullRect, new Vector4(9, 9, 9, 9));
        }

        private static void Fill(Image fill, float fraction)
        {
            Vector2 anchor = fill.rectTransform.anchorMax;
            anchor.x = Mathf.Clamp01(fraction);
            fill.rectTransform.anchorMax = anchor;
        }

        private Button MakeButton(RectTransform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Color color, Action click, Vector2? pivot = null)
        {
            RectTransform rect = Node(parent, name, anchor, position, size, pivot);
            Image graphic = Paint(rect, color, rounded: true, raycast: true);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = graphic;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.highlightedColor = new Color(0.87f, 0.98f, 1f);
            colors.pressedColor = new Color(0.65f, 0.78f, 0.8f);
            colors.disabledColor = new Color(0.65f, 0.69f, 0.71f, 0.65f);
            colors.fadeDuration = 0.11f;
            button.colors = colors;
            button.onClick.AddListener(() => click());
            return button;
        }

        private Text Label(RectTransform rect, string value, int size, Color color, TextAnchor align = TextAnchor.MiddleCenter, bool numeric = false, bool bold = false)
        {
            Text label = rect.gameObject.AddComponent<Text>();
            label.font = numeric ? latin : chinese;
            label.fontSize = size;
            label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            label.alignment = align;
            label.color = color;
            label.text = value;
            label.raycastTarget = false;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = Mathf.Max(11, size - 8);
            label.resizeTextMaxSize = size;
            return label;
        }

        private RectTransform Box(RectTransform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Color color, Vector2? pivot = null)
        {
            RectTransform rect = Node(parent, name, anchor, position, size, pivot);
            Paint(rect, color, rounded: true);
            return rect;
        }

        private Image Paint(RectTransform rect, Color color, bool rounded = false, bool raycast = false)
        {
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            if (rounded)
            {
                image.sprite = rounding;
                image.type = Image.Type.Sliced;
            }
            return image;
        }

        private static RectTransform Node(RectTransform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Vector2? pivot = null)
        {
            RectTransform node = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            node.SetParent(parent, false);
            node.anchorMin = node.anchorMax = anchor;
            node.pivot = pivot ?? Center;
            node.anchoredPosition = position;
            node.sizeDelta = size;
            return node;
        }

        private static RectTransform Stretch(RectTransform parent, string name)
        {
            RectTransform node = Node(parent, name, Vector2.zero, Vector2.zero, Vector2.zero);
            node.anchorMax = Vector2.one;
            node.offsetMin = node.offsetMax = Vector2.zero;
            return node;
        }

        private static RectTransform Inset(RectTransform parent, float left, float right, float top, float bottom)
        {
            RectTransform node = Stretch(parent, "Content");
            node.offsetMin = new Vector2(left, bottom);
            node.offsetMax = new Vector2(-right, -top);
            return node;
        }
    }
}
