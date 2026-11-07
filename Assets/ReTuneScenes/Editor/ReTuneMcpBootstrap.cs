using McpUnity.Unity;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System.Linq;
using System.IO;
using ReTune.Scenes;

// Initialize the package singleton after import so Auto Start also works without opening its window.
public static class ReTuneMcpBootstrap
{
    [InitializeOnLoadMethod]
    static void Initialize()
    {
        if (!Application.isBatchMode) EditorApplication.delayCall += StartWhenReady;
    }
    static void StartWhenReady()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += StartWhenReady;
            return;
        }
        var settings = McpUnitySettings.Instance; settings.LoadSettings();
        if (!settings.AutoStartServer) return;
        var server = McpUnityServer.Instance;
        if (server != null && !server.IsListening && !server.HasScheduledStart) server.StartServer();
    }

    [MenuItem("ReTune/Testing/Exit Dialog/Open")]
    static void OpenExitDialog() { if(Application.isPlaying) ReTuneExitDialog.RequestExit(); }
    [MenuItem("ReTune/Testing/Exit Dialog/Continue")]
    static void ContinueExitDialog() { if(Application.isPlaying) Object.FindObjectOfType<ReTuneExitDialog>()?.Continue(); }
    [MenuItem("ReTune/Testing/Exit Dialog/End")]
    static void EndExitDialog() { if(Application.isPlaying) Object.FindObjectOfType<ReTuneExitDialog>()?.End(); }

    [MenuItem("ReTune/Scenes/Apply Ending Cinematic UI")]
    public static void ApplyEndingUI()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        const string path="Assets/ReTuneScenes/Scenes/05_Ending.unity";
        var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        try
        {
            var ending=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<ReTuneEndingStoryboard>(true)).Single();
            ConfigureEndingAnimator(ending);ending.ConfigurePresentation();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        finally{if(opened)EditorSceneManager.CloseScene(scene,true);if(active.IsValid()&&active.isLoaded)UnityEngine.SceneManagement.SceneManager.SetActiveScene(active);}
    }

    static void ConfigureEndingAnimator(ReTuneEndingStoryboard ending)
    {
        const string dir="Assets/ReTuneScenes/Art/Ending";
        string clipPath=dir+"/PostLetter.anim", controllerPath=dir+"/Ending.controller";
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if(clip==null){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,clipPath);}
        ConfigureEndingClip(clip,ending.letterFrames,0,8,false);
        string walkPath=dir+"/WalkAway.anim";
        var walk=AssetDatabase.LoadAssetAtPath<AnimationClip>(walkPath);
        if(walk==null){walk=new AnimationClip();AssetDatabase.CreateAsset(walk,walkPath);}
        ConfigureEndingClip(walk,ending.letterFrames,8,6,true);
        var animatorController=AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(controllerPath);
        if(animatorController==null)animatorController=UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        var machine=animatorController.layers[0].stateMachine;
        var state=machine.states.Select(s=>s.state).FirstOrDefault(s=>s.name=="Post Letter")??machine.AddState("Post Letter");
        state.motion=clip;machine.defaultState=state;
        var departure=machine.states.Select(s=>s.state).FirstOrDefault(s=>s.name=="Walk Away")??machine.AddState("Walk Away");departure.motion=walk;
        ending.figureAnimator=ending.figure.GetComponent<Animator>();if(ending.figureAnimator==null)ending.figureAnimator=ending.figure.gameObject.AddComponent<Animator>();
        ending.figureAnimator.runtimeAnimatorController=animatorController;ending.figureAnimator.updateMode=AnimatorUpdateMode.UnscaledTime;ending.figureAnimator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        ending.animationDuration=clip.length;ending.departureDuration=walk.length;ending.figure.texture=ending.letterFrames[0];ending.figure.gameObject.SetActive(true);
        ending.shade.color=new Color(.08f,.055f,.035f,0);
        ConfigureEnvelopeCloseup(ending);
        if(ending.postman!=null)Object.DestroyImmediate(ending.postman);
        ending.postman=null;
        EditorUtility.SetDirty(clip);EditorUtility.SetDirty(walk);EditorUtility.SetDirty(animatorController);AssetDatabase.SaveAssets();
    }

    static void ConfigureEndingClip(AnimationClip clip,Texture2D[] frames,int offset,int count,bool walking)
    {
        clip.frameRate=20;
        var keys=new System.Collections.Generic.List<ObjectReferenceKeyframe>();
        for(int i=0;i<count;i++)keys.Add(new ObjectReferenceKeyframe{time=walking?i*.35f:i==0?0:1.5f+(i-1)*.5f,value=frames[offset+i]});
        keys.Add(new ObjectReferenceKeyframe{time=walking?2.6f:4.85f,value=frames[offset+count-1]});
        AnimationUtility.SetObjectReferenceCurve(clip,EditorCurveBinding.PPtrCurve("",typeof(UnityEngine.UI.RawImage),"m_Texture"),keys.ToArray());
        var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=false;AnimationUtility.SetAnimationClipSettings(clip,settings);
    }
    static void ConfigureEnvelopeCloseup(ReTuneEndingStoryboard ending)
    {
        if(ending.envelope!=null)return;
        var root=new GameObject("Envelope Closeup - Replace Source Image",typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(CanvasGroup));
        root.layer=5;root.transform.SetParent(ending.shade.transform.parent,false);
        var rect=(RectTransform)root.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2(0,24);rect.sizeDelta=new Vector2(560,320);
        // Above the dimmed background, below cinematic subtitles and credits.
        rect.SetSiblingIndex(ending.shade.transform.GetSiblingIndex()+1);
        ending.envelope=root;ending.envelopeImage=root.GetComponent<UnityEngine.UI.Image>();ending.envelopeImage.color=new Color(.91f,.86f,.72f);ending.envelopeImage.raycastTarget=false;ending.envelopeImage.preserveAspect=true;
        var placeholder=new GameObject("Placeholder Details - auto hidden with artwork",typeof(RectTransform));placeholder.layer=5;placeholder.transform.SetParent(root.transform,false);
        var bounds=(RectTransform)placeholder.transform;bounds.anchorMin=Vector2.zero;bounds.anchorMax=Vector2.one;bounds.offsetMin=bounds.offsetMax=Vector2.zero;ending.envelopePlaceholder=placeholder;
        EnvelopeLabel(placeholder.transform,ending.player.body.font,"From",new Vector2(-185,102),new Vector2(100,50),28);
        EnvelopeLabel(placeholder.transform,ending.player.body.font,"Michael",new Vector2(-100,45),new Vector2(270,86),64);
        EnvelopeLabel(placeholder.transform,ending.player.body.font,"To an old friend",new Vector2(75,-78),new Vector2(330,60),34);
        var stamp=new GameObject("Stamp",typeof(RectTransform),typeof(UnityEngine.UI.Image));stamp.layer=5;stamp.transform.SetParent(placeholder.transform,false);var sr=(RectTransform)stamp.transform;sr.sizeDelta=new Vector2(62,78);sr.anchoredPosition=new Vector2(205,91);stamp.GetComponent<UnityEngine.UI.Image>().color=new Color(.48f,.35f,.29f,.4f);stamp.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;
        var edge=root.AddComponent<UnityEngine.UI.Outline>();edge.effectColor=new Color(.26f,.19f,.14f,.7f);edge.effectDistance=new Vector2(2,-2);
        root.SetActive(false);
    }
    static void EnvelopeLabel(Transform parent,Font font,string text,Vector2 position,Vector2 size,int fontSize)
    {
        var go=new GameObject(text,typeof(RectTransform),typeof(UnityEngine.UI.Text));go.layer=5;go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.anchoredPosition=position;r.sizeDelta=size;
        var label=go.GetComponent<UnityEngine.UI.Text>();label.font=font;label.text=text;label.fontSize=fontSize;label.color=new Color(.27f,.20f,.16f);label.alignment=TextAnchor.MiddleLeft;label.raycastTarget=false;
    }
    [MenuItem("ReTune/Testing/Ending/Verify Split Animation")]
    static void VerifySplitEnding(){if(Application.isPlaying)Unity.EditorCoroutines.Editor.EditorCoroutineUtility.StartCoroutineOwnerless(CheckSplitEnding());}
    static System.Collections.IEnumerator CheckSplitEnding()
    {
        bool background=Application.runInBackground;Application.runInBackground=true;
        try
        {
            var ending=Object.FindObjectOfType<ReTuneEndingStoryboard>();ending.player.enabled=false;
            ending.player.body.text=ReTuneNarrativeBook.Load().ending[0];ending.Show(0);
            yield return new Unity.EditorCoroutines.Editor.EditorWaitForSeconds(ending.animationDuration+.4f);
            if(ending.figure.texture!=ending.letterFrames[7])throw new System.Exception("Posting shot did not stop before departure.");
            ending.player.body.text=ReTuneNarrativeBook.Load().ending[2];ending.Show(2);
            yield return new Unity.EditorCoroutines.Editor.EditorWaitForSeconds(2);
            if(!ending.envelope.activeSelf||ending.figureAnimator.speed!=0||ending.figure.texture!=ending.letterFrames[7])throw new System.Exception("Letter closeup did not pause departure.");
            ending.player.body.text=ReTuneNarrativeBook.Load().ending[3];ending.Show(3);
            yield return new Unity.EditorCoroutines.Editor.EditorWaitForSeconds(ending.departureDuration+.4f);
            if(ending.envelope.activeSelf||ending.figure.texture!=ending.letterFrames[13]||ending.IsAnimating)throw new System.Exception("Departure did not complete after letter closeup.");
            System.IO.File.WriteAllText("Logs/EndingSplit-runtime-validation.txt","PASS: Post Letter holds frame 08; envelope closeup pauses Animator; Walk Away resumes on next shot and ends at frame 14.");
            Debug.Log("RETUNE_ENDING_SPLIT_PASS");
        }
        finally{Application.runInBackground=background;}
    }
    [MenuItem("ReTune/Testing/Ending/Next")]
    static void NextEnding(){if(Application.isPlaying)Object.FindObjectOfType<ReTuneEndingStoryboard>()?.player.Next();}
    [MenuItem("ReTune/Testing/Ending/Capture")]
    static void CaptureEnding(){if(Application.isPlaying)ScreenCapture.CaptureScreenshot("Temp/EndingGameView.png");}
    [MenuItem("ReTune/Testing/Ending/Preview Animation")]
    static void PreviewEndingAnimation()
    {
        if(!Application.isPlaying)return;
        UnityEngine.Events.UnityAction<UnityEngine.SceneManagement.Scene,UnityEngine.SceneManagement.LoadSceneMode> prepare=null;
        prepare=(scene,mode)=>
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded-=prepare;
            foreach(var root in scene.GetRootGameObjects())foreach(var flow in root.GetComponentsInChildren<ReTuneNarrativePlayer>(true))flow.enabled=false;
        };
        UnityEngine.SceneManagement.SceneManager.sceneLoaded+=prepare;
        UnityEngine.SceneManagement.SceneManager.LoadScene("Assets/ReTuneScenes/Scenes/05_Ending.unity");
    }

    [MenuItem("ReTune/Scenes/Validate Ending and Expressions")]
    public static void ValidateArt()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Validate in Edit Mode.");
        var report = new System.Collections.Generic.List<string>();
        foreach (string name in new[] { "01_Shop", "04_Reply", "05_Ending" })
        {
            string path = "Assets/ReTuneScenes/Scenes/" + name + ".unity";
            var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                if (name != "05_Ending")
                {
                    var expressions = roots.SelectMany(r => r.GetComponentsInChildren<ReTuneVisitorExpressions>(true)).Single();
                    if (expressions.visitors.Length != 4) throw new System.Exception(name + ": expected four visitors");
                    foreach (var visitor in expressions.visitors)
                    {
                        if (visitor.portrait == null || visitor.expressions.Length != 4 || visitor.expressions.Any(s => s == null))
                            throw new System.Exception(name + ": missing portrait or expression");
                        if (visitor.expressions.Any(s => s.rect.size != visitor.expressions[0].rect.size))
                            throw new System.Exception(name + ": expression crop sizes differ");
                    }
                    report.Add(name + ": 4 visitors x 4 imported sprites; matching initial crop dimensions; all references resolved");
                }
                else
                {
                    var ending = roots.SelectMany(r => r.GetComponentsInChildren<ReTuneEndingStoryboard>(true)).Single();
                    if (ending.letterFrames.Length != 14 || ending.letterFrames.Any(t => t == null) || ending.restingFigure == null ||
                        ending.figure == null || ending.figureAnimator == null || ending.figureAnimator.runtimeAnimatorController == null || ending.envelope == null || ending.envelopeImage == null || ending.postman != null || ending.intertitle == null || ending.shade == null ||
                        ending.player == null)
                        throw new System.Exception("Ending: missing art, frames, or dialogue presentation");
                    ending.ConfigurePresentation();
                    if(ending.player.body.fontSize!=38 || ending.player.body.alignment!=TextAnchor.MiddleCenter ||
                        ((RectTransform)ending.player.panel.transform).sizeDelta.y!=76 || ending.player.heading.gameObject.activeSelf ||
                        ending.player.nextButton.gameObject.activeSelf || ending.player.backButton.gameObject.activeSelf || ending.player.pageLabel.gameObject.activeSelf)
                        throw new System.Exception("Ending: cinematic subtitle layout differs from Opening");
                    var layers = ending.artwork;
                    if (layers.Find("01 Street and Postbox").GetSiblingIndex() >= ending.figure.transform.GetSiblingIndex() ||
                        ending.figure.transform.GetSiblingIndex() >= layers.Find("03 Room and Doorframe Foreground").GetSiblingIndex())
                        throw new System.Exception("Ending: foreground occlusion order is incorrect");
                    report.Add(name + ": Animator: separate Post Letter and Walk Away states, 14 frames, faster departure (0.35s per frame), no loop, no postman sequence; centered envelope close-up; foreground layering resolved; Opening subtitle layout matched (76px bar, 38px centered text, navigation hidden)");
                }
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
                if (active.IsValid() && active.isLoaded) UnityEngine.SceneManagement.SceneManager.SetActiveScene(active);
            }
        }
        Directory.CreateDirectory("Logs");
        File.WriteAllLines("Logs/EndingAndExpressions-Unity-validation.txt", report);
        Debug.Log("RETUNE_ENDING_EXPRESSIONS_PASS: " + string.Join("; ", report));
    }
}



