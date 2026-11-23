using UnityEngine;

namespace Dungeons2087
{
    public sealed class AdventureRoom : MonoBehaviour
    {
        public Area Area;
        public Transform Spawn;
        public AdventureSpot[] Spots;
        public Transform[] EnemySpawns;
        public Vector2 FloorSize;
        public Color Ambient;
        public Color Fog;
    }
}
