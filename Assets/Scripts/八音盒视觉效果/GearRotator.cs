using UnityEngine;
using System.Collections;

public class GearRotator : MonoBehaviour
{
    [Header("旋转设置")]
    [Tooltip("基础转速（度/秒）")]
    public float baseDegreesPerSecond = 45f;
    
    [Tooltip("是否顺时针旋转")]
    public bool isClockwise = true;

    [Header("状态控制")]
    [Tooltip("是否允许旋转（开关）")]
    public bool isSpinning = true;

    [Header("状态监控")]
    [SerializeField] private float currentDegreesPerSecond;

    // 内部状态锁
    private bool isStuttering = false;

    private void Start()
    {
        // 初始化当前速度
        currentDegreesPerSecond = baseDegreesPerSecond;
    }

    private void Update()
    {
        // 如果开关关闭，或者正在卡顿，则不执行基础旋转
        if (!isSpinning || isStuttering) return;

        // 保证当前速度等于基础速度
        currentDegreesPerSecond = baseDegreesPerSecond;
        PerformRotation();
    }

    private void PerformRotation()
    {
        // 根据方向决定正负值
        float direction = isClockwise ? -1f : 1f;
        
        // 绕 Z 轴旋转
        transform.Rotate(0, 0, direction * currentDegreesPerSecond * Time.deltaTime);
    }

    // ==========================================
    // 外部控制接口
    // ==========================================

    /// <summary>
    /// 开关旋转（提供给外部调用的统一接口）
    /// </summary>
    /// <param name="state">true为开始旋转，false为停止旋转</param>
    public void SetSpinState(bool state)
    {
        isSpinning = state;
        
        // 如果强制停止旋转，立刻打断卡顿效果
        if (!isSpinning && isStuttering)
        {
            StopAllCoroutines();
            isStuttering = false;
            currentDegreesPerSecond = baseDegreesPerSecond;
        }
    }

    /// <summary>
    /// 动态设置转速
    /// </summary>
    public void SetSpeed(float degreesPerSecond)
    {
        baseDegreesPerSecond = degreesPerSecond;
    }

    /// <summary>
    /// 动态设置旋转方向
    /// </summary>
    public void SetDirection(bool clockwise)
    {
        isClockwise = clockwise;
    }

    /// <summary>
    /// 反转旋转方向
    /// </summary>
    public void FlipDirection()
    {
        isClockwise = !isClockwise;
    }

    // ==========================================
    // 预留：卡顿效果接口
    // ==========================================

    /// <summary>
    /// 触发卡顿效果（外部可调用）
    /// </summary>
    /// <param name="duration">卡顿持续总时间</param>
    /// <param name="intensity">卡顿强度（0~1，影响速度衰减和抽搐幅度）</param>
    public void TriggerStutter(float duration = 1f, float intensity = 0.5f)
    {
        // 如果开关没开，或者已经在卡顿中，则不触发
        if (!isSpinning || isStuttering) return;
        
        StartCoroutine(StutterRoutine(duration, intensity));
    }

    private IEnumerator StutterRoutine(float duration, float intensity)
    {
        isStuttering = true;
        float elapsed = 0f;
        float originalSpeed = baseDegreesPerSecond;
        float direction = isClockwise ? -1f : 1f;
        float reverseSpeed = 60f + 60f * Mathf.Clamp01(intensity); // 回轮速度（度/秒），明显可见

        // 卡顿期间：回轮（反向转）→ 卡住不动，循环
        while (elapsed < duration)
        {
            // 回轮：反向转一下
            float reverseDuration = Random.Range(0.06f, 0.12f);
            float reverseElapsed = 0f;
            while (reverseElapsed < reverseDuration && elapsed < duration)
            {
                if (!isSpinning) break;
                transform.Rotate(0, 0, -direction * reverseSpeed * Time.deltaTime);
                reverseElapsed += Time.deltaTime;
                elapsed += Time.deltaTime;
                yield return null;
            }

            // 卡住：停一下
            float freezeDuration = Random.Range(0.08f, 0.15f);
            float freezeElapsed = 0f;
            while (freezeElapsed < freezeDuration && elapsed < duration)
            {
                if (!isSpinning) break;
                freezeElapsed += Time.deltaTime;
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!isSpinning) break;
        }

        // 恢复
        isStuttering = false;
        currentDegreesPerSecond = originalSpeed;
    }
}