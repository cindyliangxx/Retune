using UnityEngine;
    public class MusicBoxController : MonoBehaviour
    {
        [Header("系统引用")]
        [Tooltip("八音盒的动画组件（控制滚轮旋转的）")]
        public Animator rollerAnimator;
        
        [Header("音梳配置")]
        [Tooltip("按顺序排列的所有音梳（建议按从左到右的顺序拖入）")]
        public MusicBoxTineVisual[] tines;

        [Header("动画控制")]
        public float baseSpeed = 1f;
        private float currentSpeed = 1f;

        private void Awake()
        {
            if (rollerAnimator == null) rollerAnimator = GetComponent<Animator>();
            currentSpeed = baseSpeed;
        }

        // ==========================================
        // 1. 动画事件入口 (由美术在 Animation 窗口打点)
        // ==========================================
        
        /// <summary>
        /// 当滚轮凸起压到音梳时，由动画事件调用
        /// </summary>
        /// <param name="tineIndex">音梳的数组索引</param>
        public void OnRollerHit(int tineIndex)
        {
            // 越界保护
            if (tineIndex < 0 || tineIndex >= tines.Length) return;
            
            // 触发对应音梳的视觉表现
            tines[tineIndex].Press();
        }

        // ==========================================
        // 2. 全局控制接口
        // ==========================================

        /// <summary>
        /// 设置滚轮整体播放速度（变速、暂停）
        /// </summary>
        public void SetRollerSpeed(float speed)
        {
            currentSpeed = speed;
            if (rollerAnimator != null)
            {
                rollerAnimator.speed = currentSpeed;
            }
        }

        /// <summary>
        /// 紧急复位（切场景、暂停时调用）
        /// </summary>
        public void ResetAllTines()
        {
            foreach (var tine in tines)
            {
                if (tine != null) tine.ForceReset();
            }
        }

        // ==========================================
        // 3. 预留接口：用于后续接入零件效果 / 视觉异象
        // ==========================================

        /// <summary>
        /// 切换两套不同的滚轮/动画 (比如换零件后)
        /// </summary>
        public void SwitchRollerAnimation(RuntimeAnimatorController newController)
        {
            if (rollerAnimator != null && newController != null)
            {
                rollerAnimator.runtimeAnimatorController = newController;
                ResetAllTines();
            }
        }

        /// <summary>
        /// 触发故障/视觉异象效果（卡顿、放慢、抽搐等）
        /// </summary>
        /// <param name="type">效果类型</param>
        /// <param name="intensity">强度</param>
        public void TriggerGlitchEffect(string type, float intensity = 1f)
        {
            // 目前留空，后续可以在这里加效果
            // 例如：
            // if (type == "Stutter") 开始卡顿协程...
            // if (type == "Slowdown") SetRollerSpeed(0.5f)...
            
            Debug.Log($"[MusicBoxController] 触发视觉异象: {type}, 强度: {intensity}");
            
            // 示例：如果是放慢效果
            if (type == "SlowMotion")
            {
                SetRollerSpeed(baseSpeed * 0.2f);
                // 可以在这里调用 DOTween 让画面暗下来
            }
        }
    }