using UnityEngine;
namespace ReTune.Scenes
{
    [CreateAssetMenu(menuName="ReTune/随机 QTE 间隔")]
    public sealed class ReTuneRandomQTESchedule : ReTuneQTESchedule
    {
        [Min(.1f)] public float minimum=2.5f, maximum=4.5f;
        public override float NextInterval() => Random.Range(Mathf.Max(.1f,minimum),Mathf.Max(minimum,maximum));
    }
}
