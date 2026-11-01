using UnityEngine;
using UnityEngine.EventSystems;

namespace ReTune.Scenes
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class ReTuneWindingKey : MonoBehaviour
    {
        public ReTunePartsArt musicBox;
        SpriteRenderer visual;
        void Awake() { visual = GetComponent<SpriteRenderer>(); }
        void Update()
        {
            if (musicBox == null || musicBox.controller == null || !Input.GetMouseButtonDown(0)) return;
            var c = musicBox.controller;
            if (!c.CanInteract || (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())) return;
            if (ReTuneWorldPointer.TryPosition(c.worldCamera, Input.mousePosition, transform.position.z, out var point)
                && visual.bounds.Contains(new Vector3(point.x, point.y, visual.bounds.center.z)))
                musicBox.BeginWinding();
        }
    }
}
