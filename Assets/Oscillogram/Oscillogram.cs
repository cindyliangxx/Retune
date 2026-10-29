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
