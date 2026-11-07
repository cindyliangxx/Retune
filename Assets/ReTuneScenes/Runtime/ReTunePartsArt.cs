using UnityEngine;
using DG.Tweening;
namespace ReTune.Scenes
{
    public sealed class ReTunePartsArt : MonoBehaviour
    {
        public ReTuneSceneController controller;
        public Transform small, windingKey, roller;
        // public Transform gear;
        public Collider2D rollerArea, windingArea;
        public float windingDuration=.7f, playbackDuration=5f;
        public MusicBoxController originalRoller;
        public MusicBoxController CurrentRoller { get; private set; }

        float winding, playing;
        bool jamming;
        float jamTime;
        Quaternion jamKeyRest, jamGearRest, jamSmallRest;
        const float jamDuration = 0.85f;
        Quaternion keyRest;
        SpriteRenderer defaultGear, defaultRoller;
        GearRotator gearDriver, smallDriver;
        bool gearDriverEnabled, smallDriverEnabled;
        
        bool wasRunning = false; // 监控状态改变，只在停下时重置一次

        [Header("选错零件卡壳效果（滚轮对齿轮错/滚轮错齿轮对/两个都错）")]
        public float goodDuration = 2f;      // 先正常转动多久（秒）
        public float glitchInterval = 0.5f;  // 卡顿间隔（秒）
        public float glitchDuration = 0.2f;  // 每次卡顿持续（秒）
        public bool glitchy;                 // 本次试调是否选错（运行时由 Trial 设置）

        [Header("八音盒抖动")]
        public float shakeStrength = 6f;     // 抖动强度（角度）
        public float shakeDuration = 0.25f;  // 每次抖动时长

        float glitchTimer;
        bool rollerStuck;
        float rollerStuckTimer;
        bool shaking;
        Vector3 shakeRestPosition;

        public bool IsBusy => winding>0 || playing>0 || jamming;
        public bool IsPlaying => playing>0;

        void Awake()
        {
            AssignReferences(); // 动态补齐引用：没在 Inspector 拖的，按名字自动找

            // 暂时去掉大齿轮：直接隐藏（避免它的 GearRotator 自己持续旋转）
            if (controller != null && controller.musicBox != null)
            {
                var bigGear = DirectChild(controller.musicBox, "八音盒齿轮_大");
                if (bigGear != null) bigGear.gameObject.SetActive(false);
            }

            if(windingKey!=null) keyRest=windingKey.localRotation;
            // if(gear!=null) defaultGear=gear.GetComponent<SpriteRenderer>();
            if(roller!=null) defaultRoller=roller.GetComponent<SpriteRenderer>();
            if(originalRoller==null && roller!=null) originalRoller=roller.GetComponentInChildren<MusicBoxController>();
            
            // if(gear!=null) gearDriver=gear.GetComponent<GearRotator>();
            if(small!=null) smallDriver=small.GetComponent<GearRotator>();
            
            // gearDriverEnabled=gearDriver!=null&&gearDriver.enabled;
            smallDriverEnabled=smallDriver!=null&&smallDriver.enabled;
            
            SetSpeeds(false);
        }

        // 动态补齐引用
        void AssignReferences()
        {
            Transform box = controller != null ? controller.musicBox : null;

            if (box != null)
            {
                // if (gear == null) gear = DirectChild(box, "八音盒齿轮_大");
                if (small == null) small = DirectChild(box, "八音盒齿轮_小");
                if (windingKey == null) windingKey = DirectChild(box, "winding-key");
            }

            if (roller == null) roller = SceneRootByName("滚轮控制器");
            if (originalRoller == null && roller != null) originalRoller = roller.GetComponent<MusicBoxController>();
        }

        Transform DirectChild(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var c = parent.GetChild(i);
                if (c.name == name) return c;
            }
            return null;
        }

