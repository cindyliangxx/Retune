using UnityEngine;
using UnityEngine.EventSystems;

namespace ReTune.Scenes
{
    public sealed class ReTunePartDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        public ReTuneSceneController controller;
        public int part;
        Vector3 home, beforeDrag, pointerOffset;
        public Vector3 OriginalScale { get; private set; } // 新增
        Collider2D hitbox;
        SpriteRenderer[] visuals;
        Canvas[] labels;
        int[] spriteOrders, labelOrders;
        bool dragging;
        public bool IsDragging => dragging;
        Vector3 homeScale;
        Quaternion homeRotation;
        float removeAt = -1;
        Transform worldParent;
        Transform rollerVisual;
        Vector3 rollerHomeScale, rollerHomePosition;
        GearRotator gearDriver;
        Transform defaultStateNode;
        Vector3 defaultStateHomeScale;
        static readonly Vector3 InstalledRollerScale = new Vector3(0.65f, 0.65f, 1f);
        static readonly Vector3 InstalledRollerPosition = new Vector3(0f, 0f, -0.5f);
        static readonly Vector3 SlotPosition = new Vector3(-0.327f, 0.327f, 0f);
        static readonly Vector3 SlotDefaultStateScale = new Vector3(0.75f, 0.75f, 1f);
        void Awake()
        {
            home = transform.localPosition;
            worldParent = transform.parent;
            OriginalScale = transform.localScale;
            homeScale=transform.localScale;homeRotation=transform.localRotation;
            hitbox = GetComponentInChildren<Collider2D>();
            // 把碰撞体从视觉子节点迁到根节点：避免悬浮/拖拽的缩放、上浮带动碰撞体（导致边缘来回抖动）
            if (hitbox != null && hitbox.transform != transform)
            {
                var oldBox = hitbox as BoxCollider2D;
                if (oldBox != null)
                {
                    var newBox = gameObject.AddComponent<BoxCollider2D>();
                    newBox.isTrigger = oldBox.isTrigger;
                    Vector3 lossy = oldBox.transform.lossyScale;
                    newBox.size = new Vector2(oldBox.size.x * Mathf.Abs(lossy.x), oldBox.size.y * Mathf.Abs(lossy.y));
                    Vector3 localCenter = transform.InverseTransformPoint(oldBox.transform.TransformPoint(oldBox.offset));
                    newBox.offset = new Vector2(localCenter.x, localCenter.y);
                    oldBox.enabled = false;
                    hitbox = newBox;
                }
            }
            visuals = GetComponentsInChildren<SpriteRenderer>(true);
            labels = GetComponentsInChildren<Canvas>(true);
            spriteOrders = new int[visuals.Length]; labelOrders = new int[labels.Length];
            for (int i = 0; i < visuals.Length; i++) spriteOrders[i] = visuals[i].sortingOrder;
            for (int i = 0; i < labels.Length; i++) labelOrders[i] = labels[i].sortingOrder;
            var box = GetComponentInChildren<MusicBoxController>(true);
            if (box != null)
            {
                rollerVisual = box.transform;
                rollerHomeScale = rollerVisual.localScale;
                rollerHomePosition = rollerVisual.localPosition;
            }
            else
            {
                // 结构零件：GearRotator 已配置在根节点上（场景里 isSpinning=false，由脚本 SetSpinState 控制）
                gearDriver = GetComponent<GearRotator>();
                // 找默认状态节点（根的直接子节点上的 SpriteRenderer），卡槽缩放用
                foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (sr.transform.parent == transform && sr.sprite != null)
                    {
                        defaultStateNode = sr.transform;
                        defaultStateHomeScale = defaultStateNode.localScale;
                        break;
                    }
                }
            }
        }
        public void SetHome() { home = transform.localPosition; visuals = GetComponentsInChildren<SpriteRenderer>(true); spriteOrders = new int[visuals.Length]; for (int i = 0; i < visuals.Length; i++) spriteOrders[i] = visuals[i].sortingOrder; }
        public void PlaceInSlot(Collider2D slot)
        {
            removeAt = -1;
            if (dragging) CancelDrag();
            if (slot == null) { ReparentTo(worldParent); transform.localPosition = home; transform.localScale=homeScale;transform.localRotation=homeRotation; ApplyRollerSlotTransform(null); RestoreDefaultState(); SetFeedbackActive(true); return; }
            ReparentTo(controller != null && controller.musicBox != null ? controller.musicBox : worldParent); // 放进卡槽：挂到八音盒下
            transform.localScale=homeScale;transform.localRotation=homeRotation;
            SetFeedbackActive(false); // 进卡槽：禁用手感反馈脚本
            if (slot == controller.structureSlot)
            {
                // 结构零件：吸附到固定点，缩放默认状态节点（不缩放根节点）
                transform.localPosition = SlotPosition;
                if (defaultStateNode != null) defaultStateNode.localScale = SlotDefaultStateScale;
            }
            else
            {
                var art=controller.GetComponent<ReTunePartsArt>();
                if(art!=null)art.FitInstalledPart(this,slot);
                ApplyRollerSlotTransform(slot);
                Vector3 center = slot.transform.TransformPoint(slot.offset);
                Vector3 ownOffset = hitbox.transform.TransformPoint(hitbox.offset) - transform.position;
                transform.position = new Vector3(center.x - ownOffset.x, center.y - ownOffset.y, transform.position.z);
            }
        }
        void RestoreDefaultState()
        {
            if (defaultStateNode != null) defaultStateNode.localScale = defaultStateHomeScale;
        }
        void ReparentTo(Transform newParent)
        {
            if (newParent == null || transform.parent == newParent) return;
            transform.SetParent(newParent, true);
        }
        void SetFeedbackActive(bool active)
        {
            var feedback = GetComponent<PartInteractionFeedback>();
            if (feedback != null) feedback.SetOptimizationState(active);
        }
        void ApplyRollerSlotTransform(Collider2D slot)
        {
            if (rollerVisual == null) return;
            if (slot == controller.toneSlot)
            {
                rollerVisual.localScale = InstalledRollerScale;
                rollerVisual.localPosition = InstalledRollerPosition;
            }
            else
            {
                rollerVisual.localScale = rollerHomeScale;
                rollerVisual.localPosition = rollerHomePosition;
            }
        }
        public void SetGearSpinning(bool spinning)
        {
            if (gearDriver != null) gearDriver.SetSpinState(spinning);
        }
        public void OnBeginDrag(PointerEventData e)
        {
            if (!controller.CanInteract || e.button != PointerEventData.InputButton.Left) return;
            if (!ReTuneWorldPointer.TryPosition(e.pressEventCamera, e.position, transform.position.z, out var pointer)) return;
            var art=controller.GetComponent<ReTunePartsArt>();if(art!=null)art.CancelPlayback();
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


