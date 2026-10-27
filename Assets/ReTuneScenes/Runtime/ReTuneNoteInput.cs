using UnityEngine;
using UnityEngine.EventSystems;
namespace ReTune.Scenes
{
    public sealed class ReTuneNoteInput : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public ReTuneSceneController controller;
        public void OnPointerDown(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) controller.NoteDown(); }
        public void OnPointerUp(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) controller.NoteRelease(); }
        public void OnPointerExit(PointerEventData e) { controller.NoteRelease(); }
    }
}
