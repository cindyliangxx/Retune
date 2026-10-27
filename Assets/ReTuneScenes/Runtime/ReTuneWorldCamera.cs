using UnityEngine;
using UnityEngine.UI;

namespace ReTune.Scenes
{
    [ExecuteAlways, RequireComponent(typeof(Camera))]
    public sealed class ReTuneWorldCamera : MonoBehaviour
    {
        public CanvasScaler hudScaler;
        public float pixelsPerUnit = 100;
        Camera worldCamera;
        void OnEnable() { Apply(); }
        void LateUpdate() { Apply(); }
        void OnValidate() { Apply(); }
        void Apply()
        {
            if (worldCamera == null) worldCamera = GetComponent<Camera>();
            if (worldCamera == null || hudScaler == null || pixelsPerUnit <= 0) return;
            var reference = hudScaler.referenceResolution;
            float width = worldCamera.pixelWidth, height = worldCamera.pixelHeight;
            if (width <= 0 || height <= 0 || reference.x <= 0 || reference.y <= 0) return;
            float scale = Mathf.Pow(width / reference.x, 1 - hudScaler.matchWidthOrHeight)
                * Mathf.Pow(height / reference.y, hudScaler.matchWidthOrHeight);
            worldCamera.orthographic = true;
            worldCamera.orthographicSize = height / scale / pixelsPerUnit / 2;
            var position = transform.position;
            position.x = (width / scale - reference.x) / pixelsPerUnit / 2;
            position.y = (reference.y - height / scale) / pixelsPerUnit / 2;
            transform.position = position;
        }
    }
}
