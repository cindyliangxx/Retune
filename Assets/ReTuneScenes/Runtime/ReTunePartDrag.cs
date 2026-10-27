using UnityEngine;
using UnityEngine.EventSystems;

namespace ReTune.Scenes
{
    public sealed class ReTunePartDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        public ReTuneSceneController controller;
        public int part;
        Vector3 home, beforeDrag, pointerOffset;
        Collider2D hitbox;
        SpriteRenderer[] visuals;
        Canvas[] labels;
        int[] spriteOrders, labelOrders;
        bool dragging;
        float removeAt = -1;
        void Awake()
        {
            home = transform.localPosition;
            hitbox = GetComponentInChildren<Collider2D>();
            visuals = GetComponentsInChildren<SpriteRenderer>(true);
            labels = GetComponentsInChildren<Canvas>(true);
            spriteOrders = new int[visuals.Length]; labelOrders = new int[labels.Length];
            for (int i = 0; i < visuals.Length; i++) spriteOrders[i] = visuals[i].sortingOrder;
            for (int i = 0; i < labels.Length; i++) labelOrders[i] = labels[i].sortingOrder;
        }
        public void PlaceInSlot(Collider2D slot)
        {
            removeAt = -1;
            if (dragging) CancelDrag();
            if (slot == null) { transform.localPosition = home; return; }
            Vector3 center = slot.transform.TransformPoint(slot.offset);
            Vector3 ownOffset = hitbox.transform.TransformPoint(hitbox.offset) - transform.position;
            transform.position = new Vector3(center.x - ownOffset.x, center.y - ownOffset.y, transform.position.z);
        }
        public void OnBeginDrag(PointerEventData e)
        {
            if (!controller.CanInteract || e.button != PointerEventData.InputButton.Left) return;
            if (!ReTuneWorldPointer.TryPosition(e.pressEventCamera, e.position, transform.position.z, out var pointer)) return;
            removeAt = -1; dragging = true; beforeDrag = transform.localPosition; pointerOffset = transform.position - pointer;
            hitbox.enabled = false;
            for (int i = 0; i < visuals.Length; i++) visuals[i].sortingOrder = spriteOrders[i] + 1000;
            for (int i = 0; i < labels.Length; i++) labels[i].sortingOrder = labelOrders[i] + 1000;
        }
        public void OnDrag(PointerEventData e)
        {
            if (!dragging) return;
            if (!controller.CanInteract) { CancelDrag(); return; }
            if (ReTuneWorldPointer.TryPosition(e.pressEventCamera, e.position, transform.position.z, out var pointer))
                transform.position = pointer + pointerOffset;
        }
        public void OnEndDrag(PointerEventData e)
        {
            if (!dragging) return;
            CancelDrag();
            if (controller.CanInteract) controller.DropPart(part, e.position, e.pressEventCamera);
        }
        public void CancelDrag()
        {
            if (!dragging) return;
            dragging = false; transform.localPosition = beforeDrag; hitbox.enabled = true;
            for (int i = 0; i < visuals.Length; i++) visuals[i].sortingOrder = spriteOrders[i];
            for (int i = 0; i < labels.Length; i++) labels[i].sortingOrder = labelOrders[i];
        }
        void Update()
        {
            if (!controller.CanInteract) { removeAt = -1; if (dragging) CancelDrag(); }
            if (removeAt >= 0 && Time.unscaledTime >= removeAt)
            { removeAt = -1; if (controller.CanInteract && !dragging) controller.RemovePart(part); }
        }
        void OnDisable() { removeAt = -1; CancelDrag(); }
        void OnApplicationFocus(bool focused) { if (!focused) { removeAt = -1; CancelDrag(); } }
        public void OnPointerClick(PointerEventData e)
        {
            if (!controller.CanInteract || e.button != PointerEventData.InputButton.Left) return;
            if (e.clickCount == 2) { removeAt = -1; controller.DescribePart(part); }
            else if (ReTuneSession.structure == part || ReTuneSession.tone == part) removeAt = Time.unscaledTime + .35f;
        }
    }
}
