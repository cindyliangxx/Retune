using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using ReTune.Scenes;

public static class ReTunePartsUpgrade
{
    const string ScenePath="Assets/ReTuneScenes/Scenes/02_Parts.unity";
    const string ArtPath="Assets/美术资产/retune零件/滚轮动画/";
    const string Output="Assets/ReTuneScenes/Animation/";
    [MenuItem("ReTune/Scenes/Configure Animated Replacement Parts")]
    public static void Configure()
    {
        var scene=EditorSceneManager.OpenScene(ScenePath);
        var controller=UnityEngine.Object.FindObjectOfType<ReTuneSceneController>();
        var art=controller.GetComponent<ReTunePartsArt>();
        var original=UnityEngine.Object.FindObjectsOfType<MusicBoxController>().First(x=>x.GetComponentInParent<ReTunePartDrag>()==null&&x.rollerAnimator!=null);
        if(art.roller!=original.transform){var old=art.roller.GetComponent<SpriteRenderer>();if(old!=null)old.enabled=false;}
        art.originalRoller=original;art.roller=original.transform;
        var soft=Build("SoftRoller",new[]{"2026-11-01 221238.png","2026-11-01 221242.png","2026-11-01 221246.png"});
        var warm=Build("WarmRoller",new[]{"2026-11-01 221333.png","2026-11-01 221338.png","2026-11-01 221342.png"});
        foreach(var part in controller.partObjects)
        {
            var renderers=part.GetComponentsInChildren<SpriteRenderer>(true);
            var visible=renderers.Last(x=>x.enabled);
            // Each drag target uses the visible sprite as its hit area.
            foreach(var collider in part.GetComponentsInChildren<Collider2D>(true))UnityEngine.Object.DestroyImmediate(collider);
            var box=visible.gameObject.AddComponent<BoxCollider2D>();box.size=visible.sprite.bounds.size;box.offset=visible.sprite.bounds.center;
            if(part.part<4)continue;
            var clip=part.part==4?soft:warm;
            var animator=visible.GetComponent<Animator>();if(animator==null)animator=visible.gameObject.AddComponent<Animator>();
            var assetPath=Output+(part.part==4?"SoftRoller":"WarmRoller")+".controller";
            var animatorController=AssetDatabase.LoadAssetAtPath<AnimatorController>(assetPath);
            if(animatorController==null)animatorController=AnimatorController.CreateAnimatorControllerAtPathWithClip(assetPath,clip);
            else {var states=animatorController.layers[0].stateMachine.states;states[0].state.motion=clip;EditorUtility.SetDirty(animatorController);}
            animator.runtimeAnimatorController=animatorController;animator.applyRootMotion=false;
            var musicBox=visible.GetComponent<MusicBoxController>();if(musicBox==null)musicBox=visible.gameObject.AddComponent<MusicBoxController>();
            musicBox.rollerAnimator=animator;musicBox.tines=Array.Empty<MusicBoxTineVisual>();
            visible.sprite=(Sprite)AnimationUtility.GetObjectReferenceCurve(clip,AnimationUtility.GetObjectReferenceCurveBindings(clip)[0])[0].value;
            box.size=visible.sprite.bounds.size;box.offset=visible.sprite.bounds.center;
        }
        AlignSlot(controller.structureSlot,art.gear.GetComponent<SpriteRenderer>());
        AlignSlot(controller.toneSlot,original.GetComponent<SpriteRenderer>());
        art.rollerArea=controller.toneSlot;
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Validate();
        Debug.Log("PARTS_UPGRADE_CONFIGURED");
    }
    static AnimationClip Build(string name,string[] files)
    {
        var sprites=files.Select(file=>
        {
            string path=ArtPath+file;
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit=100;importer.alphaIsTransparency=true;
            // Same crop and pivot for every frame: no jumping or wobbling around the axle.
            importer.spritesheet=new[]{new SpriteMetaData{name=name+"_"+Path.GetFileNameWithoutExtension(file),rect=new Rect(308,509,495,252),pivot=new Vector2(.5f,.5f),alignment=0}};
            importer.SaveAndReimport();return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Single();
        }).ToArray();
        string output=Output+name+".anim";
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(output);
        var template=AssetDatabase.LoadAssetAtPath<AnimationClip>(Output+"roller_Animation.anim");
        if(clip==null){clip=UnityEngine.Object.Instantiate(template);AssetDatabase.CreateAsset(clip,output);}
        else EditorUtility.CopySerialized(template,clip);
        clip.name=name;
        var binding=AnimationUtility.GetObjectReferenceCurveBindings(clip).Single();
        var keys=AnimationUtility.GetObjectReferenceCurve(template,binding);
        for(int i=0;i<keys.Length;i++)keys[i].value=sprites[i%sprites.Length];
        AnimationUtility.SetObjectReferenceCurve(clip,binding,keys);
        var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;AnimationUtility.SetAnimationClipSettings(clip,settings);
        EditorUtility.SetDirty(clip);return clip;
    }
    static void AlignSlot(Collider2D slot,SpriteRenderer renderer)
    {
        slot.transform.position=renderer.bounds.center;slot.offset=Vector2.zero;
        if(slot is BoxCollider2D box)box.size=new Vector2(renderer.bounds.size.x/Mathf.Abs(slot.transform.lossyScale.x),renderer.bounds.size.y/Mathf.Abs(slot.transform.lossyScale.y));
    }
    public static void Validate()
    {
        var controller=UnityEngine.Object.FindObjectOfType<ReTuneSceneController>();
        var art=controller.GetComponent<ReTunePartsArt>();
        if(art.originalRoller==null||art.roller!=art.originalRoller.transform)throw new Exception("Original roller is not bound");
        foreach(var part in controller.partObjects)
        {
            if(part.GetComponentsInChildren<Collider2D>(true).Length!=1)throw new Exception("Expected one collider per part");
            if(part.part>=4)
            {
                var animator=part.GetComponentInChildren<Animator>();
                if(animator==null||animator.runtimeAnimatorController==null)throw new Exception("Missing roller animation");
                var clip=animator.runtimeAnimatorController.animationClips.Single();
                if(!AnimationUtility.GetAnimationClipSettings(clip).loopTime)throw new Exception("Roller must loop");
                if(AnimationUtility.GetObjectReferenceCurve(clip,AnimationUtility.GetObjectReferenceCurveBindings(clip).Single()).Select(k=>k.value).Distinct().Count()!=3)throw new Exception("Expected three frames");
            }
        }
        Debug.Log("PARTS_UPGRADE_VALIDATION_PASS");
    }
}
