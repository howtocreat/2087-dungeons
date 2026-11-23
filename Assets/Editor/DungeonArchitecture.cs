using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dungeons2087
{
    public static partial class DungeonSceneBuilder
    {
        private static void PrepareArt()
        {
            if (!AssetDatabase.IsValidFolder(ArtPath))
            {
                AssetDatabase.CreateFolder("Assets", "StageArt");
            }

            deck = new[]
            {
                Surface("Floor alloy", "#2B404B"), Surface("Deck slate", "#2F454F"),
                Surface("Deck indigo", "#283B47"), Surface("Deck desaturated", "#334850"),
                Surface("Deck highlight", "#384E57")
            };
            shell = Surface("Wall graphite", "#364C57", 0.05f);
            deckEdge = Surface("Carbon", "#14222D");
            rim = Surface("Blackened steel", "#526775", 0.10f);
            carbon = Surface("Shadow black", "#0C1A24");
            aluminum = Surface("Light alloy", "#8EA1A3", 0.08f);
            porcelain = Surface("Ceramic armor", "#D9DCD0", 0.01f);
            bronze = Surface("Brushed bronze", "#AD7649", 0.08f);
            redPaint = Surface("Oxide paint", "#874352");
            amberPaint = Surface("Safety orange", "#E49548");
            aqua = Surface("Cyan tube", "#4EC4BA", glow: "#39D5BB", emission: 0.63f);
            amber = Surface("Amber tube", "#EAB05D", glow: "#FFAC43", emission: 0.63f);
            alarm = Surface("Red tube", "#BE4F63", glow: "#F94C65", emission: 0.53f);
            icy = Surface("Blue tube", "#649CD0", glow: "#77B8F2", emission: 0.59f);
            moss = Surface("Green tube", "#68A57C", glow: "#63D187", emission: 0.38f);
            mineRock = Surface("Mine stone", "#405666", 0.03f);
            ore = Surface("Ore crystal cyan", "#4B9CB5", 0.1f, "#4BAEC6", 0.36f);
            deepOre = Surface("Ore crystal", "#657BBA", 0.10f, "#5E8DDE", 0.42f);
            signFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            crystalMesh = LoadCrystalMesh();
        }

        private static Material Surface(string name, string color, float metallic = 0f, string glow = null, float emission = 0f)
        {
            string path = ArtPath + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard")) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader == null)
            {
                material.shader = Shader.Find("Standard");
            }

            material.color = ColorOf(color);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Glossiness", 0.30f);
            if (glow != null)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", ColorOf(glow) * emission);
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static AdventureRoom Room(Transform parent, Area area, string name, float width, float depth,
            string ambience, string fog, Material pilot, Vector3 spawn)
        {
            Transform root = Group(parent, name, Vector3.zero);
            var room = root.gameObject.AddComponent<AdventureRoom>();
            room.Area = area;
            room.FloorSize = new Vector2(width, depth);
            room.Ambient = ColorOf(ambience);
            room.Fog = ColorOf(fog);
            room.Spawn = Group(root, "Player arrival", spawn);
            room.Spots = Array.Empty<AdventureSpot>();
            room.EnemySpawns = Array.Empty<Transform>();

            Box(root, "Foundation block", 0, -0.53f, 0, width + 0.9f, 0.93f, depth + 0.8f, deckEdge);
            Box(root, "Foundation ledge", 0, -0.16f, 0, width + 0.52f, 0.24f, depth + 0.50f, rim);
            Box(root, "Walkable landing", 0, -0.13f, 0, width, 0.25f, depth, deckEdge, true);

            Transform floor = Group(root, "Sectional flooring", Vector3.zero);
            int nx = Mathf.RoundToInt(width / 2.15f);
            int nz = Mathf.RoundToInt(depth / 2.15f);
            float tw = (width - 0.12f) / nx;
            float td = (depth - 0.12f) / nz;
            var random = new System.Random(2087 + (int)area * 541);
            for (int ix = 0; ix < nx; ix++)
            {
                for (int iz = 0; iz < nz; iz++)
                {
                    float x = (ix + 0.5f) * tw - tw * nx * 0.5f;
                    float z = (iz + 0.5f) * td - td * nz * 0.5f;
                    Material tile = deck[random.Next(deck.Length)];
                    Box(floor, "Anti-slip panel", x, -0.025f, z, tw - 0.032f, 0.05f, td - 0.032f, tile).GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                    if ((ix + iz * 3) % 9 == 0)
                    {
                        Box(floor, "Panel inspection plate", x + tw * 0.27f, 0.007f, z - td * 0.32f, 0.45f, 0.014f, 0.15f, rim);
                        Box(floor, "Panel inspection stripe", x + tw * 0.27f, 0.02f, z - td * 0.32f, 0.24f, 0.02f, 0.032f, aluminum);
                    }
                }
            }

            float hx = width * 0.5f;
            float hz = depth * 0.5f;
            Box(root, "North wall", 0, 0.76f, hz - 0.2f, width + 0.15f, 1.70f, 0.50f, shell, true);
            Box(root, "North capping", 0, 1.61f, hz - 0.18f, width + 0.25f, 0.13f, 0.65f, rim);
            Box(root, "North service feed", 0, 0.43f, hz - 0.47f, width - 0.20f, 0.062f, 0.055f, pilot);
            Box(root, "South low wall", 0, 0.24f, -hz + 0.12f, width + 0.06f, 0.55f, 0.34f, shell, true);
            Box(root, "South capping", 0, 0.52f, -hz + 0.12f, width + 0.18f, 0.10f, 0.47f, rim);
            for (int side = -1; side <= 1; side += 2)
            {
                Box(root, "Side wall", side * (hx - 0.14f), 0.54f, 0, 0.42f, 1.22f, depth, shell, true);
                Box(root, "Side capping", side * (hx - 0.14f), 1.15f, 0, 0.55f, 0.12f, depth + 0.15f, rim);
                Box(root, "Side floor skirting", side * (hx - 0.57f), 0.018f, 0, 0.13f, 0.024f, depth - 0.45f, rim);
                for (int i = 0; i < 5; i++)
                {
                    float z = Mathf.Lerp(-hz + 1.05f, hz - 0.85f, i / 4f);
                    Box(root, "Pillar reinforcement", side * (hx - 0.3f), 0.53f, z, 0.58f, 1.06f, 0.66f, deckEdge);
                    Box(root, "Pillar marker", side * (hx - 0.6f), 0.84f, z, 0.045f, 0.15f, 0.35f, pilot);
                }
            }

            Point(root, "Low warehouse fill", 0, 4.7f, -3.7f, "#97BCD7", 2.8f, 15f);
            return room;
        }

        private static AdventureSpot Spot(Transform parent, string action, string label, float x, float z, Material accent, float radius)
        {
            Transform root = Group(parent, "Interact / " + label, new Vector3(x, 0, z));
            var spot = root.gameObject.AddComponent<AdventureSpot>();
            spot.ActionId = action;
            spot.Label = label;
            spot.Radius = radius;
            spot.PromptAnchor = Group(root, "Prompt", new Vector3(0, 1.55f, 0.02f));
            Octagon(root, "Marker collar", new Vector3(0, 0.032f, 0), 0.63f, 0.070f, accent, 8, 2);
            Box(root, "Marker north", 0, 0.027f, 0.65f, 0.19f, 0.032f, 0.19f, accent);
            Box(root, "Marker south", 0, 0.027f, -0.65f, 0.19f, 0.032f, 0.19f, accent);
            return spot;
        }

        private static void Gate(Transform parent, float x, float z, string title, string badge, Material indicator, float scale = 1f)
        {
            Transform root = Group(parent, title + " bulkhead", new Vector3(x, 0f, z));
            root.localScale = Vector3.one * scale;
            Box(root, "Recess", 0, 1.02f, 0.13f, 3.48f, 2.07f, 0.73f, deckEdge, true);
            Box(root, "Shutter left", -0.74f, 0.89f, -0.24f, 1.40f, 1.74f, 0.22f, shell);
            Box(root, "Shutter right", 0.74f, 0.89f, -0.24f, 1.40f, 1.74f, 0.22f, shell);
            Box(root, "Central guide rail", 0, 0.90f, -0.40f, 0.11f, 1.48f, 0.075f, rim);
            Box(root, "Armored lintel", 0, 2.04f, -0.11f, 3.74f, 0.58f, 0.97f, rim);
            Box(root, "Pilot band", 0, 2.22f, -0.61f, 3.48f, 0.065f, 0.05f, indicator);
            Box(root, "Sign plate", 0, 1.98f, -0.615f, 3.46f, 0.29f, 0.055f, deckEdge);
            Sign(root, title, new Vector3(0, 1.96f, -0.66f), 2.8f, "#D9EBE8", Quaternion.identity);
            FloorSign(root, badge, 0, -1.04f, 1.25f, "#829AA4");
            for (int side = -1; side <= 1; side += 2)
            {
                Box(root, "Jaw", side * 1.70f, 0.97f, -0.18f, 0.48f, 1.95f, 0.97f, rim);
                Box(root, "Status rail", side * 1.60f, 0.94f, -0.69f, 0.092f, 1.40f, 0.037f, indicator);
                for (int i = 0; i < 3; i++)
                {
                    Box(root, "Shutter rib", side * 0.75f, 0.45f + i * 0.30f, -0.365f, 1.08f, 0.04f, 0.04f, rim);
                }
            }
        }

        private static void Terminal(Transform parent, float x, float z, Material accent, float yaw)
        {
            Transform root = Group(parent, "Data terminal", new Vector3(x, 0f, z));
            root.rotation = Quaternion.Euler(0, yaw, 0);
            Box(root, "Pedestal", 0, 0.12f, 0, 0.83f, 0.24f, 0.67f, deckEdge, true);
            Box(root, "Sloped housing", 0, 0.59f, 0, 0.68f, 0.75f, 0.59f, rim);
            Box(root, "Bezel", 0, 0.98f, -0.06f, 0.72f, 0.17f, 0.67f, carbon).localRotation = Quaternion.Euler(12, 0, 0);
            Box(root, "Screen glass", -0.08f, 1.08f, -0.085f, 0.38f, 0.023f, 0.43f, accent).localRotation = Quaternion.Euler(12, 0, 0);
            Box(root, "Card slot", 0.22f, 1.085f, -0.13f, 0.12f, 0.02f, 0.12f, aluminum);
            Box(root, "Vent channel", 0, 0.54f, -0.32f, 0.40f, 0.13f, 0.035f, carbon);
        }

        private static void KineticCore(Transform parent, float x, float z, Material accent)
        {
            Transform root = Group(parent, "Heat exchanger", new Vector3(x, 0f, z));
            Box(root, "Counterweight", 0, 0.16f, 0, 2.36f, 0.32f, 1.72f, deckEdge, true);
            Box(root, "Exchange body", 0, 0.48f, 0, 1.96f, 0.53f, 1.32f, shell);
            Box(root, "Porcelain head", 0, 0.79f, 0, 2.13f, 0.13f, 1.43f, aluminum);
            Box(root, "Roof grille", 0, 0.87f, 0.08f, 1.65f, 0.066f, 0.85f, carbon);
            for (int i = 0; i < 7; i++)
            {
                Box(root, "Radiator slat", -0.75f + 0.25f * i, 0.91f, 0.08f, 0.09f, 0.04f, 0.81f, rim);
            }

            Box(root, "Status strip", 0, 0.50f, -0.68f, 1.55f, 0.067f, 0.057f, accent);
            Box(root, "Service pipe", -0.82f, 0.62f, 0.64f, 0.18f, 0.19f, 1.21f, bronze);
            Box(root, "Service pipe two", 0.82f, 0.62f, 0.64f, 0.18f, 0.19f, 1.21f, bronze);
        }

        private static void Bench(Transform parent, float x, float z)
        {
            Box(parent, "Transit bench", x, 0.37f, z, 2.04f, 0.16f, 0.7f, aluminum, true);
            Box(parent, "Bench stand", x, 0.18f, z, 1.63f, 0.37f, 0.45f, deckEdge);
        }

        private static void Planter(Transform parent, float x, float z, float yaw)
        {
            Transform root = Group(parent, "Hydration planter", new Vector3(x, 0, z));
            root.localRotation = Quaternion.Euler(0f, yaw, 0);
            Box(root, "Planter casing", 0, 0.29f, 0, 1.70f, 0.62f, 0.74f, rim, true);
            Box(root, "Planter soil", 0, 0.62f, 0, 1.56f, 0.06f, 0.57f, carbon);
            for (int i = 0; i < 10; i++)
            {
                Crystal(root, "Hydroponic leaf", new Vector3(-0.74f + i * 0.159f, 0.79f + i % 3 * 0.06f, i % 2 * 0.14f - 0.08f),
                    new Vector3(0.18f, 0.42f + i % 4 * 0.05f, 0.14f), Quaternion.Euler((i % 2 == 0 ? -22 : 22), i * 41f, (i % 3 - 1) * 24f), moss);
            }
        }

        private static AdventureSpot Vein(Transform parent, float x, float z, string label, int seed)
        {
            var random = new System.Random(seed);
            Transform root = Group(parent, "Ore formation", new Vector3(x, 0, z));
            Box(root, "Ground break", 0, 0.18f, 0.31f, 1.88f, 0.41f, 1.30f, mineRock, true);
            for (int i = 0; i < 8; i++)
            {
                float offset = (float)random.NextDouble() - 0.5f;
                float height = i == 3 ? 2.35f : 0.51f + (float)random.NextDouble() * 1.25f;
                Crystal(root, "Crystal seam", new Vector3(offset * 1.87f, height * 0.35f, 0.28f + (i % 3) * 0.25f),
                    new Vector3(0.26f + (i % 3) * 0.12f, height, 0.42f + (i % 3) * 0.1f),
                    Quaternion.Euler(i * 4 - 12f, i * 63, offset * 55f), i % 3 == 0 ? deepOre : ore);
            }

            for (int i = 0; i < 5; i++)
            {
                Crystal(root, "Fractured basalt", new Vector3(-0.9f + i * 0.40f, 0.31f, i % 2 == 0 ? -0.12f : 0.52f),
                    new Vector3(0.88f, 0.61f + i % 2 * 0.24f, 0.79f), Quaternion.Euler(i * 17, i * 46, -i * 28), mineRock);
            }

            return Spot(parent, "mine", label + " · 开采", x, z - 1.10f, icy, 1.65f);
        }

        private static void Lift(Transform parent, float x, float z, Material accent)
        {
            Box(parent, "Lift pad", x, 0.01f, z, 2.8f, 0.055f, 1.68f, deckEdge);
            Box(parent, "Lift inward guide L", x - 1.03f, 0.045f, z, 0.045f, 0.043f, 1.45f, accent);
            Box(parent, "Lift inward guide R", x + 1.03f, 0.045f, z, 0.045f, 0.043f, 1.45f, accent);
            FloorSign(parent, "LIFT", x, z - 0.43f, 1.0f, "#8CBAB9", 0.062f);
        }

        private static void Headframe(Transform parent, float x, float z, Material accent)
        {
            Transform root = Group(parent, "Service gantry", new Vector3(x, 0f, z));
            Box(root, "Gantry station base", 0, 0.22f, 0.25f, 2.45f, 0.44f, 1.35f, shell, true);
            for (int side = -1; side <= 1; side += 2)
            {
                Box(root, "Support upright", side * 1.04f, 0.96f, 0.26f, 0.20f, 1.93f, 0.27f, aluminum);
                Box(root, "Maintenance line", side * 0.92f, 1.33f, 0.10f, 0.07f, 0.73f, 0.043f, accent);
            }
            Box(root, "Pilot beam", 0, 1.91f, 0.26f, 2.44f, 0.27f, 0.35f, rim);
            Box(root, "Beam marker", 0, 1.92f, 0.065f, 1.5f, 0.084f, 0.045f, accent);
        }

        private static void Rail(Transform parent, float x, float z, float length, float yaw)
        {
            Transform root = Group(parent, "Minecart guide rails", new Vector3(x, 0.026f, z));
            root.localRotation = Quaternion.Euler(0, yaw, 0);
            for (int i = 0; i < Mathf.FloorToInt(length / 0.60f); i++)
            {
                Box(root, "Sleeper", 0, 0.02f, -length * 0.5f + 0.35f + i * 0.60f, 1.13f, 0.061f, 0.26f, deckEdge);
            }
            Box(root, "Track A", -0.39f, 0.055f, 0, 0.079f, 0.051f, length, rim);
            Box(root, "Track B", 0.39f, 0.055f, 0, 0.079f, 0.051f, length, rim);
        }

        private static AdventureSpot Booth(Transform parent, float x, float z, string action, string label, string sign, Material accent, float productBias)
        {
            Transform root = Group(parent, sign + " stand", new Vector3(x, 0f, z));
            Box(root, "Shop plinth", 0, 0.14f, -0.03f, 3.66f, 0.28f, 1.35f, deckEdge, true);
            Box(root, "Counter body", 0, 0.49f, -0.13f, 3.39f, 0.69f, 0.99f, shell);
            Box(root, "Counter top", 0, 0.87f, -0.14f, 3.55f, 0.12f, 1.14f, aluminum);
            Box(root, "Counter luminous strip", 0, 0.73f, -0.704f, 2.87f, 0.055f, 0.04f, accent);
            Box(root, "Rear shelf", 0, 0.61f, 0.85f, 3.25f, 1.13f, 0.24f, deckEdge);
            Box(root, "Sign bracket", 0, 1.67f, 0.93f, 3.48f, 0.66f, 0.36f, rim);
            Box(root, "Sign seat", 0, 1.62f, 0.72f, 3.34f, 0.47f, 0.12f, carbon);
            Box(root, "Shop face", 0, 1.94f, 0.67f, 3.48f, 0.076f, 0.088f, accent);
            Sign(root, sign, new Vector3(0, 1.63f, 0.643f), 2.5f, "#F6E5D3", Quaternion.identity);
            Person(root, "Merchant", new Vector3(-0.52f, 0f, 0.42f), 180f, x < 0 ? shell : bronze, accent, false);
            for (int i = 0; i < 3; i++)
            {
                float bx = 0.31f + i * 0.40f;
                if (action == "food-shop")
                {
                    Cylinder(root, "Nutrient capsule", bx, 1.02f, -0.29f, 0.105f, 0.22f, porcelain);
                    Cylinder(root, "Capsule cap", bx, 1.14f, -0.29f, 0.111f, 0.05f, accent);
                }
                else
                {
                    Box(root, "Display stock", bx, 0.975f, productBias * 0.4f, 0.28f, 0.15f, 0.36f, action == "bank" ? bronze : rim);
                    Box(root, "Display stock clamping bolt", bx, 1.065f, productBias * 0.4f, 0.20f, 0.028f, 0.22f, accent);
                }
            }
            return Spot(parent, action, label, x, z - 1.36f, accent, 1.65f);
        }

        private static void Cover(Transform parent, float x, float z, float yaw)
        {
            Transform root = Group(parent, "Ballistic partition", new Vector3(x, 0f, z));
            root.localRotation = Quaternion.Euler(0, yaw, 0);
            Box(root, "Base rail", 0, 0.16f, 0, 2.46f, 0.32f, 0.73f, deckEdge);
            Box(root, "Panel", 0, 0.63f, 0, 2.26f, 1.26f, 0.6f, shell, true);
            Box(root, "Top cap", 0, 1.24f, 0, 2.34f, 0.15f, 0.69f, rim);
            Box(root, "Warning panel", 0, 0.74f, -0.33f, 1.7f, 0.25f, 0.04f, amberPaint);
            for (int i = 0; i < 4; i++)
            {
                Box(root, "Warning chevron", -0.64f + i * 0.40f, 0.74f, -0.357f, 0.085f, 0.23f, 0.026f, deckEdge).localRotation = Quaternion.Euler(0, 0, -30f);
            }
        }

        private static void Rover(Transform parent, Vector3 position, float yaw)
        {
            Transform root = Group(parent, "Mine survey rover", position);
            root.localRotation = Quaternion.Euler(0, yaw, 0);
            for (int side = -1; side <= 1; side += 2)
            {
                Box(root, "Rubber tread", side * 0.46f, 0.17f, 0, 0.23f, 0.30f, 0.92f, carbon);
                for (int i = 0; i < 3; i++)
                {
                    Cylinder(root, "Side hub", side * 0.56f, 0.17f, -0.28f + i * 0.28f, 0.106f, 0.07f, rim).localRotation = Quaternion.Euler(0, 0, 90);
                }
            }
            Box(root, "Survey housing", 0, 0.40f, 0, 0.81f, 0.39f, 0.74f, porcelain);
            Box(root, "Battery case", 0, 0.55f, 0, 0.64f, 0.08f, 0.56f, amberPaint);
            Box(root, "Stereo sensor", 0, 0.67f, 0.22f, 0.56f, 0.17f, 0.23f, shell);
            Box(root, "Stereo emitter", 0, 0.68f, 0.348f, 0.38f, 0.062f, 0.041f, aqua);
        }
    }
}
