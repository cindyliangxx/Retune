using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
namespace ReTune.Scenes
{
    public sealed class ReTunePerformanceStage : MonoBehaviour
    {
        public ReTuneSceneController controller;
        [Header("半圆轨道：角度范围 0 到 180")]
        public Transform playerGear, targetGear;
        public Vector2 trackCenter=new Vector2(-2,-1.6f);
        [Min(.1f)] public float trackRadius=1.25f, moveSpeed=65, tolerance=13;
        [Range(0,180)] public float playerAngle=40, targetAngle=90;
        [Header("进度：第一关默认离开目标不衰减")]
        public float gainRate=6, decayRate=3;
        public bool decayWhenOutside;
        [Header("宽范围与精确范围：单位为角度，精确范围内加速")]
        [Min(0)] public float preciseTolerance=5, preciseMultiplier=1.8f;
        [Header("轻微自动漂移；A/D 持续修正，边缘自动反向")]
        public bool autoDrift=true;
        [Min(0)] public float driftSpeed=9;
        [Header("随机目标区域切换间隔，秒")]
        public Vector2 targetChangeInterval=new Vector2(6,9);
        public ReTuneDialZone broadZone, preciseZone;
        float targetTimer, driftDirection=1;
        public bool IsPrecise => Mathf.Abs(playerAngle-targetAngle)<=Mathf.Min(preciseTolerance,tolerance);
        [Header("QTE：点击目标，随机等待后出现缩圈")]
        public ReTuneQTESchedule schedule;
        [Header("邮差固定谱面：使用 A 轨采样时钟，其他顾客保留随机策略")]
        public ReTuneBeatmap postmanBeatmap;
        int beatIndex, audioLoops;
        float previousAudioTime;
        bool UsesBeatmap => ReTuneSession.customer==0 && postmanBeatmap!=null && postmanBeatmap.hitTimes!=null && postmanBeatmap.hitTimes.Length>0 && controller.trackA!=null && controller.trackA.clip!=null;
        float BeatDue => postmanBeatmap.hitTimes[beatIndex%postmanBeatmap.hitTimes.Length]+postmanBeatmap.offsetSeconds+(beatIndex/postmanBeatmap.hitTimes.Length)*controller.trackA.clip.length;
        float SongTime()
        {
            var source=controller.trackA;
            float current=(float)source.timeSamples/source.clip.frequency;
            if(current<previousAudioTime-.5f)audioLoops++;
            previousAudioTime=current;return current+audioLoops*source.clip.length;
        }
        void TickBeatmap()
        {
            if(!controller.trackA.isPlaying){QTEActive=false;return;}
            float now=SongTime();
            if(now>BeatDue+hitWindow){Resolve(false);}
            float due=BeatDue;
            QTEActive=now>=due-approachDuration && now<=due+hitWindow;
            age=approachDuration+now-due;
        }
        public float approachDuration=1.5f, hitWindow=.22f;
        public UnityEvent OnQTESuccess=new UnityEvent(), OnQTEFailure=new UnityEvent(), StageClear=new UnityEvent();
        [Header("复用 Oscillogram 波浪，默认反馈仅作用于波形")]
        public Oscillogram waveA, waveB;
        public bool enableWaveFeedback=true, enableTargetKick=false;
        public float targetKick=12;
        [Min(.01f)] public float frequency=.6f;
        public LineRenderer track;
        public RectTransform pointerVisual, targetVisual;
        [Header("真实音乐采样驱动：共用 A 轨轮廓，保留满进度重合")]
        public bool useAudioWaveform=true;
        [Min(0)] public float audioWaveGain=2.5f;
        public bool normalizeAudioWave=true;
        [Tooltip("使用音乐能量驱动平滑波浪，关闭后显示原始采样尖峰")]
        public bool smoothMusicalWave=true;
        [Min(1)] public float maximumWaveBoost=12;
        readonly ReTuneAudioWaveSampler audioWave=new ReTuneAudioWaveSampler();
        public float Progress { get; private set; }
        public bool QTEActive { get; private set; }
        float wait, age, phase, elapsed, alignedTime, successPulse, missPulse, clearDelay;
        int hits;
        bool initialized, completed;
        Image pointerImage;
        public void ResetStage()
        {
            playerAngle=40;ChooseTarget();driftDirection=1;Progress=0;wait=1;age=phase=elapsed=alignedTime=successPulse=missPulse=clearDelay=0;
            hits=0;beatIndex=audioLoops=0;previousAudioTime=0;QTEActive=completed=false;initialized=true;
            if(pointerVisual!=null)pointerImage=pointerVisual.GetComponent<Image>();
            if(controller.holdFill!=null)controller.holdFill.transform.parent.gameObject.SetActive(false);
            Present();
        }
        void Update()
        {
            if(!initialized || !controller.CanInteract)return;
            float dt=Time.deltaTime;
            if(completed){clearDelay+=dt;if(clearDelay>=.8f)controller.CompleteWaveStage(hits,elapsed>0?alignedTime/elapsed:1);return;}
            elapsed+=dt;
            targetTimer-=dt;if(targetTimer<=0)ChooseTarget();
            float correction=((Input.GetKey(KeyCode.A)?1:0)-(Input.GetKey(KeyCode.D)?1:0))*moveSpeed;
            playerAngle=Mathf.Clamp(playerAngle+(correction+(autoDrift?driftDirection*driftSpeed:0))*dt,0,180);
            if(playerAngle<=0)driftDirection=1;else if(playerAngle>=180)driftDirection=-1;
            bool aligned=Mathf.Abs(playerAngle-targetAngle)<=tolerance;
            if(aligned){alignedTime+=dt;Progress+=Mathf.Max(0,gainRate)*(IsPrecise?Mathf.Max(1,preciseMultiplier):1)*dt;}
            else if(decayWhenOutside)Progress-=Mathf.Max(0,decayRate)*dt;
            Progress=Mathf.Clamp(Progress,0,100);
            phase=Mathf.Repeat(phase+dt*Mathf.Max(.01f,frequency)*Mathf.PI*2,Mathf.PI*2);
            successPulse=Mathf.MoveTowards(successPulse,0,dt*2);missPulse=Mathf.MoveTowards(missPulse,0,dt*2);
            if(UsesBeatmap)TickBeatmap();
            else if(QTEActive){age+=dt;if(age>approachDuration+hitWindow)Resolve(false);}
            else {wait-=dt;if(wait<=0){QTEActive=true;age=0;}}
            if(Progress>=100){Progress=100;completed=true;QTEActive=false;successPulse=missPulse=0;StageClear.Invoke();}
            Present();
        }
        void ChooseTarget()
        {
            float margin=Mathf.Clamp(tolerance,1,80);
            float previous=targetAngle;
            targetAngle=Random.Range(margin,180-margin);
            // 避免换区后几乎留在原处。
            if(Mathf.Abs(targetAngle-previous)<margin*2)targetAngle=previous<90?180-margin:margin;
            targetTimer=Random.Range(Mathf.Max(1,targetChangeInterval.x),Mathf.Max(Mathf.Max(1,targetChangeInterval.x),targetChangeInterval.y));
        }
        public void ClickQTE()
        {
            if(!initialized || !controller.CanInteract || completed || !QTEActive)return;
            bool success=UsesBeatmap?Mathf.Abs(SongTime()-BeatDue)<=Mathf.Max(.01f,hitWindow):Mathf.Abs(age-approachDuration)<=Mathf.Max(.01f,hitWindow);
            Resolve(success);Present();
        }
        void Resolve(bool success)
        {
            if(UsesBeatmap)beatIndex++;
            QTEActive=false;wait=schedule!=null?schedule.NextInterval():Random.Range(2.5f,4.5f);
            if(success){hits++;if(enableWaveFeedback)successPulse=1;OnQTESuccess.Invoke();}
            else {if(enableWaveFeedback)missPulse=1;if(enableTargetKick)targetAngle=Mathf.Clamp(targetAngle+Random.Range(-targetKick,targetKick),0,180);OnQTEFailure.Invoke();}
            if(controller.hint!=null)controller.hint.text=success?"PERFECT":"MISS";
        }
        void Present()
        {
            if(playerGear!=null)playerGear.position=Point(playerAngle);
            if(targetGear!=null)targetGear.position=Point(targetAngle);
            if(pointerVisual!=null)pointerVisual.localRotation=Quaternion.Euler(0,0,playerAngle-40);
            if(targetVisual!=null)targetVisual.localRotation=Quaternion.Euler(0,0,targetAngle-40);
            if(broadZone!=null){broadZone.innerRadius=276;broadZone.outerRadius=282;}
            if(preciseZone!=null){preciseZone.innerRadius=267;preciseZone.outerRadius=274;}
            if(broadZone!=null)broadZone.SetRange(targetAngle,tolerance,new Color(.58f,.78f,.85f,.95f));
            if(preciseZone!=null)preciseZone.SetRange(targetAngle,Mathf.Min(preciseTolerance,tolerance),new Color(1,.82f,.4f,1));
            if(pointerImage!=null)pointerImage.color=IsPrecise?new Color(1,.94f,.78f,1):Color.white;
            float amount=Progress/100;
            if(useAudioWaveform)
            {
                audioWave.Read(controller.trackA,audioWaveGain,normalizeAudioWave,maximumWaveBoost,smoothMusicalWave);
                if(waveA!=null)waveA.DrawAudioWave(audioWave.Points,amount,successPulse*.18f,missPulse*.12f,phase);
                if(waveB!=null)waveB.DrawAudioWave(audioWave.Points,1,0,0,phase);
            }
            else
            {
                if(waveA!=null)waveA.DrawSharedWave(phase,amount,successPulse*.18f,missPulse*.12f);
                if(waveB!=null)waveB.DrawSharedWave(phase,1,0,0);
            }
            if(controller.approachRing!=null){controller.approachRing.gameObject.SetActive(QTEActive);controller.approachRing.localScale=Vector3.one*(1+Mathf.Clamp01(1-age/Mathf.Max(.1f,approachDuration))*2);}
            if(controller.phraseFill!=null)controller.phraseFill.fillAmount=amount;
            if(controller.stabilityFill!=null)controller.stabilityFill.fillAmount=amount;
            if(controller.stats!=null)controller.stats.text=$"Harmony {Progress:0}%\nPerfect {hits}";
            if(controller.noteLabel!=null)controller.noteLabel.text=QTEActive?"Click when rings meet":"Wait for the next ring";
            controller.SetWaveMix(amount);
        }
        public Vector3 Point(float angle){float a=angle*Mathf.Deg2Rad;return new Vector3(trackCenter.x+Mathf.Cos(a)*trackRadius,trackCenter.y+Mathf.Sin(a)*trackRadius,0);}
    }
}









