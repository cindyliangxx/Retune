using UnityEngine;

namespace ReTune.Scenes
{
    public static class ReTuneWorldPointer
    {
        public static bool TryPosition(Camera camera, Vector2 screenPosition, float planeZ, out Vector3 position)
        {
            position = default;
            if (camera == null) return false;
            var ray = camera.ScreenPointToRay(screenPosition);
            var plane = new Plane(Vector3.forward, new Vector3(0, 0, planeZ));
            if (!plane.Raycast(ray, out float distance)) return false;
            position = ray.GetPoint(distance);
            return true;
        }
    }
}
