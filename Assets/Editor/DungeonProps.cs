using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Dungeons2087
{
    public static partial class DungeonSceneBuilder
    {
        private static Transform MakePlayer()
        {
            var player = new GameObject("Delver", typeof(CharacterController));
            var controller = player.GetComponent<CharacterController>();
            controller.height = 1.85f;
            controller.radius = 0.36f;
            controller.center = new Vector3(0, 0.925f, 0);
            controller.stepOffset = 0.23f;
            controller.skinWidth = 0.04f;
            controller.minMoveDistance = 0;

            Person(player.transform, "Rig", Vector3.zero, 0f, porcelain, aqua, true);
            Octagon(player.transform, "Player suit status", new Vector3(0, 0.044f, 0), 0.49f, 0.045f, aqua, 12, 3);
            return player.transform;
        }

        private static Transform Person(Transform parent, string name, Vector3 position, float yaw, Material suit, Material glow, bool player)
        {
            Transform avatar = Group(parent, name, position);
            avatar.localRotation = Quaternion.Euler(0, yaw, 0);
            if (!player)
            {
                avatar.localScale = Vector3.one * 0.88f;
            }

            Box(avatar, "Upper shell", 0, 0.94f, 0, 0.67f, 0.52f, 0.49f, suit);
            Box(avatar, "Chest sill", 0, 0.67f, 0.03f, 0.56f, 0.14f, 0.41f, deckEdge);
            Box(avatar, "Chest plate", 0, 0.96f, 0.25f, 0.54f, 0.31f, 0.07f, player ? amberPaint : rim);
            Box(avatar, "Chest transponder", 0.15f, 1.0f, 0.297f, 0.18f, 0.088f, 0.041f, glow);
            Box(avatar, "Filter pack", 0, 0.98f, -0.30f, 0.51f, 0.43f, 0.28f, deckEdge);
            Box(avatar, "Pack cap", 0, 1.21f, -0.31f, 0.46f, 0.09f, 0.24f, player ? amberPaint : aluminum);
            Box(avatar, "Pack handle", 0, 1.29f, -0.31f, 0.21f, 0.065f, 0.1f, rim);
            for (int i = 0; i < 3; i++)
            {
                Box(avatar, "Pack vent", -0.12f + 0.12f * i, 0.99f, -0.448f, 0.058f, 0.22f, 0.022f, rim);
            }

            Box(avatar, "Collar", 0, 1.21f, 0.012f, 0.44f, 0.11f, 0.43f, deckEdge);
            Orb(avatar, "Air seal helmet", 0, 1.51f, 0.045f, 0.73f, 0.62f, 0.66f, suit);
            Orb(avatar, "Face shield", 0, 1.54f, 0.257f, 0.59f, 0.32f, 0.29f, carbon);
            Box(avatar, "Helmet sensor", 0, 1.58f, 0.400f, 0.45f, 0.063f, 0.027f, glow);
            Box(avatar, "Helmet crown stripe", 0.03f, 1.812f, -0.025f, 0.13f, 0.028f, 0.12f, player ? amberPaint : glow);
            for (int side = -1; side <= 1; side += 2)
            {
                string modifier = side < 0 ? "Left" : "Right";
                Cylinder(avatar, "Helmet actuator", side * 0.355f, 1.505f, 0.045f, 0.12f, 0.11f, rim).localRotation = Quaternion.Euler(0, 0, 90);
                Transform arm = Group(avatar, modifier + "Arm", new Vector3(side * 0.365f, 1.10f, 0f));
                arm.localRotation = Quaternion.Euler(player ? (side > 0 ? -34f : -7f) : -4f, 0, side * 8f);
                Box(arm, "Shoulder guard", side * 0.035f, -0.07f, 0, 0.27f, 0.24f, 0.39f, player ? amberPaint : suit);
                Box(arm, "Sleeve", side * 0.05f, -0.24f, 0.014f, 0.23f, 0.32f, 0.26f, rim);
                Box(arm, "Gauntlet", side * 0.05f, -0.42f, 0.052f, 0.21f, 0.16f, 0.26f, deckEdge);
                if (player && side > 0)
                {
                    Transform emitter = Group(arm, "Mining pulse emitter", new Vector3(0.03f, -0.44f, 0.13f));
                    emitter.localRotation = Quaternion.Euler(34f, 0, 0);
                    Box(emitter, "Alloy casing", 0, 0.10f, 0.21f, 0.25f, 0.20f, 0.55f, rim);
                    Box(emitter, "Status rail", 0, 0.21f, 0.31f, 0.13f, 0.035f, 0.32f, amber);
                    Box(emitter, "Pulse socket", 0, 0.10f, 0.49f, 0.13f, 0.12f, 0.06f, carbon);
                }

                Transform leg = Group(avatar, modifier + "Leg", new Vector3(side * 0.155f, 0.64f, 0));
                Box(leg, "Suit leg", 0, -0.18f, 0, 0.24f, 0.37f, 0.31f, suit);
                Box(leg, "Shin guard", 0, -0.38f, 0.072f, 0.23f, 0.28f, 0.25f, rim);
                Box(leg, "Work boot", 0, -0.515f, 0.06f, 0.27f, 0.22f, 0.40f, deckEdge);
                Box(leg, "Sole", 0, -0.624f, 0.07f, 0.27f, 0.035f, 0.4f, carbon);
            }

            return avatar;
        }

        private static void CrateStack(Transform parent, float x, float z, float scale, Material accent)
        {
            Crate(parent, x, 0f, z, scale, 3f, accent, true);
            Crate(parent, x + 0.78f * scale, 0f, z + 0.41f * scale, scale * 0.65f, 6f, accent, true);
            Crate(parent, x + 0.10f * scale, 0.60f * scale, z - 0.04f * scale, scale * 0.67f, -8f, accent, false);
        }

        private static void Crate(Transform parent, float x, float y, float z, float scale, float yaw, Material accent, bool collider)
        {
            Transform root = Group(parent, "Dock crate", new Vector3(x, y, z));
            root.localRotation = Quaternion.Euler(0, yaw, 0);
            root.localScale = Vector3.one * scale;
            Box(root, "Crate base", 0, 0.30f, 0, 0.9f, 0.60f, 0.77f, shell, collider);
            Box(root, "Crate lip", 0, 0.57f, 0, 0.91f, 0.075f, 0.79f, rim);
            Box(root, "Crate lid", 0, 0.62f, 0, 0.73f, 0.06f, 0.59f, aluminum);
            Box(root, "Crate latch", 0, 0.51f, -0.408f, 0.20f, 0.105f, 0.046f, accent);
            Box(root, "Crate lid seam", 0, 0.653f, -0.05f, 0.59f, 0.018f, 0.04f, deckEdge);
        }

        private static void Route(Transform parent, Vector2 start, Vector2 end, Material accent)
        {
            Vector2 direction = end - start;
            Vector2 center = (start + end) * 0.5f;
            float length = direction.magnitude;
            Transform route = Group(parent, "Guidance line", new Vector3(center.x, 0.023f, center.y));
            route.localRotation = Quaternion.Euler(0, Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg, 0);
            Box(route, "Line underlay", 0f, 0f, 0f, 0.19f, 0.026f, length, deckEdge);
            for (int i = 0; i < Mathf.FloorToInt(length / 0.88f); i++)
            {
                Box(route, "Line segment", 0f, 0.015f, -length * 0.5f + 0.43f + i * 0.88f, 0.042f, 0.016f, 0.56f, accent);
            }
        }

        private static void Octagon(Transform parent, string name, Vector3 center, float radius, float width,
            Material material, int sides, int stride = 1)
        {
            Transform ring = Group(parent, name, center);
            float halfAngle = Mathf.PI / sides;
            float halfChord = radius * Mathf.Sin(halfAngle);
            float offset = radius * Mathf.Cos(halfAngle);
            for (int i = 0; i < sides; i += stride)
            {
                float angle = (i + 0.5f) * 2f * Mathf.PI / sides;
                Transform part = Box(ring, "Bezel segment", offset * Mathf.Sin(angle), 0, offset * Mathf.Cos(angle),
                    halfChord * (stride == 1 ? 2.02f : 1.65f), 0.029f, width, material);
                part.localRotation = Quaternion.Euler(0, angle * Mathf.Rad2Deg, 0);
                part.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        private static void FloorSign(Transform parent, string text, float x, float z, float width, string color, float height = 0.045f)
        {
            Sign(parent, text, new Vector3(x, height, z), width, color, Quaternion.Euler(90, 0, 0));
        }

        private static void Sign(Transform parent, string text, Vector3 position, float width, string color, Quaternion rotation)
        {
            Transform root = Group(parent, text, position);
            root.localRotation = rotation;
            var sign = root.gameObject.AddComponent<TextMesh>();
            sign.font = signFont;
            sign.fontSize = 92;
            sign.characterSize = width * 0.1f / Mathf.Max(4f, text.Length);
            sign.anchor = TextAnchor.MiddleCenter;
            sign.alignment = TextAlignment.Center;
            sign.text = text;
            sign.color = ColorOf(color);
            var renderer = root.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = signFont.material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private static void Point(Transform parent, string name, float x, float y, float z, string color, float intensity, float range)
        {
            var light = Group(parent, name, new Vector3(x, y, z)).gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = ColorOf(color);
            light.intensity = intensity;
            light.range = range;
        }

        private static Transform Group(Transform parent, string name, Vector3 position)
        {
            var item = new GameObject(name);
            item.transform.SetParent(parent, false);
            item.transform.localPosition = position;
            return item.transform;
        }

        private static Transform Box(Transform parent, string name, float x, float y, float z, float w, float h, float d,
            Material material, bool collider = false)
        {
            return Shape(parent, name, new Vector3(x, y, z), new Vector3(w, h, d), material, PrimitiveType.Cube, collider);
        }

        private static Transform Cylinder(Transform parent, string name, float x, float y, float z, float radius, float height, Material material)
        {
            return Shape(parent, name, new Vector3(x, y, z), new Vector3(radius * 2f, height * 0.5f, radius * 2f), material, PrimitiveType.Cylinder);
        }

        private static Transform Orb(Transform parent, string name, float x, float y, float z, float w, float h, float d, Material material)
        {
            return Shape(parent, name, new Vector3(x, y, z), new Vector3(w, h, d), material, PrimitiveType.Sphere);
        }

        private static Transform Shape(Transform parent, string name, Vector3 position, Vector3 scale,
            Material material, PrimitiveType primitive, bool collider = false)
        {
            GameObject item = GameObject.CreatePrimitive(primitive);
            var transform = item.transform;
            item.name = name;
            transform.SetParent(parent, false);
            transform.localPosition = position;
            transform.localScale = scale;
            item.GetComponent<MeshRenderer>().sharedMaterial = material;
            if (!collider)
            {
                Object.DestroyImmediate(item.GetComponent<Collider>());
            }
            return transform;
        }

        private static void Crystal(Transform parent, string name, Vector3 position, Vector3 scale, Quaternion rotation, Material material)
        {
            Transform shape = Group(parent, name, position);
            shape.localScale = scale;
            shape.localRotation = rotation;
            shape.gameObject.AddComponent<MeshFilter>().sharedMesh = crystalMesh;
            shape.gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static Mesh LoadCrystalMesh()
        {
            string path = ArtPath + "/Faceted shard.asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null)
            {
                return mesh;
            }

            mesh = new Mesh { name = "Faceted shard" };
            var points = new List<Vector3>();
            var triangles = new List<int>();
            const int sides = 6;
            for (int i = 0; i < sides; i++)
            {
                float a = i * 2f * Mathf.PI / sides;
                float b = (i + 1f) * 2f * Mathf.PI / sides;
                Vector3 lowerA = new(Mathf.Cos(a) * 0.32f, -0.65f, Mathf.Sin(a) * 0.32f);
                Vector3 lowerB = new(Mathf.Cos(b) * 0.32f, -0.65f, Mathf.Sin(b) * 0.32f);
                Vector3 upperA = new(Mathf.Cos(a) * 0.5f, 0.28f, Mathf.Sin(a) * 0.5f);
                Vector3 upperB = new(Mathf.Cos(b) * 0.5f, 0.28f, Mathf.Sin(b) * 0.5f);
                AddFace(points, triangles, lowerA, upperA, lowerB);
                AddFace(points, triangles, upperA, upperB, lowerB);
                AddFace(points, triangles, upperA, new Vector3(0.09f, 0.8f, 0.05f), upperB);
                AddFace(points, triangles, lowerA, lowerB, new Vector3(0f, -0.65f, 0f));
            }

            mesh.SetVertices(points);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static void AddFace(List<Vector3> vertices, List<int> indices, Vector3 a, Vector3 b, Vector3 c)
        {
            int first = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            indices.Add(first);
            indices.Add(first + 1);
            indices.Add(first + 2);
        }

        private static Color ColorOf(string expression)
        {
            ColorUtility.TryParseHtmlString(expression, out Color color);
            return color;
        }
    }
}
