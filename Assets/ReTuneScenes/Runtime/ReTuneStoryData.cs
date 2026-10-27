using System;
using UnityEngine;

namespace ReTune.Scenes
{
    [CreateAssetMenu(menuName = "ReTune/Story Data")]
    public sealed class ReTuneStoryData : ScriptableObject
    {
        [Serializable] public class Customer
        {
            public string name, emotion;
            [TextArea(2, 6)] public string[] dialogue;
            [TextArea(3, 8)] public string reply;
            public int structure, tone;
            public float drift = .07f, wave = .22f, frequency = .9f, impulse;
            public bool mostlyHold;
        }
        [Serializable] public class Part
        {
            public string name;
            public bool structure;
            [TextArea] public string description;
        }
        public Customer[] customers;
        public Part[] parts;
    }

    // Only value data crosses scene boundaries. Scene objects always belong to their own scene.
    public static class ReTuneSession
    {
        public static bool active;
        public static int customer, structure = -1, tone = -1, hits;
        public static float response, stability;
        public static bool taughtClick, taughtHold;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset() { active = false; customer = hits = 0; structure = tone = -1; response = stability = 0; taughtClick = taughtHold = false; }
        public static void Begin(int index) { active = true; customer = index; structure = tone = -1; hits = 0; response = stability = 0; }
    }
}
