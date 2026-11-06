using UnityEngine;
using DG.Tweening;
//用于八音盒视觉显示
//单条音梳的状态切换
//
public class MusicBoxTineVisual : MonoBehaviour
{
    [Header("视觉引用 (直接拖入对应的 SpriteRenderer)")]
    [Tooltip("默认/抬起状态下的物体渲染器")]
    public SpriteRenderer defaultRenderer;
    
    [Tooltip("按下状态下的物体渲染器")]
    public SpriteRenderer pressedRenderer;

    [Header("动画参数")]
    [Tooltip("过渡时间（秒），决定音梳按下去有多快")]
    public float transitionDuration = 0.05f;

    [Tooltip("按下后保持的时间（秒），模拟被压住的瞬间")]
    public float pressHoldDuration = 0.1f;

    [Tooltip("过渡曲线")]
    public Ease transitionEase = Ease.OutQuad;

    public Color defaultColor;
    public Color pressedColor;
    
    // 防抖锁
    private float lastPressTime;
    
    // DOTween 序列（用于统一管理按下和弹起的过程）
    private Sequence currentSequence;

    private void Awake()
    {
        // 1. 记录初始颜色
        // if (defaultRenderer != null)
        // {
        //     defaultColor = defaultRenderer.color;
        //     defaultColor.a = 1f; // 默认不透明
        //     defaultRenderer.color = defaultColor;
        // }
        
        // if (pressedRenderer != null)
        // {
        //     pressedColor = pressedRenderer.color;
        //     pressedColor.a = 0f; // 按下状态初始透明
        //     pressedRenderer.color = pressedColor;
        //     pressedRenderer.enabled = false; // 初始不渲染
        // }
    }

    /// <summary>
    /// 触发音梳按下
    /// </summary>
    public void Press()
    {
        // 防抖处理：应对高速滚轮导致的动画事件密集触发
        if (Time.time - lastPressTime < 0.05f) return;
        lastPressTime = Time.time;

        // 如果之前的动画还在播放，直接杀掉（打断）
        currentSequence?.Kill();

        // 开启按下状态的渲染
        if (pressedRenderer != null) pressedRenderer.enabled = true;

        // 创建一个新的动画序列
        currentSequence = DOTween.Sequence();

        // 1. 默认状态淡出 & 按下状态淡入
        currentSequence.Join(defaultRenderer.DOFade(0f, transitionDuration).SetEase(transitionEase));
        currentSequence.Join(pressedRenderer.DOFade(1f, transitionDuration).SetEase(transitionEase));

        // 2. 保持被压住的瞬间
        currentSequence.AppendInterval(pressHoldDuration);

        // 3. 弹起：默认状态淡入 & 按下状态淡出
        currentSequence.Append(defaultRenderer.DOFade(1f, transitionDuration).SetEase(transitionEase));
        currentSequence.Join(pressedRenderer.DOFade(0f, transitionDuration).SetEase(transitionEase));

        // 4. 动画结束后的收尾工作
        currentSequence.OnComplete(() =>
        {
            if (pressedRenderer != null) pressedRenderer.enabled = false; // 节省性能
        });
    }

    /// <summary>
    /// 强制复位（用于暂停、退出、切动画等紧急情况）
    /// </summary>
    public void ForceReset()
    {
        // 停止并清理当前动画
        currentSequence?.Kill();
        
        if (defaultRenderer != null) 
        { 
            Color c = defaultColor; c.a = 1f; defaultRenderer.color = c; 
        }
        
        if (pressedRenderer != null) 
        { 
            Color c = pressedColor; c.a = 0f; pressedRenderer.color = c; 
            pressedRenderer.enabled = false; 
        }
    }
}