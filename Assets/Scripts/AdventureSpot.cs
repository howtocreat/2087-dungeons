using UnityEngine;

namespace Dungeons2087
{
    public sealed class AdventureSpot : MonoBehaviour
    {
        public string ActionId;
        public string Label;
        public Transform PromptAnchor;
        [Min(0.3f)] public float Radius = 2f;
    }
}
