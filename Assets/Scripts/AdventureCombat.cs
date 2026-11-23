using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dungeons2087
{
    public sealed class AdventureCombat : MonoBehaviour
    {
        private const float ShotHeight = 0.65f;
        private const float HeroRadius = 0.42f;
        private const float ShotInterval = 0.15f;
        private const float HurtGrace = 0.82f;

        private enum FoeKind { Crawler, Spitter, Warden }
        private enum FoePhase { Entering, Stalk, Windup, Charge, Recover }

        private sealed class Foe
        {
            public FoeKind Kind;
            public FoePhase Phase;
            public Transform Root;
            public Transform Body;
            public Transform Halo;
            public Transform Lane;
            public Transform Bar;
            public Transform BarFill;
            public Vector3 Direction;
            public float Timer;
            public float Ready;
            public float Radius;
            public int Health;
            public int MaxHealth;
        }

        private sealed class Projectile
        {
            public Transform Shape;
            public Vector3 Position;
            public Vector3 Direction;
            public float Distance;
            public float Speed;
            public float Radius;
            public int Damage;
            public bool Friendly;
        }

        private struct Spark
        {
            public Transform Shape;
            public Vector3 Velocity;
            public float Life;
            public float Duration;
            public Vector3 Scale;
        }

        private readonly List<Foe> foes = new();
        private readonly List<Projectile> projectiles = new();
        private readonly List<Spark> sparks = new();
        private readonly Dictionary<PrimitiveType, Mesh> meshes = new();
        private readonly List<Material> materials = new();
        private readonly RaycastHit[] wallHits = new RaycastHit[16];
        private Transform field;
        private Transform player;
        private Transform[] spawns;
        private DungeonGame game;
        private AudioSource audioSource;
        private AudioClip fireSound;
        private AudioClip impactSound;
        private Mesh ring;
        private Material shell;
        private Material dark;
        private Material coral;
        private Material violet;
        private Material amber;
        private Material mint;
        private Material white;
        private Material warning;
        private Material shadow;
        private float cooldown;
        private float protectedTime;
        private float betweenWaves;
        private float elapsed;
        private float floorY;
        private bool running;

        public int Wave { get; private set; }
        public int MaxWaves => 3;
        public int Enemies => foes.Count;

        public void Begin(Transform hero, Transform[] enemySpawns, DungeonGame model)
        {
            Stop();
            InitializeAssets();
            game = model;
            player = hero;
            spawns = enemySpawns;
            floorY = hero.position.y;
            field = new GameObject("Arena threats").transform;
            field.SetParent(transform, true);
            cooldown = 0f;
            elapsed = 0f;
            protectedTime = 1f;
            betweenWaves = 0f;
            running = true;
            Wave = 0;
            StartWave();
        }

        public void Stop()
        {
            running = false;
            if (field != null)
            {
                field.gameObject.SetActive(false);
                Destroy(field.gameObject);
                field = null;
            }

            foes.Clear();
            projectiles.Clear();
            sparks.Clear();
            player = null;
            game = null;
            Wave = 0;
        }

        public void Step(float dt, Vector3 aimAt, bool fire, bool playerDodging)
        {
            if (!running || !game.IsRealTimeCombat || dt <= 0f) return;

            dt = Mathf.Min(dt, 0.06f);
            elapsed += dt;
            cooldown -= dt;
            protectedTime -= dt;
            Vector3 hero = AtFloor(player.position);
            Vector3 aim = AtFloor(aimAt) - hero;
            if (fire && cooldown <= 0f && aim.sqrMagnitude > 0.025f)
            {
                ShootHero(hero, aim.normalized);
            }

            StepProjectiles(dt, hero, playerDodging);
            if (!running || !game.IsRealTimeCombat) return;

            for (int i = 0; i < foes.Count; i++)
            {
                StepFoe(foes[i], hero, dt, playerDodging);
                if (!running || !game.IsRealTimeCombat) return;
            }

            StepSparks(dt);
            if (foes.Count != 0) return;

            betweenWaves += dt;
            if (Wave < MaxWaves)
            {
                if (betweenWaves >= 1.25f) StartWave();
            }
            else if (betweenWaves >= 0.42f)
            {
                running = false;
                game.CompleteRealTimeCombat();
            }
        }

        private void StartWave()
        {
            Wave++;
            betweenWaves = 0f;
            ClearProjectiles();
            FoeKind[] group = Wave switch
            {
                1 => new[] { FoeKind.Crawler, FoeKind.Crawler, FoeKind.Spitter },
                2 => new[] { FoeKind.Crawler, FoeKind.Spitter, FoeKind.Crawler, FoeKind.Spitter },
                _ => new[] { FoeKind.Warden, FoeKind.Crawler, FoeKind.Spitter, FoeKind.Crawler }
            };
            List<Vector3> points = SpawnPositions();
            for (int i = 0; i < group.Length; i++)
            {
                SpawnFoe(group[i], points[i], i);
            }
        }

        private List<Vector3> SpawnPositions()
        {
            var points = new List<Vector3>();
            Vector3 hero = AtFloor(player.position);
            foreach (Transform mark in spawns ?? System.Array.Empty<Transform>())
            {
                if (mark != null && (AtFloor(mark.position) - hero).sqrMagnitude >= 12f)
                {
                    points.Add(AtFloor(mark.position));
                }
            }

            if (points.Count < 4)
            {
                foreach (Transform mark in spawns ?? System.Array.Empty<Transform>())
                {
                    if (mark != null && !points.Contains(AtFloor(mark.position)))
                    {
                        points.Add(AtFloor(mark.position));
                    }
                }
            }

            for (int i = points.Count; i < 4; i++)
            {
                float angle = (-60f + 40f * i) * Mathf.Deg2Rad;
                points.Add(hero + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * 6.5f);
            }

            if (points.Count > 4 && Wave > 1)
            {
                Vector3 first = points[0];
                points.RemoveAt(0);
                points.Add(first);
            }

            return points;
        }

        private void SpawnFoe(FoeKind kind, Vector3 position, int index)
        {
            bool crawler = kind == FoeKind.Crawler;
            bool warden = kind == FoeKind.Warden;
            Material glow = crawler ? coral : warden ? amber : violet;
            Transform root = Group(kind.ToString(), field, position);
            float radius = warden ? 0.78f : crawler ? 0.51f : 0.58f;
            int health = warden ? 47 : crawler ? 17 : 22;

            Transform body = Group("Sentry body", root, position);
            body.localPosition = Vector3.up * 0.69f;
            body.localScale = Vector3.one * (warden ? 1.29f : 1f);
            Part(body, "Mantle", PrimitiveType.Sphere, new Vector3(0f, 0.03f, -0.07f),
                new Vector3(0.94f, 0.59f, 0.95f), shell);
            Part(body, "Face shroud", PrimitiveType.Cube, new Vector3(0f, 0.06f, 0.36f),
                new Vector3(0.68f, 0.25f, 0.24f), dark);
            Part(body, "Core", PrimitiveType.Cube, new Vector3(0f, 0.11f, 0.49f),
                new Vector3(0.4f, 0.09f, 0.07f), glow);
            Part(body, "Beacon spine", PrimitiveType.Cube, new Vector3(0f, 0.32f, -0.03f),
                new Vector3(0.16f, 0.035f, 0.42f), glow);
            for (int side = -1; side <= 1; side += 2)
            {
                Transform plate = Part(body, crawler ? "Shoulder claw" : "Emitter cowling", PrimitiveType.Cube,
                    new Vector3(side * 0.46f, 0.02f, crawler ? 0.2f : 0.03f),
                    new Vector3(crawler ? 0.27f : 0.24f, 0.23f, crawler ? 0.79f : 0.66f), shell);
                plate.localRotation = Quaternion.Euler(0f, crawler ? -side * 24f : -side * 8f, side * -12f);
                Part(plate, "Inset lamp", PrimitiveType.Cube, new Vector3(0f, 0.19f, 0.28f),
                    new Vector3(0.61f, 0.66f, 0.29f), glow);
                if (crawler)
                {
                    Transform foot = Part(body, "Lateral blade", PrimitiveType.Cube, new Vector3(side * 0.48f, -0.32f, -0.15f),
                        new Vector3(0.56f, 0.11f, 0.19f), dark);
                    foot.localRotation = Quaternion.Euler(0f, side * 24f, side * -22f);
                }
            }

            if (warden)
            {
                Transform crown = Part(body, "Heavy crown", PrimitiveType.Cylinder, new Vector3(0f, 0.34f, -0.14f),
                    new Vector3(0.63f, 0.08f, 0.63f), dark);
                Part(crown, "Power ring", PrimitiveType.Cylinder, new Vector3(0f, 1f, 0f),
                    new Vector3(0.71f, 0.34f, 0.71f), amber);
            }

            Part(root, "Ground silhouette", PrimitiveType.Cylinder, new Vector3(0f, 0.025f, 0f),
                new Vector3(radius * 2.05f, 0.011f, radius * 1.73f), shadow);
            Transform halo = Shape("Warning ring", root, ring, warning);
            halo.localPosition = Vector3.up * 0.053f;
            halo.localScale = new Vector3(radius * 1.63f, 1f, radius * 1.63f);
            Transform lane = Shape("Charge lane", field, MeshFor(PrimitiveType.Cube), warning);
            lane.gameObject.SetActive(false);
            Transform bar = Group("Enemy health", field, position);
            Part(bar, "Health track", PrimitiveType.Cube, Vector3.zero, new Vector3(0.91f, 0.022f, 0.071f), dark);
            Transform barFill = Part(bar, "Health", PrimitiveType.Cube, Vector3.up * 0.019f,
                new Vector3(0.88f, 0.02f, 0.046f), glow);
            bar.gameObject.SetActive(false);

            foes.Add(new Foe
            {
                Root = root,
                Body = body,
                Halo = halo,
                Lane = lane,
                Bar = bar,
                BarFill = barFill,
                Kind = kind,
                Phase = FoePhase.Entering,
                Radius = radius,
                Timer = 0.56f + index * 0.16f,
                Ready = 0.63f + index * 0.25f,
                Health = health,
                MaxHealth = health
            });
        }

        private void StepFoe(Foe foe, Vector3 hero, float dt, bool playerDodging)
        {
            Vector3 origin = foe.Root.position;
            Vector3 delta = hero - origin;
            float distance = delta.magnitude;
            Vector3 toward = distance > 0.004f ? delta / distance : foe.Root.forward;
            bool close = foe.Kind == FoeKind.Crawler;
            bool heavy = foe.Kind == FoeKind.Warden;
            foe.Timer -= dt;
            foe.Ready -= dt;
            foe.Body.localPosition = Vector3.up * (0.68f + Mathf.Sin(elapsed * (close ? 9f : 5f) + origin.x) * 0.075f);

            if (foe.Phase is FoePhase.Windup or FoePhase.Charge)
            {
                Face(foe, foe.Direction, dt);
            }
            else
            {
                Face(foe, toward, dt);
            }

            switch (foe.Phase)
            {
                case FoePhase.Entering:
                    if (foe.Timer <= 0f)
                    {
                        foe.Halo.gameObject.SetActive(false);
                        foe.Phase = FoePhase.Stalk;
                    }
                    break;
                case FoePhase.Stalk:
                    float attackRange = close ? 3.75f : heavy ? 7.4f : 7.15f;
                    if (distance <= attackRange && foe.Ready <= 0f && !Blocked(origin, toward, distance, 0.12f, out _))
                    {
                        BeginAttack(foe, toward, distance);
                    }
                    else
                    {
                        Vector3 travel = close || distance > attackRange - 0.48f ? toward :
                            distance < 3.3f ? -toward : Vector3.Cross(Vector3.up, toward) * (heavy ? 0.26f : 0.58f);
                        float speed = close ? 3.35f : heavy ? 2.02f : 2.45f;
                        WalkFoe(foe, travel, speed * dt);
                    }
                    break;
                case FoePhase.Windup:
                    float haloSize = foe.Radius * (1.37f + Mathf.Max(0f, foe.Timer) * 0.8f);
                    foe.Halo.localScale = new Vector3(haloSize, 1f, haloSize);
                    if (foe.Timer <= 0f)
                    {
                        foe.Lane.gameObject.SetActive(false);
                        foe.Halo.gameObject.SetActive(false);
                        if (close)
                        {
                            foe.Phase = FoePhase.Charge;
                            foe.Timer = 0.35f;
                        }
                        else
                        {
                            ShootFoe(foe);
                            Recover(foe);
                        }
                    }
                    break;
                case FoePhase.Charge:
                    float length = 10.4f * dt;
                    bool wall = Blocked(origin, foe.Direction, length, foe.Radius * 0.65f, out float wallDistance);
                    Vector3 tip = origin + foe.Direction * (wall ? Mathf.Max(0f, wallDistance - 0.03f) : length);
                    foe.Root.position = tip;
                    if (!playerDodging && Contact(origin, tip, hero, foe.Radius + HeroRadius, out _))
                    {
                        Recover(foe);
                        ApplyHeroDamage(4);
                    }
                    else if (wall || foe.Timer <= 0f)
                    {
                        Recover(foe);
                    }
                    break;
                case FoePhase.Recover:
                    if (foe.Timer <= 0f) foe.Phase = FoePhase.Stalk;
                    break;
            }

            if (foe.Bar.gameObject.activeSelf)
            {
                foe.Bar.position = foe.Root.position + new Vector3(0f, 0.077f, -foe.Radius - 0.19f);
            }
        }

        private void BeginAttack(Foe foe, Vector3 toward, float distance)
        {
            bool charge = foe.Kind == FoeKind.Crawler;
            foe.Phase = FoePhase.Windup;
            foe.Direction = toward;
            foe.Timer = charge ? 0.49f : 0.71f;
            foe.Halo.gameObject.SetActive(true);
            float length = charge ? 3.7f : Mathf.Min(distance + 1f, 8.9f);
            if (Blocked(foe.Root.position, toward, length, 0.12f, out float wallDistance)) length = wallDistance;
            SetLine(foe.Lane, foe.Root.position + Vector3.up * 0.06f, toward, length, charge ? 0.28f : 0.082f, 0.02f);
            foe.Lane.gameObject.SetActive(true);
        }

        private static void Recover(Foe foe)
        {
            foe.Phase = FoePhase.Recover;
            foe.Timer = foe.Kind == FoeKind.Crawler ? 0.7f : 0.8f;
            foe.Ready = foe.Kind == FoeKind.Warden ? 1.44f : 1.77f;
        }

        private void WalkFoe(Foe foe, Vector3 intention, float distance)
        {
            if (intention.sqrMagnitude < 0.005f) return;
            Vector3 origin = foe.Root.position;
            Vector3 repel = Vector3.zero;
            foreach (Foe other in foes)
            {
                if (other == foe) continue;
                Vector3 offset = origin - other.Root.position;
                float margin = foe.Radius + other.Radius + 0.3f;
                float distanceSquared = offset.sqrMagnitude;
                if (distanceSquared > 0.0001f && distanceSquared < margin * margin)
                {
                    repel += offset.normalized * (margin - Mathf.Sqrt(distanceSquared));
                }
            }

            Vector3 direction = (intention + repel * 1.3f).normalized;
            float buffer = foe.Radius * 0.78f;
            if (Blocked(origin, direction, distance + 0.065f, buffer, out _))
            {
                Vector3 right = Vector3.Cross(Vector3.up, direction);
                Vector3 side = (direction + right * 1.55f).normalized;
                if (Blocked(origin, side, distance + 0.1f, buffer, out _))
                {
                    side = (direction - right * 1.55f).normalized;
                    if (Blocked(origin, side, distance + 0.1f, buffer, out _)) return;
                }
                direction = side;
            }
            foe.Root.position = origin + direction * distance;
        }

        private void ShootHero(Vector3 origin, Vector3 direction)
        {
            cooldown = ShotInterval;
            const float offset = 0.54f;
            if (Blocked(origin, direction, offset, 0.065f, out float hit))
            {
                Burst(origin + direction * hit + Vector3.up * ShotHeight, mint, 3, 0.17f);
                audioSource?.PlayOneShot(fireSound, 0.46f);
                return;
            }

            Vector3 tip = origin + direction * offset;
            Launch(tip, direction, true, game.AttackPower, 23.5f, 14.5f, 0.085f, mint);
            Burst(tip + Vector3.up * ShotHeight, white, 3, 0.11f);
            audioSource?.PlayOneShot(fireSound, 0.46f);
        }

        private void ShootFoe(Foe foe)
        {
            bool heavy = foe.Kind == FoeKind.Warden;
            Material color = heavy ? amber : violet;
            int damage = heavy ? 5 : 3;
            int spread = heavy ? 1 : 0;
            for (int i = -spread; i <= spread; i++)
            {
                Vector3 direction = Quaternion.AngleAxis(i * 15f, Vector3.up) * foe.Direction;
                Vector3 origin = foe.Root.position + direction * foe.Radius * 0.72f;
                Launch(origin, direction, false, damage, heavy ? 6.5f : 7.9f, 12f, heavy ? 0.18f : 0.14f, color);
            }
            Burst(foe.Root.position + foe.Direction * foe.Radius + Vector3.up * ShotHeight, color, 4, 0.16f);
        }

        private void Launch(Vector3 origin, Vector3 direction, bool friendly, int damage, float speed, float distance, float radius, Material color)
        {
            Transform visual = Shape(friendly ? "Player shot" : "Hostile spore", field, MeshFor(PrimitiveType.Cube), color);
            var projectile = new Projectile
            {
                Shape = visual,
                Position = origin,
                Direction = direction,
                Friendly = friendly,
                Damage = damage,
                Speed = speed,
                Distance = distance,
                Radius = radius
            };
            UpdateProjectile(projectile);
            projectiles.Add(projectile);
        }

        private void StepProjectiles(float dt, Vector3 hero, bool playerDodging)
        {
            for (int i = projectiles.Count - 1; i >= 0; i--)
            {
                Projectile shot = projectiles[i];
                float advance = Mathf.Min(shot.Speed * dt, shot.Distance);
                Vector3 end = shot.Position + shot.Direction * advance;
                bool hitsWall = Blocked(shot.Position, shot.Direction, advance, shot.Radius, out float hitDistance);
                if (hitsWall) end = shot.Position + shot.Direction * hitDistance;
                shot.Distance -= advance;
                if (shot.Friendly)
                {
                    Foe target = FirstFoe(shot.Position, end, shot.Radius);
                    if (target != null)
                    {
                        RemoveProjectile(i);
                        HurtFoe(target, shot.Damage);
                        if (foes.Count == 0)
                        {
                            ClearProjectiles();
                            return;
                        }
                        continue;
                    }
                }
                else if (!playerDodging && Contact(shot.Position, end, hero, HeroRadius + shot.Radius, out _))
                {
                    RemoveProjectile(i);
                    ApplyHeroDamage(shot.Damage);
                    if (!running || !game.IsRealTimeCombat) return;
                    continue;
                }

                if (hitsWall || shot.Distance <= 0f)
                {
                    if (hitsWall)
                    {
                        Burst(end + Vector3.up * ShotHeight, shot.Friendly ? mint : violet, 3, 0.18f);
                    }
                    RemoveProjectile(i);
                    continue;
                }

                shot.Position = end;
                UpdateProjectile(shot);
            }
        }

        private Foe FirstFoe(Vector3 start, Vector3 end, float radius)
        {
            Foe target = null;
            float nearest = float.MaxValue;
            foreach (Foe foe in foes)
            {
                if (foe.Phase == FoePhase.Entering) continue;
                if (!Contact(start, end, foe.Root.position, foe.Radius + radius, out float hit) || hit >= nearest) continue;
                target = foe;
                nearest = hit;
            }
            return target;
        }

        private void HurtFoe(Foe foe, int damage)
        {
            foe.Health = Mathf.Max(0, foe.Health - damage);
            bool down = foe.Health == 0;
            Material color = foe.Kind == FoeKind.Crawler ? coral : foe.Kind == FoeKind.Warden ? amber : violet;
            Burst(foe.Body.position, down ? color : white, down ? 13 : 5, down ? 0.38f : 0.19f);
            audioSource?.PlayOneShot(impactSound, down ? 0.74f : 0.45f);
            if (down)
            {
                foes.Remove(foe);
                Remove(foe.Root);
                Remove(foe.Lane);
                Remove(foe.Bar);
                return;
            }

            float fill = (float)foe.Health / foe.MaxHealth;
            foe.BarFill.localScale = new Vector3(0.88f * fill, 0.02f, 0.046f);
            foe.BarFill.localPosition = new Vector3(-0.44f * (1f - fill), 0.019f, 0f);
            foe.Bar.gameObject.SetActive(true);
        }

        private void ApplyHeroDamage(int damage)
        {
            if (protectedTime > 0f) return;

            protectedTime = HurtGrace;
            Burst(AtFloor(player.position) + Vector3.up * ShotHeight, coral, 11, 0.31f);
            game.TakeRealTimeHit(damage);
        }

        private static bool Contact(Vector3 start, Vector3 end, Vector3 center, float radius, out float hit)
        {
            Vector3 travel = end - start;
            Vector3 relative = start - center;
            float difference = relative.sqrMagnitude - radius * radius;
            if (difference <= 0f)
            {
                hit = 0f;
                return true;
            }

            float length = travel.sqrMagnitude;
            float b = Vector3.Dot(relative, travel);
            float discriminant = b * b - length * difference;
            hit = 0f;
            if (length < 0.00001f || discriminant < 0f || b > 0f) return false;
            hit = (-b - Mathf.Sqrt(discriminant)) / length;
            return hit >= 0f && hit <= 1f;
        }

        private bool Blocked(Vector3 start, Vector3 direction, float maxDistance, float radius, out float distance)
        {
            distance = maxDistance;
            if (maxDistance <= 0.0001f) return false;
            int count = Physics.SphereCastNonAlloc(start + Vector3.up * ShotHeight, radius, direction, wallHits,
                maxDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = wallHits[i];
                if (hit.transform == player || hit.transform.IsChildOf(player)) continue;
                if (hit.distance > distance) continue;
                distance = hit.distance;
                found = true;
            }
            return found;
        }

        private void Burst(Vector3 origin, Material material, int count, float duration)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 direction = Random.onUnitSphere;
                direction.y = Mathf.Abs(direction.y) * 0.65f + 0.07f;
                Transform glow = Shape("Impact fleck", field, MeshFor(PrimitiveType.Cube), material);
                glow.position = origin;
                glow.rotation = Random.rotation;
                Vector3 size = new(0.052f, 0.052f, Random.Range(0.087f, 0.17f));
                glow.localScale = size;
                sparks.Add(new Spark
                {
                    Shape = glow,
                    Life = duration,
                    Duration = duration,
                    Scale = size,
                    Velocity = direction * Random.Range(2.4f, 5.9f)
                });
            }
        }

        private void StepSparks(float dt)
        {
            for (int i = sparks.Count - 1; i >= 0; i--)
            {
                Spark spark = sparks[i];
                spark.Life -= dt;
                if (spark.Life <= 0f)
                {
                    Remove(spark.Shape);
                    sparks.RemoveAt(i);
                    continue;
                }
                spark.Shape.position += spark.Velocity * dt;
                spark.Shape.localScale = spark.Scale * (spark.Life / spark.Duration);
                spark.Velocity += Vector3.down * (dt * 7f);
                sparks[i] = spark;
            }
        }

        private void ClearProjectiles()
        {
            foreach (Projectile projectile in projectiles) Remove(projectile.Shape);
            projectiles.Clear();
        }

        private void RemoveProjectile(int index)
        {
            Remove(projectiles[index].Shape);
            projectiles.RemoveAt(index);
        }

        private Vector3 AtFloor(Vector3 point) => new(point.x, floorY, point.z);

        private static void Face(Foe foe, Vector3 direction, float dt)
        {
            foe.Root.rotation = Quaternion.Slerp(foe.Root.rotation, Quaternion.LookRotation(direction, Vector3.up), 1f - Mathf.Exp(-dt * 14f));
        }

        private static void UpdateProjectile(Projectile projectile)
        {
            float length = projectile.Friendly ? 0.53f : 0.32f;
            SetLine(projectile.Shape, projectile.Position + Vector3.up * ShotHeight - projectile.Direction * (length * 0.72f),
                projectile.Direction, length, projectile.Radius * (projectile.Friendly ? 1.0f : 1.6f), 0.077f);
        }

        private static void SetLine(Transform line, Vector3 origin, Vector3 direction, float length, float width, float height)
        {
            line.SetPositionAndRotation(origin + direction * (length * 0.5f), Quaternion.LookRotation(direction, Vector3.up));
            line.localScale = new Vector3(width, height, length);
        }

        private static Transform Group(string name, Transform parent, Vector3 position)
        {
            Transform item = new GameObject(name).transform;
            item.SetParent(parent, true);
            item.position = position;
            return item;
        }

        private Transform Shape(string name, Transform parent, Mesh mesh, Material material)
        {
            var item = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            item.transform.SetParent(parent, false);
            item.GetComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = item.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return item.transform;
        }

        private Transform Part(Transform parent, string name, PrimitiveType primitive, Vector3 position, Vector3 scale, Material material)
        {
            Transform item = Shape(name, parent, MeshFor(primitive), material);
            item.localPosition = position;
            item.localScale = scale;
            return item;
        }

        private Mesh MeshFor(PrimitiveType type)
        {
            if (meshes.TryGetValue(type, out Mesh mesh)) return mesh;
            GameObject sample = GameObject.CreatePrimitive(type);
            mesh = sample.GetComponent<MeshFilter>().sharedMesh;
            sample.SetActive(false);
            Destroy(sample);
            meshes.Add(type, mesh);
            return mesh;
        }

        private void InitializeAssets()
        {
            if (ring != null) return;
            audioSource = GetComponent<AudioSource>();
            fireSound = BuildCue("Pulse rifle", 0.083f, 225f, 0.36f);
            impactSound = BuildCue("Shield crackle", 0.102f, 450f, 0.24f);
            Shader standard = Shader.Find("Standard");
            shell = Surface(standard, "Sentry shell", new Color32(93, 101, 128, 255), 0f);
            dark = Surface(standard, "Gunmetal", new Color32(25, 30, 46, 255), 0f);
            shadow = Surface(standard, "Drone shadow", new Color32(19, 26, 34, 255), 0f);
            coral = Surface(standard, "Crawler coral", new Color32(243, 93, 107, 255), 1.14f);
            violet = Surface(standard, "Spore violet", new Color32(214, 133, 255, 255), 1.16f);
            amber = Surface(standard, "Warden gold", new Color32(255, 191, 108, 255), 1.11f);
            warning = Surface(standard, "Warning amber", new Color32(229, 145, 67, 255), 0.79f);
            mint = Surface(standard, "Rifle beam", new Color32(103, 245, 215, 255), 1.17f);
            white = Surface(standard, "Impact flash", new Color32(237, 255, 239, 255), 1.19f);
            ring = BuildRing();
            MeshFor(PrimitiveType.Cube);
            MeshFor(PrimitiveType.Cylinder);
            MeshFor(PrimitiveType.Sphere);
        }

        private Material Surface(Shader shader, string title, Color color, float glow)
        {
            var material = new Material(shader) { name = title, color = color };
            material.SetFloat("_Glossiness", 0.3f);
            if (glow > 0f)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * glow);
            }
            materials.Add(material);
            return material;
        }

        private static Mesh BuildRing()
        {
            const int segments = 32;
            var vertices = new Vector3[segments * 2];
            var indices = new int[segments * 6];
            for (int i = 0; i < segments; i++)
            {
                float angle = Mathf.PI * 2f * i / segments;
                Vector3 edge = new(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                int current = i * 2;
                int next = (current + 2) % vertices.Length;
                vertices[current] = edge * 0.83f;
                vertices[current + 1] = edge;
                int face = i * 6;
                indices[face] = current;
                indices[face + 1] = current + 1;
                indices[face + 2] = next + 1;
                indices[face + 3] = current;
                indices[face + 4] = next + 1;
                indices[face + 5] = next;
            }

            var result = new Mesh { name = "Charge marker", vertices = vertices, triangles = indices };
            result.RecalculateNormals();
            result.RecalculateBounds();
            return result;
        }

        private static AudioClip BuildCue(string name, float length, float frequency, float noiseVolume)
        {
            const int rate = 22050;
            int samples = Mathf.RoundToInt(rate * length);
            var data = new float[samples];
            var noise = new System.Random(85);
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)rate;
                float remaining = 1f - t / length;
                float envelope = Mathf.Min(1f, t * 1400f) * remaining * remaining;
                float chirp = Mathf.Sin(2f * Mathf.PI * frequency * t * (0.64f + remaining * 0.36f)) * 0.29f;
                data[i] = (chirp + ((float)noise.NextDouble() * 2f - 1f) * noiseVolume) * envelope;
            }

            AudioClip clip = AudioClip.Create(name, samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private void Remove(Transform item)
        {
            item.gameObject.SetActive(false);
            Destroy(item.gameObject);
        }

        private void OnDestroy()
        {
            Stop();
            foreach (Material material in materials) Destroy(material);
            if (ring != null) Destroy(ring);
            if (fireSound != null) Destroy(fireSound);
            if (impactSound != null) Destroy(impactSound);
        }
    }
}
