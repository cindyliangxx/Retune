using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ReTune.Scenes
{
    public static class ReTuneProgress
    {
        const string Key = "ReTune.Progress.v1";
        [Serializable] sealed class Snapshot
        {
            public int version = 1, customer, structure, tone, hits, page;
            public float response, stability;
            public bool taughtClick, taughtHold, taughtParts;
            public string scene;
        }
        static Snapshot pending;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Initialize() { pending = null; }
        static bool Valid(Snapshot state)
        {
            return state != null && state.version == 1 && state.customer >= 0 && state.customer < 4 &&
                state.structure >= -1 && state.structure < 4 && (state.tone == -1 || state.tone == 4 || state.tone == 5) &&
                Array.IndexOf(new[] { "01_Shop", "02_Parts", "03_Performance", "04_Reply", "05_Ending" }, state.scene) >= 0;
        }
        public static void Save(Component owner, int page = 0)
        {
            string scene = owner.gameObject.scene.name;
            var state = new Snapshot { scene = scene, customer = ReTuneSession.customer,
                structure = ReTuneSession.structure, tone = ReTuneSession.tone, hits = ReTuneSession.hits,
                response = ReTuneSession.response, stability = ReTuneSession.stability, page = page,
                taughtClick = ReTuneSession.taughtClick, taughtHold = ReTuneSession.taughtHold, taughtParts = ReTuneSession.taughtParts };
            if (!Valid(state)) return;
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(state)); PlayerPrefs.Save();
        }
        public static bool Resume()
        {
            Snapshot state;
            try { state = JsonUtility.FromJson<Snapshot>(PlayerPrefs.GetString(Key, "")); }
            catch (ArgumentException) { return false; }
            if (!Valid(state)) return false;
            pending = state;
            ReTuneSession.Begin(state.customer);
            ReTuneSession.structure = state.structure; ReTuneSession.tone = state.tone; ReTuneSession.hits = state.hits;
            ReTuneSession.response = state.response; ReTuneSession.stability = state.stability;
            ReTuneSession.taughtClick = state.taughtClick; ReTuneSession.taughtHold = state.taughtHold; ReTuneSession.taughtParts = state.taughtParts;
            SceneManager.LoadScene("Assets/ReTuneScenes/Scenes/" + state.scene + ".unity");
            return true;
        }
        public static int ConsumePage(Component owner)
        {
            if (pending == null || pending.scene != owner.gameObject.scene.name) return 0;
            int result = pending.page; pending = null; return result;
        }
        public static void StartNew() { pending = null; PlayerPrefs.DeleteKey(Key); PlayerPrefs.Save(); }
    }
}
