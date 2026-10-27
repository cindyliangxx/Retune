using UnityEngine;
using UnityEngine.EventSystems;

namespace ReTune.Scenes
{
    public sealed class ReTuneWorldClick : MonoBehaviour, IPointerClickHandler
    {
        public enum Action { TouchMusicBox, ClearRhythm, ClearTone }
        public ReTuneSceneController controller;
        public Action action;
        public void OnPointerClick(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left || !controller.CanInteract) return;
            if (action == Action.TouchMusicBox) controller.TouchMusicBox();
            else if (action == Action.ClearRhythm) controller.ClearStructure();
            else controller.ClearTone();
        }
    }
}
