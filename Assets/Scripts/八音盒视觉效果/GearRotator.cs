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

        // 卡顿期间：随机停止、减速、甚至偶尔轻微倒转
        while (elapsed < duration)
        {
            // 随机决定这一小段的表现：1=卡住不动, 2=极慢, 3=反向抽搐
            int state = Random.Range(1, 4);
            
            if (state == 1)
            {
                currentDegreesPerSecond = 0f;
            }
            else if (state == 2)
            {
                currentDegreesPerSecond = originalSpeed * Random.Range(0.1f, 0.3f);
            }
            else
            {
                // 反向抽搐（根据强度决定幅度）
                currentDegreesPerSecond = -originalSpeed * intensity * Random.Range(0.5f, 1.5f);
            }

            // 执行当前状态下的旋转（模拟抽搐）
            float stateDuration = Random.Range(0.03f, 0.1f);
            float stateElapsed = 0f;
            
            while (stateElapsed < stateDuration && elapsed < duration)
            {
                // 每次旋转前再检查一遍开关，防止在卡顿中突然被外部关闭
                if (!isSpinning) break; 

                transform.Rotate(0, 0, direction * currentDegreesPerSecond * Time.deltaTime);
                stateElapsed += Time.deltaTime;
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            if (!isSpinning) break; // 如果被外部中断，直接跳出大循环
        }

        // 恢复
        isStuttering = false;
        currentDegreesPerSecond = originalSpeed;
    }
}