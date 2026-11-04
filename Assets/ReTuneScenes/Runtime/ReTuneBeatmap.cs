using UnityEngine;
namespace ReTune.Scenes
{
    [CreateAssetMenu(menuName="ReTune/固定点击谱面")]
    public sealed class ReTuneBeatmap : ScriptableObject
    {
        [Tooltip("相对 A 轨开始的点击时间，单位为秒，按升序填写")]
        public float[] hitTimes;
        [Tooltip("手动校准：正数使所有点击稍晚，负数使其提前")]
        public float offsetSeconds;
        void OnValidate(){if(hitTimes==null)return;for(int i=0;i<hitTimes.Length;i++)hitTimes[i]=Mathf.Max(0,hitTimes[i]);System.Array.Sort(hitTimes);}
    }
}
