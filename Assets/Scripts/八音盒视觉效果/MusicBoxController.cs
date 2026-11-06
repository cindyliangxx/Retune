using UnityEngine;

public class MusicBoxController : MonoBehaviour
{
    [Header("系统引用")]
    public Animator rollerAnimator;
    
    [Header("音梳配置")]
    public MusicBoxTineVisual[] tines;

    [Header("动画控制")]
    public float baseSpeed = 1f;
    
    private void Awake()
    {
        if (rollerAnimator == null) rollerAnimator = GetComponent<Animator>();
    }

    private void Start()
    {
        StopRolling();
    }

    public void OnRollerHit(int tineIndex)
    {
        if (tines == null || tineIndex < 0 || tineIndex >= tines.Length) return;
        if (tines[tineIndex] != null) tines[tineIndex].Press();
    }

    public void StartRolling(float speedMultiplier = 1f)
    {
        if (rollerAnimator != null)
        {
            rollerAnimator.speed = baseSpeed * speedMultiplier;
            if (!rollerAnimator.enabled) rollerAnimator.enabled = true;
        }
    }

    public void StopRolling()
    {
        if (rollerAnimator != null) rollerAnimator.speed = 0f;
        ResetAllTines();
    }

    public void SetRollerSpeed(float speed)
    {
        baseSpeed = speed;
        if (rollerAnimator != null)
        {
            rollerAnimator.speed = baseSpeed;
        }
        // 【删掉】不要在这里 ResetAllTines()，把它交给外部控制！
    }

    public void ResetAllTines()
    {
        if (tines == null) return;
        foreach (var tine in tines)
        {
            if (tine != null) tine.ForceReset();
        }
    }

    public void TriggerGlitchEffect(string type, float intensity = 1f)
    {
        if (rollerAnimator == null) return;
        if (type == "SlowMotion") rollerAnimator.speed = baseSpeed * 0.2f;
        else if (type == "Stutter") rollerAnimator.speed = 0f;
    }
}