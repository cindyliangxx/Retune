using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ReTune.Scenes
{
    public sealed class ReTuneSceneController : MonoBehaviour
    {
        public enum Screen { Menu, Shop, Parts, Performance, Reply, Ending }
        [Header("Scene entry — opening any scene directly is supported")]
        public Screen screen;
        public ReTuneStoryData story;
        [Range(0, 3)] public int previewCustomer;
        [Header("Scene-owned UI references")]
        public Text customerLabel, body, hint, actionLabel, structureLabel, toneLabel, stats, noteLabel, mixLabel, overlayTitle, overlayBody;
        [Header("Scene-owned world objects")]
        public Camera worldCamera;
        public Collider2D structureSlot, toneSlot;
        public Transform needle, approachRing;
        public ReTunePartDrag[] partObjects;
        public Image stabilityFill, responseFill, holdFill, phraseFill;
        public GameObject overlay;
        public GameObject[] portraits;
        public Button trialButton, originalButton;
        [Header("Audio sources are pre-existing scene objects")]
        public AudioSource trackA, trackB, trackC;
        [Header("Prototype tuning")]
        public int noteCount = 8;
        public float noteSpacing = 3.2f, firstNoteTime = 2.3f, hitWindow = .55f, holdDuration = 1.25f;
        public float safeRange = .2f, minimumStability = .35f, correctionSpeed = .8f;
        int line, note, hits;
        float time, balance, stableTime, offTime, holdTime, response, noise = 1, noiseTarget = 1, transitionTime;
        bool holding, transitioning, frozen, audioPaused, loading;
        bool resonating;
        ReTuneStoryData.Customer Customer => story.customers[Mathf.Clamp(ReTuneSession.customer, 0, story.customers.Length - 1)];
        public bool CanInteract => !frozen && !transitioning && !loading;
        bool IsHold => Customer.mostlyHold ? note % 3 != 0 : ReTuneSession.customer == 0 ? note == 3 || note == 7 : note % 2 == 1;
        float Due => firstNoteTime + note * noteSpacing;
        public float Stability => time > 0 ? stableTime / time : 1;
        public int CurrentNote => note;

        void Start()
        {
            if (!ReTuneSession.active && screen != Screen.Menu && screen != Screen.Ending) ReTuneSession.Begin(previewCustomer);
            if (overlay != null) overlay.SetActive(false);
            frozen = false;
            if (customerLabel != null) customerLabel.text = "DAY 0" + (ReTuneSession.customer + 1) + "   " + Customer.name + "  /  " + Customer.emotion;
            if (portraits != null) for (int i = 0; i < portraits.Length; i++) if (portraits[i] != null) portraits[i].SetActive(i == ReTuneSession.customer);
            if (screen == Screen.Shop) RefreshDialogue();
            else if (screen == Screen.Parts) { RefreshSlots(); PlayAudio(); }
            else if (screen == Screen.Performance) ResetPerformance();
            else if (screen == Screen.Reply) { Set(body, Customer.reply); Set(actionLabel, ReTuneSession.customer < story.customers.Length - 1 ? "Next Day" : "Epilogue"); response = ReTuneSession.response; noise = noiseTarget = 0; PlayAudio(); }
            UpdateMix();
        }
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) { if (frozen) CloseOverlay(); else Pause(); }
            if (audioPaused != frozen)
            {
                foreach (var source in Sources()) if (source != null) { if (frozen) source.Pause(); else source.UnPause(); }
                audioPaused = frozen;
            }
            if (frozen || loading) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, .05f);
            noise = Mathf.MoveTowards(noise, noiseTarget, dt * .8f);
            if (screen == Screen.Parts && transitioning)
            { transitionTime += dt; if (transitionTime >= 1.9f) Load(Screen.Performance); }
            if (screen == Screen.Performance) TickPerformance(dt);
            UpdateMix();
        }
        public void StartHealing() { if (!CanInteract) return; ReTuneSession.Reset(); ReTuneSession.Begin(0); Load(Screen.Shop); }
        public void ContinueStory()
        {
            if (!CanInteract) return;
            if (screen == Screen.Shop)
            {
                if (line < Customer.dialogue.Length - 1) { line++; RefreshDialogue(); }
                else if (!resonating)
                { resonating = true; Set(body, "A hand rests on the music box.\nA troubled melody begins."); Set(actionLabel, "Open Music Box"); PlayAudio(); }
                else Load(Screen.Parts);
            }
            else if (screen == Screen.Reply)
            { if (ReTuneSession.customer < story.customers.Length - 1) { ReTuneSession.Begin(ReTuneSession.customer + 1); Load(Screen.Shop); } else Load(Screen.Ending); }
        }
        void RefreshDialogue() { Set(body, Customer.dialogue[line]); Set(actionLabel, line < Customer.dialogue.Length - 1 ? "Next" : "Touch Music Box"); }
        public void TouchMusicBox()
        {
            if (!CanInteract || screen != Screen.Shop) return;
            if (line < Customer.dialogue.Length - 1) { Set(hint, "Finish the dialogue first."); return; }
            ContinueStory();
        }
        public void ShowHistory() { if (CanInteract) ShowOverlay("Dialogue / " + Customer.name, string.Join("\n\n", Customer.dialogue)); }
        public void Pause() { ShowOverlay("Paused", "Select Continue to resume."); }
        void ShowOverlay(string title, string text)
        { if (holding) ResolveNote(false); frozen = true; Set(overlayTitle, title); Set(overlayBody, text); if (overlay != null) overlay.SetActive(true); }
        public void CloseOverlay() { frozen = false; if (overlay != null) overlay.SetActive(false); }
        public void MainMenu() { if (loading) return; ReTuneSession.Reset(); Load(Screen.Menu); }
        public void RestartPhase()
        {
            if (loading) return;
            CloseOverlay(); transitioning = false;
            if (screen == Screen.Performance) ResetPerformance();
            else if (screen == Screen.Parts) ClearParts();
            else { ReTuneSession.Begin(ReTuneSession.customer); Load(Screen.Shop); }
        }
        public void OriginalMelody()
        { if (!CanInteract || screen != Screen.Parts) return; noiseTarget = 1; Set(hint, "Playing the original melody."); PlayAudio(); }
        public void ClearStructure() { if (CanInteract) { ReTuneSession.structure = -1; RefreshSlots(); RestoreEmptyNoise(); } }
        public void ClearTone() { if (CanInteract) { ReTuneSession.tone = -1; RefreshSlots(); RestoreEmptyNoise(); } }
        public void ClearParts() { if (!CanInteract) return; ReTuneSession.structure = ReTuneSession.tone = -1; noiseTarget = 1; RefreshSlots(); Set(hint, "Slots cleared."); }
        void RestoreEmptyNoise() { if (ReTuneSession.structure < 0 && ReTuneSession.tone < 0) noiseTarget = 1; }
        public void DescribePart(int index) { Set(hint, story.parts[index].name + ": " + story.parts[index].description); }
        public void DropPart(int index, Vector2 position, Camera eventCamera = null)
        {
            if (!CanInteract || screen != Screen.Parts || index < 0 || index >= story.parts.Length) return;
            Camera camera = eventCamera != null ? eventCamera : worldCamera;
            if (!ReTuneWorldPointer.TryPosition(camera, position, structureSlot.transform.position.z, out var point)) return;
            Physics2D.SyncTransforms();
            bool intoStructure = structureSlot.OverlapPoint(point);
            bool intoTone = toneSlot.OverlapPoint(point);
            if (!intoStructure && !intoTone) { Set(hint, "Drop a part into a slot."); return; }
            if (story.parts[index].structure != intoStructure) { Set(hint, "Wrong slot type."); return; }
            InstallPart(index); Set(hint, "Part fitted. Select Test.");
        }
        public void InstallPart(int index)
        {
            if (index < 0 || index >= story.parts.Length) return;
            if (story.parts[index].structure) ReTuneSession.structure = index; else ReTuneSession.tone = index;
            RefreshSlots();
        }
        void RefreshSlots()
        {
            Set(structureLabel, "1. Rhythm Slot\n" + (ReTuneSession.structure < 0 ? "Drop a rhythm part here" : story.parts[ReTuneSession.structure].name + " (click to remove)"));
            Set(toneLabel, "2. Tone Slot\n" + (ReTuneSession.tone < 0 ? "Drop a tone part here" : story.parts[ReTuneSession.tone].name + " (click to remove)"));
            if (structureLabel != null) structureLabel.gameObject.SetActive(ReTuneSession.structure < 0);
            if (toneLabel != null) toneLabel.gameObject.SetActive(ReTuneSession.tone < 0);
            if (partObjects != null) foreach (var part in partObjects)
            {
                if (part == null) continue;
                part.PlaceInSlot(part.part == ReTuneSession.structure ? structureSlot : part.part == ReTuneSession.tone ? toneSlot : null);
            }
        }
        public void RemovePart(int index)
        {
            if (!CanInteract) return;
            if (ReTuneSession.structure == index) ClearStructure();
            else if (ReTuneSession.tone == index) ClearTone();
        }
        public int RecipeMatches() => (ReTuneSession.structure == Customer.structure ? 1 : 0) + (ReTuneSession.tone == Customer.tone ? 1 : 0);
        public void Trial()
        {
            if (!CanInteract || screen != Screen.Parts) return;
            int correct = RecipeMatches();
            if (correct == 2) { noiseTarget = 0; transitioning = true; transitionTime = 0; Set(hint, "Good match. Get ready to play."); }
            else if (correct == 1) { noiseTarget = .5f; Set(hint, "A little calmer. Try another part."); }
            else { noiseTarget = ReTuneSession.structure < 0 && ReTuneSession.tone < 0 ? 1 : 1.5f; Set(hint, "Try a different pair."); }
        }
        public void ResetPerformance()
        {
            time = balance = stableTime = offTime = holdTime = response = 0; note = hits = 0;
            holding = transitioning = false; noise = noiseTarget = 0;
            Set(hint, "A/D: stay green. Mouse: click or hold."); PlayAudio(); UpdatePerformanceUI();
        }
        void TickPerformance(float dt)
        {
            if (ReTuneSession.customer == 0 && !ReTuneSession.taughtClick && time >= .7f)
            { ReTuneSession.taughtClick = true; ShowOverlay("How to Play", "Use A/D to stay in the green zone.\nClick when the rings meet.\nMissed notes do not end the song."); return; }
            if (ReTuneSession.customer == 0 && note == 3 && !ReTuneSession.taughtHold && time >= Due - 1.6f)
            { ReTuneSession.taughtHold = true; ShowOverlay("Hold Notes", "Hold the left mouse button when the rings meet.\nKeep holding until the bar fills. Stay inside the box."); return; }
            time += dt;
            float input = (Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0);
            float disturbance = Customer.drift + Mathf.Sin(time * Customer.frequency) * Customer.wave;
            disturbance += Mathf.Pow(Mathf.Max(0, Mathf.Sin(time * 2)), 12) * Customer.impulse;
            balance = Mathf.Clamp(balance + (input * correctionSpeed + disturbance) * dt, -1, 1);
            if (Mathf.Abs(balance) <= safeRange) { stableTime += dt; offTime = 0; } else offTime += dt;
            if (offTime > 2) Set(hint, "Use A/D to return to green.");
            if (holding) { holdTime += dt; if (holdTime >= holdDuration) ResolveNote(true); }
            else if (note < noteCount && time > Due + hitWindow) ResolveNote(false);
            UpdatePerformanceUI();
            if (note >= noteCount)
            {
                if (Stability < minimumStability) { ResetPerformance(); Set(hint, "Too far off balance. Try again."); }
                else { ReTuneSession.hits = hits; ReTuneSession.response = response; ReTuneSession.stability = Stability; Load(Screen.Reply); }
            }
        }
        public void NoteDown()
        {
            if (!CanInteract || screen != Screen.Performance || note >= noteCount || holding) return;
            if (Mathf.Abs(time - Due) > hitWindow) { response = Mathf.Max(0, response - .04f); Set(hint, "Wait for the rings to meet."); return; }
            if (IsHold) { holding = true; holdTime = 0; Set(hint, "Keep holding..."); } else ResolveNote(true);
        }
        public void NoteRelease() { if (holding) ResolveNote(false); }
        void ResolveNote(bool success)
        {
            if (success) hits++;
            response = Mathf.Clamp01(response + (success ? .14f : -.09f)); note++; holding = false; holdTime = 0;
            Set(hint, success ? "PERFECT" : "MISS");
        }
        void UpdatePerformanceUI()
        {
            if (needle != null) needle.localRotation = Quaternion.Euler(0, 0, -balance * 90);
            if (approachRing != null) approachRing.localScale = Vector3.one * (1 + Mathf.Clamp01((Due - time) / 1.6f) * 1.8f);
            if (stabilityFill != null) stabilityFill.fillAmount = Stability;
            if (holdFill != null) holdFill.fillAmount = holding ? holdTime / holdDuration : 0;
            if (phraseFill != null) phraseFill.fillAmount = (float)note / noteCount;
            Set(stats, "Balance " + (Stability * 100).ToString("0") + "%\nHits " + hits + " / " + noteCount);
            Set(noteLabel, (IsHold ? "Hold " + holdDuration.ToString("0.00") + " s" : "Click") + "\nNote " + Mathf.Min(note + 1, noteCount) + " / " + noteCount);
        }
        void UpdateMix()
        {
            if (trackA != null) trackA.volume = .17f;
            if (trackB != null) trackB.volume = response * .17f;
            if (trackC != null) trackC.volume = noise * .08f;
            if (responseFill != null) responseFill.fillAmount = response;
            Set(mixLabel, "A Melody\nB Reply " + (response * 100).ToString("0") + "%\nC Noise " + (noise * 100).ToString("0") + "%");
        }
        AudioSource[] Sources() => new[] { trackA, trackB, trackC };
        void PlayAudio() { double start = AudioSettings.dspTime + .1; foreach (var source in Sources()) if (source != null) { source.Stop(); source.PlayScheduled(start); } }
        void OnApplicationFocus(bool focus) { if (!focus && screen != Screen.Menu && screen != Screen.Ending) Pause(); }
        void OnDisable() { foreach (var source in Sources()) if (source != null) source.Stop(); }
        static void Set(Text label, string text) { if (label != null) label.text = text; }
        public static string ScenePath(Screen target) => "Assets/ReTuneScenes/Scenes/" + new[] { "00_Start", "01_Shop", "02_Parts", "03_Performance", "04_Reply", "05_Ending" }[(int)target] + ".unity";
        void Load(Screen target)
        {
            if (loading) return;
            loading = true;
            SceneManager.LoadScene(ScenePath(target));
        }
    }
}
