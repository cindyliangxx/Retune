using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class PartInteractionFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("========== 全局设置 ==========")]
    [Tooltip("总开关：是否开启手感优化")]
    public bool enableOptimization = true;

    [Header("拖拽判定区域 (独立检测)")]
    [Tooltip("把目标槽位的 Collider 拖入这里。拖拽进入该区域时自动触发拖拽反馈")]
    public Collider2D targetZone; 

    [Header("素材渲染器 (按需拖入)")]
    public SpriteRenderer defaultRenderer;
    public SpriteRenderer hoverRenderer;
    public SpriteRenderer dragRenderer;
    public float spriteFadeDuration = 0.15f;

    [Header("========== 悬浮 (Hover) 状态独立配置 ==========")]
    public bool enableHover = true;
    public bool hover_UseScale = true;
    public float hover_ScaleMultiplier = 1.1f;
    
    public bool hover_UseRotate = true;
    public float hover_TiltAngle = 5f;
    public float hover_VibrateStrength = 2f;

    public bool hover_UseSound = true;
    public AudioClip hover_Sound;
    public bool hover_UseSpriteChange = true;

    [Header("========== 拖拽 (Drag) 状态独立配置 ==========")]
    public bool enableDrag = true;
    public bool drag_UseScale = true;
    public float drag_ScaleMultiplier = 1.2f;

    public bool drag_UseSound = true;
    public AudioClip drag_Sound;
    public bool drag_UseSpriteChange = true;

    [Header("========== 通用动画参数 ==========")]
    public float scaleDuration = 0.2f;
    public Ease scaleEase = Ease.OutBack;
    [Range(0f, 1f)] public float soundVolume = 0.5f;

    // --- 内部状态 ---
    private enum PartState { Default, Hover, Dragging }
    private PartState currentState = PartState.Default;
    
    private Vector3 originalScale;
    private Quaternion originalRotation;
    private AudioSource audioSource;

    // 状态记录
    private bool isHovering = false;
    private bool isDragging = false;   // 是否正在被鼠标拖拽
    private bool isInTargetZone = false; // 是否在判定区内

    // Tween 引用
    private Tween scaleTween;
    private Tween rotateTween;
    private Tween vibrateTween;
    private Tween defaultFadeTween;
    private Tween hoverFadeTween;
    private Tween dragFadeTween;

    private void Awake()
    {
        originalScale = transform.localScale;
        originalRotation = transform.localRotation;

        AutoAssignRenderers();

        if (hover_UseSound || drag_UseSound)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }

        InitSpriteAlpha();
    }

    private void InitSpriteAlpha()
    {
        if (defaultRenderer != null) SetAlpha(defaultRenderer, 1f);
        if (hoverRenderer != null) SetAlpha(hoverRenderer, 0f);
        if (dragRenderer != null) SetAlpha(dragRenderer, 0f);
    }

    // 素材没在 Inspector 拖入时，按「根的直接子节点=默认素材，其子节点=hover/drag 素材」自动补全
    private void AutoAssignRenderers()
    {
        if (defaultRenderer != null && hoverRenderer != null && dragRenderer != null) return;

        Transform gearNode = null;
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr.transform.parent == transform && sr.sprite != null) { gearNode = sr.transform; break; }
        }
        if (gearNode == null) return;

        if (defaultRenderer == null) defaultRenderer = gearNode.GetComponent<SpriteRenderer>();

        foreach (var sr in gearNode.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr == defaultRenderer) continue;
            if (hoverRenderer == null) hoverRenderer = sr;
            else if (dragRenderer == null) dragRenderer = sr;
            else break;
        }
    }

    // ==========================================
    // 鼠标事件 (控制拖拽的开始和结束，以及悬浮)
    // ==========================================
    
    public void OnPointerEnter(PointerEventData e)
    {
        if (!enableOptimization) return;
        isHovering = true;
        if (!isDragging && !isInTargetZone) ChangeState(PartState.Hover);
    }

    public void OnPointerExit(PointerEventData e)
    {
        if (!enableOptimization) return;
        isHovering = false;
        if (!isDragging && !isInTargetZone) ChangeState(PartState.Default);
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (!enableOptimization || e.button != PointerEventData.InputButton.Left) return;
        // 鼠标按下，标记为正在拖拽
        isDragging = true;
        ChangeState(PartState.Dragging);
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (!enableOptimization || e.button != PointerEventData.InputButton.Left) return;
        // 鼠标松开，结束拖拽
        isDragging = false;
        isInTargetZone = false; // 松手重置区域状态
        
        if (isHovering) ChangeState(PartState.Hover);
        else ChangeState(PartState.Default);
    }

    // ==========================================
    // 核心：独立检测碰撞体状态
    // ==========================================
    private void Update()
    {
        if (!enableOptimization) return;
        
        // 只有在鼠标按住拖拽，且配置了判定区时，才进行独立检测
        if (isDragging && targetZone != null)
        {
            // 使用当前物体的坐标去检测是否落在 targetZone 内
            // (假设你的判定区是 2D Collider)
            bool currentlyInside = targetZone.OverlapPoint(transform.position);

            // 如果状态发生了改变（刚进入或刚离开）
            if (currentlyInside != isInTargetZone)
            {
                isInTargetZone = currentlyInside;
                
                if (isInTargetZone)
                {
                    // 刚进入判定区：可以在这里加额外的音效或特效，或者直接保持 Drag 放大
                    ChangeState(PartState.Dragging);
                }
                else
                {
                    // 刚离开判定区：如果鼠标还在物体上就是 Hover，否则是 Default
                    if (isHovering) ChangeState(PartState.Hover);
                    else ChangeState(PartState.Default);
                }
            }
        }
    }

    // ==========================================
    // 核心状态机
    // ==========================================

    private void ChangeState(PartState newState)
    {
        if (currentState == newState) return;
        
        rotateTween?.Kill();
        vibrateTween?.Kill();
        
        currentState = newState;

        switch (currentState)
        {
            case PartState.Default:
                ApplyDefaultState();
                break;
            case PartState.Hover:
                ApplyHoverState();
                break;
            case PartState.Dragging:
                ApplyDragState();
                break;
        }
    }

    private void ApplyDefaultState()
    {
        if (hover_UseScale || drag_UseScale)
        {
            scaleTween?.Kill();
            scaleTween = transform.DOScale(originalScale, scaleDuration).SetEase(Ease.OutQuad);
        }
        transform.DORotateQuaternion(originalRotation, scaleDuration);
        UpdateSpriteVisuals(false, false);
    }

    private void ApplyHoverState()
    {
        if (!enableHover) { ApplyDefaultState(); return; }

        PlaySound(hover_Sound);

        if (hover_UseScale)
        {
            scaleTween?.Kill();
            scaleTween = transform.DOScale(originalScale * hover_ScaleMultiplier, scaleDuration).SetEase(scaleEase);
        }

        if (hover_UseRotate)
        {
            Quaternion targetRot = originalRotation * Quaternion.Euler(0, 0, hover_TiltAngle);
            rotateTween = transform.DORotateQuaternion(targetRot, 0.4f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
            
            if (hover_VibrateStrength > 0)
            {
                vibrateTween = transform.DOShakeRotation(0.3f, new Vector3(0, 0, hover_VibrateStrength), 10, 90f).SetLoops(-1, LoopType.Restart);
            }
        }
        UpdateSpriteVisuals(hover_UseSpriteChange, false);
    }

    private void ApplyDragState()
    {
        if (!enableDrag) { ApplyDefaultState(); return; }

        PlaySound(drag_Sound);

        if (drag_UseScale)
        {
            scaleTween?.Kill();
            scaleTween = transform.DOScale(originalScale * drag_ScaleMultiplier, scaleDuration).SetEase(scaleEase);
        }

        transform.DORotateQuaternion(originalRotation, scaleDuration);
        UpdateSpriteVisuals(false, drag_UseSpriteChange);
    }

    private void UpdateSpriteVisuals(bool isHover, bool isDrag)
    {
        float defaultAlpha = (!isHover && !isDrag) ? 1f : 0f;
        float hoverAlpha = (isHover && !isDrag) ? 1f : 0f;
        float dragAlpha = (isDrag) ? 1f : 0f;

        if (defaultRenderer != null) { defaultFadeTween?.Kill(); defaultFadeTween = defaultRenderer.DOFade(defaultAlpha, spriteFadeDuration); }
        if (hoverRenderer != null) { hoverFadeTween?.Kill(); hoverFadeTween = hoverRenderer.DOFade(hoverAlpha, spriteFadeDuration); }
        if (dragRenderer != null) { dragFadeTween?.Kill(); dragFadeTween = dragRenderer.DOFade(dragAlpha, spriteFadeDuration); }
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip, soundVolume);
    }

    private void SetAlpha(SpriteRenderer sr, float alpha)
    {
        Color c = sr.color; c.a = alpha; sr.color = c;
    }

    public void ForceReturnToDefault()
    {
        isInTargetZone = false;
        isHovering = false;
        isDragging = false;
        ChangeState(PartState.Default);
    }

    public void SetOptimizationState(bool state)
    {
        enableOptimization = state;
        if (!enableOptimization)
        {
            KillAllTweens();
            ForceReturnToDefault();
            transform.localScale = originalScale;
            transform.localRotation = originalRotation;

            if (defaultRenderer != null) SetAlpha(defaultRenderer, 1f);
            if (hoverRenderer != null) SetAlpha(hoverRenderer, 0f);
            if (dragRenderer != null) SetAlpha(dragRenderer, 0f);
        }
    }

    private void KillAllTweens()
    {
        scaleTween?.Kill(); rotateTween?.Kill(); vibrateTween?.Kill();
        defaultFadeTween?.Kill(); hoverFadeTween?.Kill(); dragFadeTween?.Kill();
    }

    private void OnDisable()
    {
        SetOptimizationState(false);
    }
}