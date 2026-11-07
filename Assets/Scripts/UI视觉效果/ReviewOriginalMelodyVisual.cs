using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

/// <summary>
/// Review / Original Melody 两个按钮的悬浮视觉（纯视觉，不涉及按钮逻辑）。
/// 放到 Review_OriginalMelody_Layers 上，把两个按钮、外圈、bg大齿轮、小齿轮拖进对应字段即可。
/// 悬浮时按钮水平滑动 + 外圈/齿轮旋转，全部用 DOTween。
/// </summary>
public class ReviewOriginalMelodyVisual : MonoBehaviour
{
    [Header("按钮（bg1 = Original Melody，bg2 = Review）")]
    public RectTransform reviewButton;
    public RectTransform originalMelodyButton;

    [Header("Review 按钮滑动")]
    [Tooltip("不悬浮时的 posX")]
    public float reviewIdlePosX = 209.5f;
    [Tooltip("悬浮时向右移动的距离（悬浮 posX = idle + 距离）")]
    public float reviewHoverOffset = 23.9f;

    [Header("Original Melody 按钮滑动")]
    [Tooltip("不悬浮时的 posX")]
    public float originalIdlePosX = 265.4f;
    [Tooltip("悬浮时向右移动的距离（悬浮 posX = idle + 距离）")]
    public float originalHoverOffset = 28.7f;

    [Header("旋转物体")]
    public Transform outerRing;    // 外圈
    public Transform bgBigGear;    // bg大齿轮
    public Transform bgSmallGear;  // bg_小齿轮（场景里叫“小齿轮”）

    [Header("外圈 旋转角度 z")]
    public float outerRingReviewZ = 0.39f;    // 悬浮 Review
    public float outerRingOriginalZ = 0f;     // 悬浮 Original
    public float outerRingIdleZ = 24f;        // 无悬浮

    [Header("大齿轮 旋转角度 z")]
    public float bigGearReviewZ = -30f;
    public float bigGearOriginalZ = 30f;
    public float bigGearIdleZ = 0f;

    [Header("小齿轮 旋转角度 z")]
    public float smallGearReviewZ = 40f;
    public float smallGearOriginalZ = -40f;
    public float smallGearIdleZ = 0f;

    [Header("动画参数")]
    public float duration = 0.3f;
    public Ease ease = Ease.OutQuad;

    private bool reviewHovered;
    private bool originalHovered;

    private void Awake()
    {
        SetupHoverTrigger(reviewButton, true);
        SetupHoverTrigger(originalMelodyButton, false);
    }

    private void Start()
    {
        // 初始应用一次 idle 状态，避免启动时停留在场景里的初始角度（外圈=0 等）
        ApplyState();
    }

    // 给按钮自动挂 EventTrigger 的 PointerEnter / PointerExit（不依赖 Inspector 配置）
    private void SetupHoverTrigger(RectTransform btn, bool isReview)
    {
        if (btn == null) return;

        // 确保按钮可接收射线（有些按钮的 Image 关了 Raycast Target，悬浮会失效）
        var graphic = btn.GetComponent<UnityEngine.UI.Graphic>();
        if (graphic != null) graphic.raycastTarget = true;

        var trigger = btn.GetComponent<EventTrigger>();
        if (trigger == null) trigger = btn.gameObject.AddComponent<EventTrigger>();

        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => SetHover(isReview, true));
        trigger.triggers.Add(enter);

        var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => SetHover(isReview, false));
        trigger.triggers.Add(exit);
    }

    private void SetHover(bool isReview, bool hovered)
    {
        if (isReview) reviewHovered = hovered;
        else originalHovered = hovered;
        ApplyState();
    }

    private void ApplyState()
    {
        // 优先级：Review 悬浮 > Original 悬浮 > 无悬浮
        if (reviewHovered)
        {
            MoveX(reviewButton, reviewIdlePosX + reviewHoverOffset);
            MoveX(originalMelodyButton, originalIdlePosX);
            RotateTo(outerRing, outerRingReviewZ);
            RotateTo(bgBigGear, bigGearReviewZ);
            RotateTo(bgSmallGear, smallGearReviewZ);
        }
        else if (originalHovered)
        {
            MoveX(reviewButton, reviewIdlePosX);
            MoveX(originalMelodyButton, originalIdlePosX + originalHoverOffset);
            RotateTo(outerRing, outerRingOriginalZ);
            RotateTo(bgBigGear, bigGearOriginalZ);
            RotateTo(bgSmallGear, smallGearOriginalZ);
        }
        else
        {
            MoveX(reviewButton, reviewIdlePosX);
            MoveX(originalMelodyButton, originalIdlePosX);
            RotateTo(outerRing, outerRingIdleZ);
            RotateTo(bgBigGear, bigGearIdleZ);
            RotateTo(bgSmallGear, smallGearIdleZ);
        }
    }

    private void MoveX(RectTransform btn, float posX)
    {
        if (btn == null) return;
        btn.DOKill();
        btn.DOAnchorPosX(posX, duration).SetEase(ease);
    }

    private void RotateTo(Transform t, float z)
    {
        if (t == null) return;
        t.DOKill();
        t.DOLocalRotate(new Vector3(0f, 0f, z), duration).SetEase(ease);
    }
}
