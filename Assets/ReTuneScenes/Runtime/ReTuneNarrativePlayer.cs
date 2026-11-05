using System;
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
        bool begun, busy;
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
            floatingDialogue=GetComponent<ReTuneDialoguePresentation>();album=GetComponent<ReTuneAlbumSequence>();Show();
        }
        void Update()
        {
            if(!begun||busy)return;
            if(album!=null&&album.BlocksDialogueInput)return;
            if(controller!=null && controller.PartsAnimationPaused)return;
            if(Input.GetKeyDown(KeyCode.Space)||Input.GetKeyDown(KeyCode.Return)||(Input.GetMouseButtonDown(0)&&!ReTuneBackgroundMusic.IsPointerOverVolumeControl()&&(mode==Mode.Opening||floatingDialogue!=null&&floatingDialogue.CanClickContinue())))Next();
        }
        void Show()
        {
            heading.text=mode==Mode.Opening?"The old street":mode==Mode.Ending?"A letter finds its way home":"DAY "+new[]{1,4,8,12}[ReTuneSession.customer].ToString("00")+" / "+controller.story.customers[ReTuneSession.customer].name;
            body.text=pages[index];
            if(mode==Mode.Opening){var storyboard=GetComponent<ReTuneOpeningStoryboard>();if(storyboard!=null)storyboard.Show(index);}
            if(visitorPortraits!=null)visitorPortraits.SetActive(mode==Mode.Shop||mode==Mode.Reply&&index<book.customers[ReTuneSession.customer].reply.Length);pageLabel.text=(index+1)+" / "+pages.Count;
            nextLabel.text=index<pages.Count-1?"Next":mode==Mode.Shop?"Open Music Box":mode==Mode.Reply?(ReTuneSession.customer<3?"Next Visitor":"Epilogue"):mode==Mode.Ending?"Return to Title":"Continue";
            backButton.gameObject.SetActive(mode!=Mode.Opening&&index>0);
            if(mode==Mode.Opening){nextButton.gameObject.SetActive(false);pageLabel.gameObject.SetActive(false);}
            if(floatingDialogue!=null)floatingDialogue.Show(pages[index]);
            if(openingArtwork!=null)openingArtwork.SetActive(mode!=Mode.Opening || index==0);
            if(mode==Mode.Shop && index==dialogueEnd-1)controller.NarrativeAudio(false);
        }
        public void Previous(){if(busy||index<=0||controller!=null&&controller.PartsAnimationPaused)return;index--;Show();}
        public void Next()
        {
            if(busy||controller!=null&&controller.PartsAnimationPaused)return;
            if(album!=null&&album.IsOpen)return;
            if(floatingDialogue!=null&&floatingDialogue.ConsumeNext())return;
            if(mode==Mode.Reply&&ReTuneSession.customer==2&&album!=null&&albumShownAt!=index&&pages[index].Contains("These are some of the photos that'll be on display."))
            {albumShownAt=index;album.Open(this);return;}
            if(mode==Mode.Opening){var storyboard=GetComponent<ReTuneOpeningStoryboard>();if(storyboard!=null){if(storyboard.IsTyping){storyboard.CompleteSubtitle();storyboard.CompleteTransition();return;}if(storyboard.IsTransitioning){storyboard.CompleteTransition();return;}if(storyboard.AdvanceSubtitle())return;}}
            if(mode==Mode.Title){ReTuneSession.Reset();ReTuneSession.Begin(0);Go("01_Shop");return;}
            if(index+1<pages.Count){index++;Show();return;}
            if(mode==Mode.Opening)Go("00_Start");
            else if(mode==Mode.Shop)Go("02_Parts");
            else if(mode==Mode.Reply){if(ReTuneSession.customer<3){ReTuneSession.Begin(ReTuneSession.customer+1);Go("01_Shop");}else Go("05_Ending");}
            else {ReTuneSession.Reset();Go("00_Start");}
        }
        public void ReplayOpening(){ReTuneSession.Reset();Go("00_Opening");}
        public void ReturnToTitle(){ReTuneSession.Reset();Go("00_Start");}
        public void StepOut(){Application.Quit();}
        void Go(string name){busy=true;SceneManager.LoadScene("Assets/ReTuneScenes/Scenes/"+name+".unity");}
    }
}



