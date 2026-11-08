using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace ReTune.Scenes
{
    [Serializable] public sealed class ReTuneNarrativeBook
    {
        [Serializable] public sealed class Visitor { public string[] dialogue, reply, departure; }
        public string[] opening, ending;
        public Visitor[] customers;
        static ReTuneNarrativeBook cached;
        public static ReTuneNarrativeBook Load()
        {
            if(cached==null){var file=Resources.Load<TextAsset>("ReTuneNarrative");if(file==null)throw new InvalidOperationException("Missing ReTuneNarrative.json");cached=JsonUtility.FromJson<ReTuneNarrativeBook>(file.text);}
            return cached;
        }
    }
    public sealed class ReTuneNarrativePlayer : MonoBehaviour
    {
        public enum Mode { Opening, Title, Shop, Reply, Ending }
        public Mode mode;
        public ReTuneSceneController controller;
        public GameObject panel;
        public Text heading, body, pageLabel, nextLabel;
        public Button nextButton, backButton;
        public GameObject openingArtwork;
        public GameObject visitorPortraits;
        readonly List<string> pages=new List<string>();
        int index, dialogueEnd;
        bool begun, busy, portraitInitialized;
        ReTuneNarrativeBook book;
        ReTuneDialoguePresentation floatingDialogue;
        ReTuneAlbumSequence album;
        int albumShownAt=-1;
        void Start(){if(controller==null)Begin(null);}
        public void Begin(ReTuneSceneController owner)
        {
            if(begun)return;begun=true;controller=owner;book=ReTuneNarrativeBook.Load();
            if(mode==Mode.Title){heading.text="Re:Tune";body.text="A small shop. A place to listen.";nextLabel.text="New Game";pageLabel.text="";backButton.gameObject.SetActive(false);return;}
            if(mode==Mode.Opening)pages.AddRange(book.opening);
            else if(mode==Mode.Ending)pages.AddRange(book.ending);
            else
            {
                if(!ReTuneSession.active)ReTuneSession.Begin(controller.previewCustomer);
                var customer=book.customers[Mathf.Clamp(ReTuneSession.customer,0,book.customers.Length-1)];
                if(mode==Mode.Shop){pages.AddRange(customer.dialogue);dialogueEnd=pages.Count;}
                else {pages.AddRange(customer.reply);pages.AddRange(customer.departure);controller.NarrativeAudio(true);}
            }
            floatingDialogue=mode==Mode.Ending?null:GetComponent<ReTuneDialoguePresentation>();album=GetComponent<ReTuneAlbumSequence>();index=mode==Mode.Ending?0:Mathf.Clamp(ReTuneProgress.ConsumePage(this),0,Mathf.Max(0,pages.Count-1));portraitInitialized=false;Show();
            if(mode==Mode.Shop&&!busy)StartCoroutine(SlideVisitor());
        }
        void Update()
        {
            if(!begun||busy||ReTuneExitDialog.IsOpen)return;
            if(album!=null&&album.BlocksDialogueInput)return;
            if(controller!=null && controller.PartsAnimationPaused)return;
            if(Input.GetKeyDown(KeyCode.Space)||Input.GetKeyDown(KeyCode.Return)||(Input.GetMouseButtonDown(0)&&!ReTuneBackgroundMusic.IsPointerOverVolumeControl()&&(mode==Mode.Opening||mode==Mode.Ending&&(UnityEngine.EventSystems.EventSystem.current==null||!UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())||floatingDialogue!=null&&floatingDialogue.CanClickContinue())))Next();
        }
        void Show()
        {
            heading.text=mode==Mode.Opening?"The old street":mode==Mode.Ending?"A letter finds its way home":"DAY "+new[]{1,4,8,12}[ReTuneSession.customer].ToString("00")+" / "+controller.story.customers[ReTuneSession.customer].name;
            body.text=pages[index];
            if(mode==Mode.Opening){var storyboard=GetComponent<ReTuneOpeningStoryboard>();if(storyboard!=null)storyboard.Show(index);}
            if(visitorPortraits!=null)
            {
                bool showPortrait=mode==Mode.Shop||(mode==Mode.Reply&&index<book.customers[ReTuneSession.customer].reply.Length);
                if(!portraitInitialized){visitorPortraits.SetActive(showPortrait);portraitInitialized=true;}
                else if(showPortrait&&!visitorPortraits.activeSelf)visitorPortraits.SetActive(true);
                else if(!showPortrait&&visitorPortraits.activeSelf&&mode==Mode.Reply)StartCoroutine(SlideVisitorOut());
            }
            pageLabel.text=(index+1)+" / "+pages.Count;
            nextLabel.text=index<pages.Count-1?"Next":mode==Mode.Shop?"Open Music Box":mode==Mode.Reply?(ReTuneSession.customer<3?"Next Visitor":"Epilogue"):mode==Mode.Ending?"Return to Title":"Continue";
            backButton.gameObject.SetActive(mode!=Mode.Opening&&index>0);
            if(mode==Mode.Opening){nextButton.gameObject.SetActive(false);pageLabel.gameObject.SetActive(false);}
            if(floatingDialogue!=null)floatingDialogue.Show(pages[index]);
            if(mode==Mode.Shop||mode==Mode.Reply){var expressions=GetComponent<ReTuneVisitorExpressions>();if(expressions!=null)expressions.Show(ReTuneSession.customer,index);}
            if(mode==Mode.Ending){var ending=GetComponent<ReTuneEndingStoryboard>();if(ending!=null)ending.Show(index);}
            ReTuneProgress.Save(this,index);
            if(openingArtwork!=null)openingArtwork.SetActive(mode!=Mode.Opening || index==0);
            if(mode==Mode.Shop && index==dialogueEnd-1)controller.NarrativeAudio(false);
        }
        public void Previous(){if(busy||index<=0||controller!=null&&controller.PartsAnimationPaused)return;index--;Show();}
        public void Next()
        {
            if(busy||controller!=null&&controller.PartsAnimationPaused)return;
            if(album!=null&&album.IsOpen)return;
            if(mode==Mode.Ending){var ending=GetComponent<ReTuneEndingStoryboard>();if(ending!=null&&ending.ConsumeNext())return;}
            if(floatingDialogue!=null&&panel.activeSelf&&floatingDialogue.ConsumeNext())return;
            if(mode==Mode.Reply&&ReTuneSession.customer==2&&album!=null&&albumShownAt!=index&&pages[index].Contains("These are some of the photos that'll be on display."))
            {albumShownAt=index;album.Open(this);return;}
            if(mode==Mode.Opening){var storyboard=GetComponent<ReTuneOpeningStoryboard>();if(storyboard!=null){if(storyboard.IsTyping){storyboard.CompleteSubtitle();storyboard.CompleteTransition();return;}if(storyboard.IsTransitioning){storyboard.CompleteTransition();return;}if(storyboard.AdvanceSubtitle())return;}}
            if(mode==Mode.Ending){var ending=GetComponent<ReTuneEndingStoryboard>();if(ending!=null&&ending.IsAnimating)return;}
            if(mode==Mode.Title){ReTuneProgress.StartNew();ReTuneSession.Reset();ReTuneSession.Begin(0);StartCoroutine(EnterShop());return;}
            if(index+1<pages.Count){index++;Show();return;}
            if(mode==Mode.Opening)Go("00_Start");
            else if(mode==Mode.Shop)Go("02_Parts");
            else if(mode==Mode.Reply){if(ReTuneSession.customer<3){StartCoroutine(AdvanceToNextVisitor());}else Go("05_Ending");}
            else {ReTuneSession.Reset();Go("00_Start");}
        }
        [Header("New Game transition (seconds)")]
        [Min(.1f)] public float menuExitDuration=.55f, backgroundBlendDuration=.35f, visitorEntranceDuration=1.1f, visitorExitDuration=1f;
        IEnumerator EnterShop()
        {
            busy=true;
            if(controller!=null)controller.StoryTransitionLocked=true;
            var source=gameObject.scene;
            var groups=new List<CanvasGroup>();
            ReTuneTitleMotion motion=null;
            foreach(var root in source.GetRootGameObjects())
            {
                foreach(var m in root.GetComponentsInChildren<ReTuneTitleMotion>(true)){motion=m;m.enabled=false;}
                foreach(var h in root.GetComponentsInChildren<ReTuneTitleHotspots>(true))h.enabled=false;
                foreach(var b in root.GetComponentsInChildren<Button>(true))b.interactable=false;
                foreach(var c in root.GetComponentsInChildren<Canvas>(true))
                {
                    if(!c.isRootCanvas)continue;
                    c.sortingOrder=1000;
                    var g=c.GetComponent<CanvasGroup>();if(g==null)g=c.gameObject.AddComponent<CanvasGroup>();
                    g.blocksRaycasts=false;groups.Add(g);
                }
            }
            var targets=new List<RectTransform>();
            var directions=new List<Vector2>();
            if(motion!=null){if(motion.logo!=null){targets.Add(motion.logo);directions.Add(Vector2.left);}if(motion.menus!=null)foreach(var m in motion.menus)if(m!=null&&!targets.Contains(m)){targets.Add(m);directions.Add(Vector2.right);}}
            else if(panel!=null){targets.Add(panel.GetComponent<RectTransform>());directions.Add(Vector2.left);}
            var starts=new List<Vector2>();foreach(var t in targets)starts.Add(t.anchoredPosition);
            var operation=SceneManager.LoadSceneAsync(ReTuneSceneController.ScenePath(ReTuneSceneController.Screen.Shop),LoadSceneMode.Additive);
            operation.allowSceneActivation=false;
            for(float t=0;t<menuExitDuration;t+=Time.unscaledDeltaTime)
            {
                float u=Mathf.SmoothStep(0,1,t/menuExitDuration);
                for(int i=0;i<targets.Count;i++)targets[i].anchoredPosition=starts[i]+directions[i]*1600*u;
                yield return null;
            }
            foreach(var target in targets)target.gameObject.SetActive(false);
            while(operation.progress<.9f)yield return null;
            ReTuneNarrativePlayer destination=null;
            UnityEngine.Events.UnityAction<Scene,LoadSceneMode> prepare=(scene,modeLoaded)=>
            {
                if(scene.path!=ReTuneSceneController.ScenePath(ReTuneSceneController.Screen.Shop))return;
                foreach(var root in scene.GetRootGameObjects())foreach(var p in root.GetComponentsInChildren<ReTuneNarrativePlayer>(true))
                {
                    if(p.mode!=Mode.Shop)continue;destination=p;p.busy=true;
                    if(p.controller!=null)p.controller.StoryTransitionLocked=true;
                    if(p.panel!=null)p.panel.SetActive(false);
                    if(p.visitorPortraits!=null)p.visitorPortraits.SetActive(false);
                }
                foreach(var root in source.GetRootGameObjects())
                {
                    foreach(var camera in root.GetComponentsInChildren<Camera>(true))camera.enabled=false;
                    foreach(var events in root.GetComponentsInChildren<UnityEngine.EventSystems.EventSystem>(true))events.enabled=false;
                }
            };
            SceneManager.sceneLoaded+=prepare;
            operation.allowSceneActivation=true;
            while(!operation.isDone)yield return null;
            SceneManager.sceneLoaded-=prepare;
            // Start initializes dialogue and portrait visibility; conceal them before the first render.
            if(destination!=null)
            {
                if(destination.panel!=null)destination.panel.SetActive(false);
                if(destination.visitorPortraits!=null)destination.visitorPortraits.SetActive(false);
            }
            for(float t=0;t<backgroundBlendDuration;t+=Time.unscaledDeltaTime)
            {
                foreach(var g in groups)g.alpha=1-Mathf.SmoothStep(0,1,t/backgroundBlendDuration);
                yield return null;
            }
            foreach(var g in groups)g.alpha=0;
            if(destination!=null)yield return destination.SlideVisitor();
            var shop=SceneManager.GetSceneByPath(ReTuneSceneController.ScenePath(ReTuneSceneController.Screen.Shop));
            SceneManager.SetActiveScene(shop);
            SceneManager.UnloadSceneAsync(source);
        }
        IEnumerator SlideVisitor()
        {
            busy=true;if(controller!=null)controller.StoryTransitionLocked=true;
            if(panel!=null)panel.SetActive(false);
            if(visitorPortraits!=null)
            {
                var portrait=visitorPortraits.transform;var end=portrait.localPosition;
                var endScale=portrait.localScale;
                var rect=portrait as RectTransform;
                var camera=controller!=null?controller.worldCamera:null;
                float distance=camera!=null?camera.orthographicSize*camera.aspect*2+4:20;
                if(rect!=null){var parent=rect.parent as RectTransform;distance=Mathf.Max(320,(parent!=null?parent.rect.width:1280)*.35f);}
                var offset=Vector3.left*distance+Vector3.up*(rect!=null?32:.25f);
                var startScale=endScale*.94f;
                portrait.localPosition=end+offset;portrait.localScale=startScale;portrait.gameObject.SetActive(true);
                for(float t=0;t<visitorEntranceDuration;t+=Time.unscaledDeltaTime)
                {
                    float u=1-Mathf.Pow(1-Mathf.Clamp01(t/visitorEntranceDuration),3);
                    portrait.localPosition=end+offset*(1-u);
                    portrait.localScale=Vector3.Lerp(startScale,endScale,u);yield return null;
                }
                portrait.localPosition=end;portrait.localScale=endScale;
            }
            if(panel!=null)panel.SetActive(true);
            busy=false;if(controller!=null)controller.StoryTransitionLocked=false;
        }
        IEnumerator SlideVisitorOut()
        {
            busy=true;if(controller!=null)controller.StoryTransitionLocked=true;
            if(visitorPortraits!=null && visitorPortraits.activeSelf)
            {
                var portrait=visitorPortraits.transform;var start=portrait.localPosition;
                var rect=portrait as RectTransform;
                var camera=controller!=null?controller.worldCamera:null;
                float distance=camera!=null?camera.orthographicSize*camera.aspect*2+4:20;
                if(rect!=null){var parent=rect.parent as RectTransform;distance=Mathf.Max(320,(parent!=null?parent.rect.width:1280)*.35f);}
                var offset=Vector3.left*distance+Vector3.up*(rect!=null?32:.25f);
                var target=start+offset;
                for(float t=0;t<visitorExitDuration;t+=Time.unscaledDeltaTime)
                {
                    float p=Mathf.Clamp01(t/visitorExitDuration);
                    float u=p<.5f?8f*p*p*p*p:1f-Mathf.Pow(-2f*p+2f,4)*.5f; // Ease.InOutQuart：先慢后快再缓停，向左平滑退场
                    portrait.localPosition=Vector3.LerpUnclamped(start,target,u);yield return null;
                }
                portrait.localPosition=target;portrait.gameObject.SetActive(false);
            }
            busy=false;if(controller!=null)controller.StoryTransitionLocked=false;
        }
        IEnumerator AdvanceToNextVisitor()
        {
            if(visitorPortraits!=null&&visitorPortraits.activeSelf)yield return SlideVisitorOut();
            ReTuneSession.Begin(ReTuneSession.customer+1);
            Go("01_Shop");
        }
        public void ReplayOpening(){if(busy)return;ReTuneSession.Reset();Go("00_Opening");}
        public void ReturnToTitle(){if(busy)return;ReTuneSession.Reset();Go("00_Start");}
        public void ResumeGame(){if(!busy&&controller!=null)controller.ResumeGame();}
        public void StepOut(){ReTuneExitDialog.RequestExit();}
        void Go(string name){busy=true;SceneManager.LoadScene("Assets/ReTuneScenes/Scenes/"+name+".unity");}
    }
}



