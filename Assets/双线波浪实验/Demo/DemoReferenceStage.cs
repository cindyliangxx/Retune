using System;
using UnityEngine;

// Demo-only driver. Notes and both stems run on one fixed DSP timeline.
public sealed class DemoReferenceStage : MonoBehaviour
{
    [Serializable] public struct Note { public float time; public float duration; }
    public Note[] notes = { new Note { time=3 }, new Note { time=6 }, new Note { time=9, duration=1.5f }, new Note { time=13 }, new Note { time=16, duration=2 }, new Note { time=21 }, new Note { time=24 }, new Note { time=27, duration=2 } };
    public Material ribbonMaterial;
    public AudioClip trackA, trackB;
    [Tooltip("Loop the short demo stems. Turn off for full-length authored music.")]
    public bool loopTracks=true;
    public float hitWindow=.3f, gainPerHit=.14f, lossPerMiss=.09f;
    [Header("Wave rendering only — does not change the spring or beat simulation")]
    [Min(.01f)] public float ribbonWidth=.06f;
    [Range(0,1)] public float minimumBVisibility=.5f;
    [ColorUsage(true,true)] public Color blueLineHDR=new Color(.3f,.65f,1,1);
    [ColorUsage(true,true)] public Color yellowLineHDR=new Color(1,.72f,.32f,1);
    Material[] materials;
    Oscillogram[] waves;
    AudioSource a, b;
    Texture2D ring;
    double start;
    float amount, audible, balance, flash;
    int index;
    bool playing, holding;
    string message="";
    float SongTime => (float)(AudioSettings.dspTime-start);
    void OnValidate()
    {
        hitWindow=Mathf.Max(.01f,hitWindow);
        if(notes==null) notes=Array.Empty<Note>();
        for(int i=0;i<notes.Length;i++) { notes[i].time=Mathf.Max(0,notes[i].time); notes[i].duration=Mathf.Max(0,notes[i].duration); }
        Array.Sort(notes,(left,right)=>left.time.CompareTo(right.time));
    }
    void Start()
    {
        OnValidate();
        waves=FindObjectsOfType<Oscillogram>();
        Array.Sort(waves,(left,right)=>left.GetComponentInChildren<ParticleSystem>().gameObject.layer.CompareTo(right.GetComponentInChildren<ParticleSystem>().gameObject.layer));
        AlignWaveViews(waves);
        materials=new Material[waves.Length];
        for(int i=0;i<waves.Length;i++)
        {
            var ps=waves[i].GetComponentInChildren<ParticleSystem>();
            if(ps==null) continue;
            materials[i]=new Material(ribbonMaterial);
            materials[i].SetColor("_LineColor",i==0?blueLineHDR:yellowLineHDR);
            var renderer=ps.GetComponent<ParticleSystemRenderer>();
            renderer.trailMaterial=materials[i];
            ConfigureRibbon(ps,ribbonWidth);
            waves[i].enabled=false;
            waves[i].playBeatEvents=trackA==null && trackB==null;
            waves[i].ResetWave();
            var oldAudio=waves[i].GetComponent<AudioSource>();
            if(oldAudio!=null) { oldAudio.enabled=waves[i].playBeatEvents; oldAudio.volume=.25f; oldAudio.spatialBlend=0; }
        }
        a=gameObject.AddComponent<AudioSource>(); b=gameObject.AddComponent<AudioSource>();
        a.playOnAwake=b.playOnAwake=false; a.clip=trackA; b.clip=trackB;
        a.loop=b.loop=loopTracks;
        ring=new Texture2D(128,128,TextureFormat.RGBA32,false);
        for(int y=0;y<128;y++) for(int x=0;x<128;x++)
        {
            float d=Vector2.Distance(new Vector2(x,y),new Vector2(63.5f,63.5f));
            ring.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(1-Mathf.Abs(d-55)/3)));
        }
        ring.Apply();
        message="Choose Start to begin";
    }
    public void Restart()
    {
        a.Stop(); b.Stop(); amount=audible=balance=flash=0; index=0; holding=false;
        start=AudioSettings.dspTime+.3; a.volume=.65f; b.volume=0;
        if(a.clip!=null) a.PlayScheduled(start); if(b.clip!=null) b.PlayScheduled(start);
        foreach(var wave in waves) { wave.enabled=true; wave.ResetWave(); }
        playing=true; message="";
    }
    void Update()
    {
        if(!playing) return;
        float t=SongTime, dt=Time.unscaledDeltaTime;
        float input=(Input.GetKey(KeyCode.D)?1:0)-(Input.GetKey(KeyCode.A)?1:0);
        balance=Mathf.Clamp(balance+(input*.8f+Mathf.Sin(t*.7f)*.16f)*dt,-1,1);
        audible=Mathf.MoveTowards(audible,amount,dt*.6f); b.volume=audible*.65f;
        flash=Mathf.MoveTowards(flash,0,dt*3.5f);
        for(int i=0;i<materials.Length;i++) if(materials[i]!=null)
        {
            materials[i].SetFloat("_Flash",flash);
            materials[i].SetColor("_LineColor",i==0?blueLineHDR:yellowLineHDR);
            materials[i].SetFloat("_Strength",i==0?1:Mathf.Lerp(minimumBVisibility,1,audible));
        }
        if(index>=notes.Length) { holding=false; message="Complete — restart or return to start"; return; }
        Note note=notes[index];
        if(Input.GetMouseButtonDown(0) && HitArea().Contains(Input.mousePosition))
        {
            if(Mathf.Abs(t-note.time)<=hitWindow)
            { if(note.duration>0) holding=true; else Resolve(true); }
            else { amount=Mathf.Max(0,amount-lossPerMiss); message="Early / late"; }
        }
        if(holding)
        {
            if(!Input.GetMouseButton(0)) Resolve(false);
            else if(t>=note.time+note.duration) Resolve(true);
        }
        else if(index<notes.Length && t>notes[index].time+hitWindow) Resolve(false);
    }
    public static void ConfigureRibbon(ParticleSystem ps,float width)
    {
        var main=ps.main; main.startColor=Color.white;
        var trails=ps.trails;
        // Width used to be multiplied by startSize (.02 * .13), making the ribbon almost invisible.
        trails.sizeAffectsWidth=false;
        trails.widthOverTrail=new ParticleSystem.MinMaxCurve(Mathf.Max(.01f,width));
        trails.minVertexDistance=.015f;
        trails.colorOverTrail=new ParticleSystem.MinMaxGradient(Color.white);
        trails.colorOverLifetime=new ParticleSystem.MinMaxGradient(Color.white);
    }
    public static void AlignWaveViews(Oscillogram[] waveObjects)
    {
        foreach(var wave in waveObjects)
        {
            var ps=wave.GetComponentInChildren<ParticleSystem>();
            if(ps==null) continue;
            foreach(var camera in Camera.allCameras)
            {
                if(camera.CompareTag("MainCamera") || camera.cullingMask!=(1<<ps.gameObject.layer)) continue;
                // Each isolated wave keeps its own world position and spring anchor.
                // Matching the camera offset puts their rest positions on the same screen baseline.
                camera.transform.position=new Vector3(wave.transform.position.x-2.5f,
                    wave.transform.position.y, camera.transform.position.z);
                camera.orthographic=true; camera.orthographicSize=2;
            }
        }
    }
    void Resolve(bool success)
    {
        amount=Mathf.Clamp01(amount+(success?gainPerHit:-lossPerMiss));
        if(success) flash=1;
        message=success?"Perfect":"Miss"; index++; holding=false;
    }
    Rect HitArea() => new Rect(Screen.width*.55f-65,Screen.height*.5f-65,130,130);
    void OnGUI()
    {
        GUI.color=new Color(.85f,.9f,1);
        if(GUI.Button(new Rect(20,20,130,32),playing?"Restart stage":"Start stage")) Restart();
        if(GUI.Button(new Rect(165,20,130,32),"Return to start"))
        { playing=false; holding=false; a.Stop(); b.Stop(); foreach(var wave in waves) { wave.enabled=false; var cue=wave.GetComponent<AudioSource>(); if(cue!=null) cue.Stop(); } message="Choose Start to begin"; }
        GUI.Label(new Rect(20,65,600,30),"A / D: balance    Mouse: click / hold the ring");
        GUI.Label(new Rect(20,95,700,30),$"B stem {amount:P0}    Balance {balance:F2}    {message}");
        if(trackA==null && trackB==null) GUI.Label(new Rect(20,150,700,25),"Beat preview: assign Track A and Track B on Main Camera for music.");
        GUI.color=new Color(.15f,.2f,.27f); GUI.DrawTexture(new Rect(20,130,240,8),Texture2D.whiteTexture);
        GUI.color=new Color(.4f,.8f,.65f); GUI.DrawTexture(new Rect(116,130,48,8),Texture2D.whiteTexture);
        GUI.color=Color.white; GUI.DrawTexture(new Rect(138+balance*118,126,4,16),Texture2D.whiteTexture);
        if(!playing || index>=notes.Length || ring==null) return;
        float s=Mathf.Min(Screen.width,Screen.height);
        Vector2 center=new Vector2(Screen.width*.55f,Screen.height*.5f);
        DrawRing(center,56,new Color(.7f,.88f,1,.8f));
        float approach=Mathf.Clamp01((notes[index].time-SongTime)/1.8f);
        DrawRing(center,56+approach*s*.16f,new Color(1,.83f,.5f,.85f));
        if(flash>0) DrawRing(center,65+(1-flash)*85,new Color(1,.95f,.75f,flash));
        if(notes[index].duration>0)
        {
            float progress=holding?Mathf.Clamp01((SongTime-notes[index].time)/notes[index].duration):0;
            GUI.Label(new Rect(center.x-70,center.y+75,220,25),$"Hold {notes[index].duration:F1}s   {progress:P0}");
        }
    }
    void DrawRing(Vector2 center,float size,Color color)
    { GUI.color=color; GUI.DrawTexture(new Rect(center.x-size,center.y-size,size*2,size*2),ring); GUI.color=Color.white; }
    void OnDestroy()
    { if(materials!=null) foreach(var material in materials) if(material!=null) Destroy(material); if(ring!=null) Destroy(ring); }
}