        Transform SceneRootByName(string name)
        {
            foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root.name == name) return root.transform;
            }
            return null;
        }

        public void FitInstalledPart(ReTunePartDrag part,Collider2D slot)
        {
            var reference = roller; // 大齿轮已去掉，只按滚轮参考
            if (reference == null) return;

            SpriteRenderer visual=null;
            foreach(var candidate in part.GetComponentsInChildren<SpriteRenderer>(true))
                if(candidate.enabled&&candidate.gameObject.activeInHierarchy&&candidate.sprite!=null)visual=candidate;
                
            var original=reference.GetComponent<SpriteRenderer>();
            if(visual==null || original==null || visual.sprite==null)return;
            
            float width=original.sprite.bounds.size.x * Mathf.Abs(reference.lossyScale.x);
            float current=visual.sprite.bounds.size.x * Mathf.Abs(visual.transform.lossyScale.x);
            float height=original.sprite.bounds.size.y * Mathf.Abs(reference.lossyScale.y);
            float currentHeight=visual.sprite.bounds.size.y * Mathf.Abs(visual.transform.lossyScale.y);
            
            if(current>0 && currentHeight>0)
            {
                float scaleMultiplier = Mathf.Min(width/current, height/currentHeight);
                part.transform.localScale = part.OriginalScale * scaleMultiplier; 
            }
        }

        public void RefreshInstalledParts()
        {
            // if(defaultGear==null && gear!=null) defaultGear=gear.GetComponent<SpriteRenderer>();
            if(defaultRoller==null && roller!=null) defaultRoller=roller.GetComponent<SpriteRenderer>();
            
            // 八音盒齿轮/滚轮作为固定底座，零件装入后保持可见（不再隐藏）
            // if(defaultGear!=null) defaultGear.enabled=true;
            if(defaultRoller!=null) defaultRoller.enabled=true;
            
            SetSpeeds(false);
        }

        public void CancelPlayback()
        {
            bool wasJamming = jamming;
            winding=playing=0;
            jamming=false;
            StopShake();
            if(windingKey!=null) windingKey.localRotation=keyRest;
            if(wasJamming)
            {
                // if(gear!=null) gear.localRotation=jamGearRest;
                if(small!=null) small.localRotation=jamSmallRest;
            }
            SetSpeeds(false);
            controller.StopPartsAudio();
        }

        public void BeginWinding()
        {
            if(!controller.CanInteract||IsBusy||AnyDragging())return;
            if(controller.PartsIncomplete){ controller.Trial(); return; } // 零件不齐 → Trial 内部走卡住效果
            winding=Mathf.Max(.05f,windingDuration);
        }

        public bool BeginPreview()
        {
            if(IsBusy || AnyDragging()) return false;
            playing=Mathf.Max(.1f,playbackDuration);
            glitchy=false;
            glitchTimer=0f;
            rollerStuck=false;
            rollerStuckTimer=0f;
            SetSpeeds(false);
            return true;
        }

        public void PlayJam()
        {
            if (IsBusy) return;
            if (windingKey != null) jamKeyRest = windingKey.localRotation;
            // if (gear != null) jamGearRest = gear.localRotation;
            if (small != null) jamSmallRest = small.localRotation;
            jamming = true;
            jamTime = 0f;
        }

        void ApplyJam(float t)
        {
            float outEnd = 0.32f, stuckEnd = 0.55f;
            float keyAngle, gearAngle, smallAngle;
            if (t < outEnd)
            {
                float u = QuadOut(t / outEnd);
                keyAngle = 180f * u; gearAngle = -22f * u; smallAngle = 22f * u;
            }
            else if (t < stuckEnd)
            {
                keyAngle = 180f; gearAngle = -22f; smallAngle = 22f;
            }
            else
            {
                float u = QuadInOut((t - stuckEnd) / (1f - stuckEnd));
                keyAngle = 180f * (1f - u); gearAngle = -22f * (1f - u); smallAngle = 22f * (1f - u);
            }
            if (windingKey != null) windingKey.localRotation = jamKeyRest * Quaternion.Euler(keyAngle, 0f, 0f);
            // if (gear != null) gear.localRotation = jamGearRest * Quaternion.Euler(0f, 0f, gearAngle);
            if (small != null) small.localRotation = jamSmallRest * Quaternion.Euler(0f, 0f, smallAngle);
        }

        static float QuadOut(float u) => 1f - (1f - u) * (1f - u);
        static float QuadInOut(float u) => u < 0.5f ? 2f * u * u : 1f - Mathf.Pow(-2f * u + 2f, 2f) * 0.5f;

        bool AnyDragging(){foreach(var part in controller.partObjects)if(part!=null&&part.IsDragging)return true;return false;}

        void UpdateGlitch(float dt)
        {
            if (rollerStuck)
            {
                rollerStuckTimer -= dt;
                if (rollerStuckTimer <= 0f) rollerStuck = false;
            }

            glitchTimer -= dt;
            if (glitchTimer <= 0f)
            {
                glitchTimer = glitchInterval;
                TriggerStutterOnce();
            }
        }

        void TriggerStutterOnce()
        {
            // 齿轮卡顿（带回轮）
            // if (gearDriver != null) gearDriver.TriggerStutter(glitchDuration, 1f); // 大齿轮已去掉
            if (smallDriver != null) smallDriver.TriggerStutter(glitchDuration, 1f);
            foreach (var part in controller.partObjects)
            {
                if (part == null) continue;
                if (part.part == ReTuneSession.structure)
                {
                    var gr = part.GetComponent<GearRotator>();
                    if (gr != null) gr.TriggerStutter(glitchDuration, 1f);
                }
            }

            // 滚轮卡顿：调用 TriggerGlitchEffect，并用 rollerStuck 让 SetSpeeds 保持冻结（音梳随之停住）
            rollerStuck = true;
            rollerStuckTimer = glitchDuration;
            if (CurrentRoller != null) CurrentRoller.TriggerGlitchEffect("Stutter");

            StartShake();
        }

        void StartShake()
        {
            if (shaking || controller == null || controller.musicBox == null) return;
            shaking = true;
            shakeRestPosition = controller.musicBox.localPosition;
            controller.musicBox.DOKill();
            controller.musicBox.DOShakePosition(shakeDuration, new Vector3(shakeStrength, shakeStrength, 0f), 30, 90f, false, false)
                .SetLoops(-1, LoopType.Restart);
        }

        void StopShake()
        {
            if (!shaking) return;
            shaking = false;
            if (controller != null && controller.musicBox != null)
            {
                controller.musicBox.DOKill();
                controller.musicBox.localPosition = shakeRestPosition;
            }
        }
        
        void SetSpeeds(bool paused)
        {
            bool running = playing > 0 && !paused;
            
            // 【核心修复1】只在播放停止的瞬间，复位所有音梳一次，绝不在每帧强杀
            if (wasRunning && !running)
            {
                if (originalRoller != null) originalRoller.ResetAllTines();
                foreach(var part in controller.partObjects)
                {
                    if (part == null) continue;
                    var box = part.GetComponentInChildren<MusicBoxController>();
                    if (box != null) box.ResetAllTines();
                }
            }
            wasRunning = running;

            // if(gearDriver!=null)gearDriver.enabled=gearDriverEnabled&&running; // 大齿轮已去掉
            if(smallDriver!=null)smallDriver.enabled=smallDriverEnabled&&running;
            
            float rollerSpeed = originalRoller != null ? originalRoller.baseSpeed : 1f;
            if (rollerSpeed <= 0.01f) rollerSpeed = 1f;

            // 【核心修复2】当没有装音色零件时，才控制 default 的 originalRoller
            bool hasTonePart = ReTuneSession.tone >= 0;
            if(originalRoller != null)
            {
                originalRoller.SetRollerSpeed(!hasTonePart && running ? rollerSpeed : 0);
            }
            CurrentRoller = !hasTonePart ? originalRoller : null;

            if(controller.partObjects==null)return;
            
            foreach(var part in controller.partObjects)
            {
                if(part==null)continue;
                
                var animator=part.GetComponentInChildren<Animator>();
                if(animator==null)continue;
                
                bool installedAsTone = part.part == ReTuneSession.tone;
                bool installedAsStructure = part.part == ReTuneSession.structure;
                bool isInstalled = installedAsTone || installedAsStructure;

                if (!isInstalled || part.IsDragging || paused || rollerStuck)
                {
                    animator.speed = 0;
                }
                else
                {
                    animator.speed = running ? rollerSpeed : 0;
                }
                
                var box = animator.GetComponent<MusicBoxController>();
                if(box != null)
                {
                    if(isInstalled && running && !part.IsDragging)
                    {
                        CurrentRoller = box;
                        
                        // 【核心修复3】不要强行覆盖数组！只在它为空时才借用，防止破坏它自己的配置
                        if ((box.tines == null || box.tines.Length == 0) && originalRoller != null)
                        {
                            box.tines = originalRoller.tines;
                        }
                    }
                }
            }

            // 结构零件齿轮：只有装入节奏槽的那个，在播放时随大小齿轮一起转动
            foreach(var part in controller.partObjects)
            {
                if(part==null)continue;
                bool installedAsStructure = part.part == ReTuneSession.structure;
                part.SetGearSpinning(installedAsStructure && running && !part.IsDragging);
            }
        }

        void Update()
        {
            if(controller.PartsAnimationPaused){SetSpeeds(true);return;}
            bool pointer=ReTuneWorldPointer.TryPosition(controller.worldCamera,Input.mousePosition,0,out var point);
            if(controller.CanInteract&&windingArea!=null&&pointer&&windingArea.OverlapPoint(point)&&Input.GetMouseButtonDown(0))BeginWinding();
            
            float dt=Time.unscaledDeltaTime;
            if(winding>0)
            {
                winding=Mathf.Max(0,winding-dt);
                if(windingKey!=null) windingKey.localRotation=keyRest*Quaternion.Euler(360*(1-winding/Mathf.Max(.05f,windingDuration)),0,0);
                if(winding==0)
                {
                    if(windingKey!=null) windingKey.localRotation=keyRest;
                    controller.Trial(); 
                }
            }
            if(playing>0)
            {
                playing=Mathf.Max(0,playing-dt);
                if(glitchy && (playbackDuration - playing) >= goodDuration)
                {
                    UpdateGlitch(dt);
                }
                if(playing==0)
                {
                    controller.StopPartsAudio();
                    glitchy=false;
                    rollerStuck=false;
                    StopShake();
                }
            }
            if(jamming)
            {
                jamTime=Mathf.Min(jamDuration,jamTime+dt);
                ApplyJam(jamTime/jamDuration);
                if(jamTime>=jamDuration){jamming=false;ApplyJam(1f);}
            }
            SetSpeeds(false);
        }

        void OnDisable()
        {
            if(windingKey!=null) windingKey.localRotation=keyRest;
            if(jamming){ /* if(gear!=null) gear.localRotation=jamGearRest; */ if(small!=null) small.localRotation=jamSmallRest; jamming=false; }
            SetSpeeds(true);
        }
    }
}