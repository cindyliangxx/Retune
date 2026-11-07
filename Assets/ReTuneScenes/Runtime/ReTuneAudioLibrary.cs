using UnityEngine;
namespace ReTune.Scenes
{
    public sealed class ReTuneAudioLibrary : ScriptableObject
    {
        public AudioClip[] postman, guitarist, bookshopOwner;
        public AudioClip menuMusic, dialogueMusic, endingMusic;
        public static ReTuneAudioLibrary Load()=>Resources.Load<ReTuneAudioLibrary>("ReTuneAudioLibrary");
        public AudioClip[] Stems(int customer)=>customer==0?postman:customer==1?guitarist:customer==2?bookshopOwner:null;
    }
}
