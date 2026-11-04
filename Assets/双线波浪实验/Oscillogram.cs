using UnityEngine;
using UnityEngine.Events;

public class Oscillogram : MonoBehaviour
{
    [SerializeField] new ParticleSystem particleSystem;
    [SerializeField] new Rigidbody rigidbody;
    [SerializeField] [Min(0)] [Tooltip("每分钟跳动次数")] int frequency;
    [SerializeField] [Tooltip("跳动时施加的力")] float amplitude;
    [SerializeField] UnityEvent beatEvent;
    public bool playBeatEvents = true;

    [Header("演奏关卡共享相位曲线；不启用原粒子物理时使用")]
    public LineRenderer sharedCurve;
    public float curveWidth=4.3f, curveAmplitude=.6f;
    // 两条波浪在同一相位上采样，进度为 1 时坐标逐点相等。
    public void DrawSharedWave(float phase, float amount, float pulse, float noise)
    {
        if(sharedCurve==null)return;
        const int count=160;sharedCurve.positionCount=count;
        for(int i=0;i<count;i++)
        {
            float x=(float)i/(count-1);
            float basis=Mathf.Sin(x*Mathf.PI*4+phase);
            float y=basis*curveAmplitude*amount + Mathf.Sin(x*Mathf.PI*4+phase)*pulse + Mathf.Sin(x*85+phase*3)*noise;
            sharedCurve.SetPosition(i,new Vector3(x*curveWidth,y,0));
        }
    }
    // 音乐驱动显示：共用采样轮廓，进度只改变幅度，不改变播放速度。
    public void DrawAudioWave(float[] samples,float amount,float pulse,float noise,float phase)
    {
        if(sharedCurve==null || samples==null || samples.Length<2)return;
        sharedCurve.positionCount=samples.Length;
        for(int i=0;i<samples.Length;i++)
        {
            float x=(float)i/(samples.Length-1);
            float y=samples[i]*curveAmplitude*amount + samples[i]*pulse + Mathf.Sin(x*85+phase*3)*noise;
            sharedCurve.SetPosition(i,new Vector3(x*curveWidth,y,0));
        }
    }
    float lastTime;
    Vector3 initialPosition;
    bool initialized;

    void Awake() { if (rigidbody != null) { initialPosition = rigidbody.position; initialized = true; } }
    void OnEnable() { if (rigidbody != null) rigidbody.isKinematic = false; }
    void OnDisable() { if (rigidbody != null) rigidbody.isKinematic = true; }
    public void ResetWave()
    {
        if (particleSystem != null) { particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); }
        if (rigidbody != null && initialized) { rigidbody.position = initialPosition; rigidbody.velocity = Vector3.zero; rigidbody.angularVelocity = Vector3.zero; }
        lastTime = Time.fixedTime;
    }

    void FixedUpdate()
    {
        if (particleSystem == null || rigidbody == null) return;
        particleSystem.Emit(1);
        particleSystem.Simulate(Time.fixedDeltaTime, true, false);

        if (frequency > 0 && Time.fixedTime - lastTime > 60.0f / frequency)
        {
            if (playBeatEvents) beatEvent.Invoke();
            rigidbody.AddForce(Vector3.up * amplitude);
            lastTime = Time.fixedTime;
        }
    }
}


