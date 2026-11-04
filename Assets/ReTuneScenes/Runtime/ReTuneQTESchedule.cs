using UnityEngine;
namespace ReTune.Scenes
{
    // 随机出题策略；后续 BPM 策略只需继承并替换 Inspector 引用。
    public abstract class ReTuneQTESchedule : ScriptableObject
    {
        public abstract float NextInterval();
    }
}
