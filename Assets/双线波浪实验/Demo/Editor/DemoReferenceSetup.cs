using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class DemoReferenceSetup
{
    [MenuItem("ReTune/Apply Reference Look to Demo")]
    public static void Apply()
    {
        var scene=EditorSceneManager.OpenScene("Assets/Oscillogram/Demo/Demo.unity");
        const string path="Assets/Oscillogram/Demo/DemoRibbon.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null) { material=new Material(Shader.Find("ReTune/Demo Ribbon Glow")); AssetDatabase.CreateAsset(material,path); }
        var camera=Camera.main;
        var stage=camera.GetComponent<DemoReferenceStage>();
        if(stage==null) stage=camera.gameObject.AddComponent<DemoReferenceStage>();
        stage.ribbonMaterial=material;
        // Beat.wav is a demo cue, not a pair of authored music stems. Leave stem slots empty.
        foreach(var listener in Object.FindObjectsOfType<AudioListener>()) listener.enabled=listener.gameObject==camera.gameObject;
        foreach(var wave in Object.FindObjectsOfType<Oscillogram>())
        {
            var serialized=new SerializedObject(wave);
            serialized.FindProperty("beatEvent.m_PersistentCalls.m_Calls").arraySize=0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var ps=wave.GetComponentInChildren<ParticleSystem>();
            ps.GetComponent<ParticleSystemRenderer>().trailMaterial=material;
            DemoReferenceStage.ConfigureRibbon(ps,stage.ribbonWidth);
        }
        DemoReferenceStage.AlignWaveViews(Object.FindObjectsOfType<Oscillogram>());
        foreach(var volume in Object.FindObjectsOfType<Volume>())
        {
            volume.isGlobal=true;
            if(volume.sharedProfile!=null && volume.sharedProfile.TryGet<Bloom>(out var bloom))
            { bloom.tint.Override(Color.white); bloom.threshold.Override(1); bloom.intensity.Override(.65f); EditorUtility.SetDirty(volume.sharedProfile); }
        }
        camera.backgroundColor=new Color(.012f,.022f,.042f);
        EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        Debug.Log("Demo reference setup complete.");
    }
}
