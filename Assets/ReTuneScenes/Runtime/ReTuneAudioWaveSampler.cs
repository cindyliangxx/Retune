using UnityEngine;
namespace ReTune.Scenes
{
    // 只读取 A 轨采样；两条视觉线共享同一次读取，避免音量及采样时差导致错位。
    public sealed class ReTuneAudioWaveSampler
    {
        AudioClip clip;
        float[] interleaved;
        public readonly float[] Points=new float[160];
        const int WindowFrames=8192;
        bool warned;
        float peakEnvelope;
        float[] rawPoints=new float[160];
        readonly float[] energies=new float[160], smoothedEnergy=new float[160];
        public bool Read(AudioSource source,float gain,bool autoNormalize=true,float maximumBoost=12,bool smoothMusicalWave=true)
        {
            if(source==null || source.clip==null){System.Array.Clear(Points,0,Points.Length);return false;}
            var current=source.clip;
            if(current!=clip){clip=current;interleaved=new float[Mathf.Min(WindowFrames,clip.samples)*clip.channels];warned=false;peakEnvelope=0;System.Array.Clear(smoothedEnergy,0,smoothedEnergy.Length);}
            if(clip.loadState!=AudioDataLoadState.Loaded){clip.LoadAudioData();System.Array.Clear(Points,0,Points.Length);return false;}
            int frames=interleaved.Length/clip.channels;
            // 使用音轨自身的播放位置，暂停、重新开始和循环都与声音同步。
            int offset=Mathf.Clamp(source.timeSamples,0,Mathf.Max(0,clip.samples-frames));
            if(!clip.GetData(interleaved,offset))
            {
                if(!warned){Debug.LogWarning("波浪无法读取音频，请将 A 轨 Audio Import Settings 的 Load Type 设为 Decompress On Load。");warned=true;}
                System.Array.Clear(Points,0,Points.Length);return false;
            }
            float windowPeak=0;
            for(int i=0;i<Points.Length;i++)
            {
                int begin=i*frames/Points.Length,end=Mathf.Max(begin+1,(i+1)*frames/Points.Length);
                float peak=0;double energy=0;int sampleCount=0;
                for(int frame=begin;frame<end;frame++)for(int channel=0;channel<clip.channels;channel++)
                {
                    float value=interleaved[frame*clip.channels+channel];
                    if(Mathf.Abs(value)>Mathf.Abs(peak))peak=value;
                    energy+=value*value;sampleCount++;
                }
                energies[i]=(float)System.Math.Sqrt(energy/Mathf.Max(1,sampleCount));
                rawPoints[i]=peak;windowPeak=Mathf.Max(windowPeak,smoothMusicalWave?energies[i]:Mathf.Abs(peak));
            }
            // 快速跟随强音，缓慢释放；只放大显示，不改变音乐音量。
            float dt=Mathf.Clamp(Time.unscaledDeltaTime,0,.05f);
            float follow=1-Mathf.Exp(-dt/(windowPeak>peakEnvelope?.04f:.4f));
            peakEnvelope=Mathf.Lerp(peakEnvelope,windowPeak,follow);
            float scale=autoNormalize?Mathf.Clamp(.85f/Mathf.Max(.001f,peakEnvelope),1,Mathf.Max(1,maximumBoost))*Mathf.Max(0,gain)/2.5f:Mathf.Max(0,gain);
            for(int i=0;i<Points.Length;i++)
            {
                // 安静处保留安静，避免自动增益把底噪变成剧烈波浪。
                float value;
                if(smoothMusicalWave)
                {
                    // 五点平滑能量包络，消除逐采样正负峰跳变；载波由音乐播放时间推进。
                    float energy=0,weight=0;
                    for(int j=-2;j<=2;j++){float w=3-Mathf.Abs(j);energy+=energies[Mathf.Clamp(i+j,0,energies.Length-1)]*w;weight+=w;}
                    smoothedEnergy[i]=Mathf.Lerp(smoothedEnergy[i],energy/weight,1-Mathf.Exp(-dt/.09f));
                    float envelope=Mathf.Clamp01(smoothedEnergy[i]*scale);
                    value=Mathf.Sin((float)i/(Points.Length-1)*Mathf.PI*4+(float)source.timeSamples/clip.frequency*Mathf.PI*1.1f)*Mathf.Sqrt(envelope)*1.25f;
                }
                else value=rawPoints[i]*scale;
                float gate=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.001f,.004f,windowPeak));
                value*=gate;
                Points[i]=autoNormalize?(float)System.Math.Tanh(value)*.95f:Mathf.Clamp(value,-1,1);
            }
            return true;
        }
    }
}


