using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Dungeons2087
{
    public static partial class DungeonSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/2087.unity";
        private const string ArtPath = "Assets/StageArt";
        private static Material[] deck;
        private static Material shell, deckEdge, rim, carbon, aluminum, porcelain, bronze, redPaint, amberPaint;
        private static Material aqua, amber, alarm, icy, moss, mineRock, ore, deepOre;
        private static Font signFont;
        private static Mesh crystalMesh;

        [MenuItem("2087/Build Windows player")]
        public static void BuildWindows()
        {
            string output = Environment.GetEnvironmentVariable("DUNGEONS_BUILD_PATH");
            if (string.IsNullOrWhiteSpace(output))
            {
                output = "Builds/Windows/2087 Dungeons.exe";
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
            BuildReport report = BuildPipeline.BuildPlayer(new[] { ScenePath }, output, BuildTarget.StandaloneWindows64, BuildOptions.None);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException("Windows player build failed.");
            }
        }

        [MenuItem("2087/Rebuild authored scene")]
        public static void Create()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            PrepareArt();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "2087";
            PlayerSettings.productName = "2087 Dungeons";
            PlayerSettings.companyName = "2087 Dungeons";
            PlayerSettings.defaultScreenWidth = 1440;
            PlayerSettings.defaultScreenHeight = 810;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            QualitySettings.pixelLightCount = 7;
            QualitySettings.antiAliasing = 4;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ColorOf("#637A8A");
            RenderSettings.fog = false;
            RenderSettings.fogColor = ColorOf("#081018");
            RenderSettings.skybox = null;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;

            var cameraObject = new GameObject("Exploration Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = RenderSettings.fogColor;
            camera.orthographic = true;
            camera.orthographicSize = 8.5f;
            camera.nearClipPlane = 0.15f;
            camera.farClipPlane = 60f;
            var cameraTarget = new Vector3(0f, 0f, 0.6f);
            camera.transform.position = cameraTarget + new Vector3(0f, 17f, -10f);
            camera.transform.LookAt(cameraTarget);

            var lightObject = new GameObject("Main warehouse light", typeof(Light));
            Light key = lightObject.GetComponent<Light>();
            key.type = LightType.Directional;
            key.color = ColorOf("#EAF2FB");
            key.intensity = 1.12f;
            key.shadows = LightShadows.Soft;
            key.shadowStrength = 0.50f;
            key.transform.rotation = Quaternion.Euler(53f, -39f, 2f);

            Transform levels = Group(null, "Decks", Vector3.zero);
            AdventureRoom[] rooms =
            {
                Title(levels),
                Infirmary(levels),
                City(levels),
                Mine(levels),
                Market(levels),
                Combat(levels)
            };
            foreach (AdventureRoom room in rooms)
            {
                room.gameObject.SetActive(room.Area == Area.Title);
            }

            Transform player = MakePlayer();
            var game = new GameObject("Adventure", typeof(DungeonRuntime));
            game.GetComponent<DungeonRuntime>().Configure(camera, rooms, player);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
        }

        private static AdventureRoom Title(Transform parent)
        {
            var room = Room(parent, Area.Title, "00 — Service lock", 21f, 17f, "#5E7E8A", "#0A131C", aqua, new Vector3(0f, 0f, 0f));
            Transform root = room.transform;
            var ship = Group(root, "Anemone service lander", new Vector3(4.15f, 0f, 1.9f));
            ship.localRotation = Quaternion.Euler(0f, -22f, 0f);
            Octagon(root, "Berthing circumference", new Vector3(4.2f, 0.024f, 1.65f), 4.12f, 0.065f, rim, 16);
            Octagon(root, "Berthing safety lamps", new Vector3(4.2f, 0.05f, 1.65f), 3.68f, 0.035f, aqua, 16, 2);
            Box(ship, "Landing skid L", -1.72f, 0.28f, 0f, 0.21f, 0.30f, 4.02f, deckEdge);
            Box(ship, "Landing skid R", 1.72f, 0.28f, 0f, 0.21f, 0.30f, 4.02f, deckEdge);
            Box(ship, "Hull", 0f, 0.94f, 0f, 2.94f, 1.17f, 4.55f, shell);
            Box(ship, "Hull armor", 0f, 1.51f, -0.25f, 2.67f, 0.22f, 3.5f, porcelain);
            Box(ship, "Observation glass", 0f, 1.41f, 1.61f, 2.45f, 0.25f, 1.05f, carbon);
            Box(ship, "Observation frame", 0f, 1.57f, 1.83f, 2.1f, 0.045f, 0.20f, aqua);
            Box(ship, "Service stripe", 0.62f, 1.635f, -0.52f, 0.37f, 0.04f, 2.85f, amberPaint);
            Box(ship, "Service stripe", 0.91f, 1.635f, -0.52f, 0.11f, 0.04f, 2.85f, amberPaint);
            Box(ship, "Air inlet", -0.65f, 1.64f, -0.37f, 0.73f, 0.06f, 0.68f, carbon);
            for (int i = 0; i < 4; i++)
            {
                Box(ship, "Inlet louver", -0.65f, 1.68f, -0.61f + 0.16f * i, 0.66f, 0.04f, 0.045f, aluminum);
            }

            for (int side = -1; side <= 1; side += 2)
            {
                Box(ship, "Skid pier", side * 1.62f, 0.61f, 0.5f, 0.26f, 0.77f, 0.62f, rim);
                Box(ship, "Propulsion chamber", side * 1.68f, 1.02f, -1.15f, 0.55f, 0.50f, 1.67f, shell);
                Box(ship, "Propulsion exhaust", side * 1.68f, 1.01f, -2.01f, 0.37f, 0.22f, 0.12f, aqua);
            }

            FloorSign(root, "2 0 8 7", 4.2f, -3.08f, 13.4f, "#88ABB0");
            FloorSign(root, "ANEMONE / SERVICE", 4.2f, -4.0f, 6.4f, "#678793");
            Point(root, "Dock cyan", 6.1f, 3.6f, 2.0f, "#66EBE4", 4f, 10f);
            Gate(root, 4.2f, 7.1f, "SERVICE LOCK", "00", aqua);
            CrateStack(root, -6.8f, 5.1f, 1.14f, amber);
            CrateStack(root, 8.1f, -4.7f, 1f, aqua);
            return room;
        }

        private static AdventureRoom Infirmary(Transform parent)
        {
            var room = Room(parent, Area.Infirmary, "01 — Recovery wing", 20f, 16f, "#6D8598", "#0F1824", icy, new Vector3(0, 0, -3.4f));
            Transform root = room.transform;
            Gate(root, 0f, 6.5f, "MEDICAL", "01", icy);
            FloorSign(root, "RECOVERY", 0f, 1.55f, 11.0f, "#6F91A2");
            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? -5.85f : 5.85f;
                var bed = Group(root, "Recovery cot", new Vector3(x, 0f, 3.4f));
                Box(bed, "Bed base", 0f, 0.23f, 0f, 1.59f, 0.52f, 3.05f, deckEdge, true);
                Box(bed, "Thermoformed shell", 0f, 0.54f, 0f, 1.78f, 0.38f, 3.13f, porcelain);
                Box(bed, "Quilt", 0f, 0.77f, -0.30f, 1.35f, 0.11f, 2.28f, shell);
                Box(bed, "Headrest", 0f, 0.80f, 1.00f, 1.30f, 0.18f, 0.61f, porcelain);
                Box(bed, "Wake status", 0f, 0.55f, -1.585f, 1.38f, 0.045f, 0.03f, aqua);
                Terminal(root, x + i * 4f - 2f, 3.0f, icy, 0);
                Route(root, new Vector2(x, -1.35f), new Vector2(x, 1.58f), icy);
            }

            Person(root, "小帅", new Vector3(2.2f, 0f, 0.45f), 150f, porcelain, icy, false);
            room.Spots = new[] { Spot(root, "next", "小帅", 2.2f, -0.72f, icy, 2.2f) };
            Terminal(root, -6.7f, -4.8f, aqua, 0);
            CrateStack(root, 8.0f, -4.58f, 1f, aqua);
            Point(root, "Blue exam light", 2.7f, 3.7f, 1.7f, "#B0CBF8", 3.1f, 7f);
            return room;
        }

        private static AdventureRoom City(Transform parent)
        {
            var room = Room(parent, Area.City, "02 — Central interchange", 23f, 17f, "#647D8A", "#0A151F", aqua, new Vector3(0f, 0f, -4.85f));
            Transform root = room.transform;
            Gate(root, -7.25f, 6.0f, "EXTRACTION", "K", amber);
            Gate(root, 0f, 6.95f, "QUARANTINE", "Z", alarm);
            Gate(root, 7.25f, 6.0f, "EXCHANGE", "S", aqua);
            room.Spots = new[]
            {
                Spot(root, "mine-area", "进入矿区", -7.25f, 4.22f, amber, 2f),
                Spot(root, "market", "进入集市", 7.25f, 4.22f, aqua, 2f),
                Spot(root, "combat", "进入战区", 0f, 5.28f, alarm, 2f),
                Spot(root, "contract", "委托终端", 4.0f, -4.22f, aqua, 1.8f)
            };
            Route(root, new Vector2(0, -3.2f), new Vector2(0, 4.25f), alarm);
            Route(root, new Vector2(-0.9f, -2.76f), new Vector2(-7.25f, -2.76f), amber);
            Route(root, new Vector2(-7.25f, -2.76f), new Vector2(-7.25f, 3.2f), amber);
            Route(root, new Vector2(0.9f, -2.76f), new Vector2(7.25f, -2.76f), aqua);
            Route(root, new Vector2(7.25f, -2.76f), new Vector2(7.25f, 3.2f), aqua);
            FloorSign(root, "2 0 8 7", 0f, -5.95f, 10.4f, "#507487");
            Octagon(root, "Transit badge", new Vector3(0f, 0.024f, -2.75f), 1.07f, 0.10f, aluminum, 8);
            KineticCore(root, -3.85f, 2.28f, amber);
            KineticCore(root, 3.85f, 2.28f, aqua);
            Planter(root, -8.7f, -4.9f, 0);
            Planter(root, 8.7f, -4.9f, 0);
            Bench(root, -5.5f, -6.1f);
            Bench(root, 5.5f, -6.1f);
            Terminal(root, -8.8f, 0f, amber, 0f);
            Terminal(root, 8.8f, 0f, aqua, 0f);
            Terminal(root, 4.0f, -3.57f, aqua, 0f);
            FloorSign(root, "PATROL", 4.0f, -5.2f, 2.8f, "#83C4BA");
            return room;
        }

        private static AdventureRoom Mine(Transform parent)
        {
            var room = Room(parent, Area.Mine, "03 — Cobalt seams", 22f, 18f, "#638192", "#0B1A25", amber, new Vector3(0f, 0f, -4.25f));
            Transform root = room.transform;
            Gate(root, 0f, 7.95f, "FRACTURE", "03", amber);
            room.Spots = new[]
            {
                Vein(root, -6.55f, 3.10f, "矿脉 A", 91),
                Vein(root, 0f, 5.40f, "矿脉 B", 77),
                Vein(root, 6.65f, 0.45f, "矿脉 C", 94),
                Spot(root, "robot-mine", "机器人采矿", 6.65f, 4.65f, amber, 2.15f),
                Spot(root, "back", "返回主城区", 0f, -7.22f, aqua, 1.8f)
            };
            Lift(root, 0f, -7.25f, aqua);
            Headframe(root, 6.65f, 6.28f, amber);
            Rover(root, new Vector3(6.62f, 0.0f, 5.68f), 184f);
            Rail(root, -3.25f, -3.65f, 7.8f, 0f);
            Rail(root, 2.30f, 2.65f, 5.85f, 90f);
            Rail(root, -6.0f, 1.88f, 5.4f, 90f);
            CrateStack(root, -8.45f, -5.3f, 1.1f, amber);
            CrateStack(root, 8.75f, -5.0f, 1.1f, amber);
            FloorSign(root, "EXTRACTION DECK", 0f, -2.65f, 7.8f, "#6A8493");
            Point(root, "Blue seam A", -6.5f, 2.9f, 3.2f, "#49BDEB", 3.2f, 6.6f);
            Point(root, "Blue seam C", 6.5f, 2.9f, 0.8f, "#51DBE5", 2.6f, 6.5f);
            return room;
        }

        private static AdventureRoom Market(Transform parent)
        {
            var room = Room(parent, Area.Market, "04 — Exchange hall", 23f, 18f, "#807878", "#17151B", amber, new Vector3(0f, 0f, -4.38f));
            Transform root = room.transform;
            Gate(root, 0f, 7.9f, "EXCHANGE HALL", "04", amber);
            room.Spots = new[]
            {
                Booth(root, -7.0f, 1.02f, "robot", "机器人系统", "WORKSHOP", icy, 0.5f),
                Booth(root, 7.0f, 1.02f, "bank", "矿物兑换", "RESERVE BANK", amber, -0.5f),
                Booth(root, -7.0f, 5.8f, "armory", "装备升级", "ARMORY", alarm, 0.5f),
                Booth(root, 7.0f, 5.8f, "food-shop", "购买补给", "PROVISIONS", moss, -0.5f),
                Spot(root, "back", "返回主城区", 0f, -7.28f, aqua, 1.8f)
            };
            Lift(root, 0f, -7.3f, aqua);
            Box(root, "Trading strip", 0f, 0.015f, 1.10f, 5.3f, 0.035f, 10.25f, deckEdge);
            Box(root, "Carpet", 0f, 0.038f, 1.1f, 4.5f, 0.035f, 9.5f, redPaint);
            for (int s = -1; s <= 1; s += 2)
            {
                Box(root, "Inlay", s * 1.9f, 0.059f, 1.1f, 0.08f, 0.014f, 9.35f, bronze);
                Planter(root, s * 3.5f, 6.35f, 45f);
                CrateStack(root, s * 8.35f, -5.20f, 1.0f, amber);
            }

            FloorSign(root, "OPEN MARKET", 0f, -1.85f, 8.6f, "#D7AA8D", 0.07f);
            Octagon(root, "Trade crest", new Vector3(0f, 0.069f, 2.17f), 1.42f, 0.10f, bronze, 8);
            Octagon(root, "Trade crest inset", new Vector3(0f, 0.070f, 2.17f), 0.70f, 0.12f, bronze, 8);
            Point(root, "Amber trading lights", 0f, 4.5f, 2.0f, "#FFC089", 3.1f, 10f);
            return room;
        }

        private static AdventureRoom Combat(Transform parent)
        {
            var room = Room(parent, Area.Combat, "05 — Breach chamber", 25f, 20f, "#806875", "#211622", alarm, new Vector3(0, 0, -6.45f));
            Transform root = room.transform;
            Gate(root, 0f, 8.85f, "QUARANTINE", "Z", alarm, 1.28f);
            Route(root, new Vector2(0, -5.20f), new Vector2(0, 6.88f), alarm);
            Octagon(root, "Containment ring", new Vector3(0f, 0.033f, 2.03f), 4.54f, 0.075f, redPaint, 16);
            Octagon(root, "Ring lamps", new Vector3(0f, 0.039f, 2.03f), 3.88f, 0.057f, alarm, 16, 2);
            FloorSign(root, "CONTAINMENT", 0f, 2.18f, 7.6f, "#BC8090");

            for (int side = -1; side <= 1; side += 2)
            {
                Box(root, "Conduit trench", side * 11.3f, 0.03f, -0.92f, 1.74f, 0.18f, 13.2f, carbon);
                for (int i = 0; i < 13; i++)
                {
                    Box(root, "Conduit grate", side * 11.30f, 0.13f, -6.9f + i * 0.97f, 1.5f, 0.08f, 0.35f, rim);
                }
                Box(root, "Conduit power feed", side * 11.32f, 0.16f, -0.91f, 0.19f, 0.08f, 12.7f, alarm);
                Cover(root, side * 5.9f, -1.48f, side * 12f);
                Cover(root, side * 6.85f, 5.31f, side * -14f);
                CrateStack(root, side * 9.14f, -6.10f, 1f, alarm);
                Point(root, "Red breach strobe", side * 8.2f, 3.55f, 3.6f, "#FF5349", 3.7f, 9f);
            }

            var spawns = new List<Transform>();
            foreach (Vector3 position in new[]
            {
                new Vector3(0f, 0f, 5.4f), new Vector3(-8.2f, 0f, 1.62f),
                new Vector3(8.2f, 0f, 1.62f), new Vector3(-4.13f, 0f, 6.05f), new Vector3(4.13f, 0f, 6.05f),
                new Vector3(-8.1f, 0f, -4.55f), new Vector3(8.1f, 0f, -4.55f)
            })
            {
                spawns.Add(Group(root, "Encounter point " + (spawns.Count + 1), position));
            }

            room.EnemySpawns = spawns.ToArray();
            room.Spots = new[] { Spot(root, "back", "返回主城区", 0f, -8.26f, aqua, 1.65f) };
            Lift(root, 0f, -8.26f, aqua);
            return room;
        }
    }
}
