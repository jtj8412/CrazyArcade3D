using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;
    public AudioSource myAudio;

    public AudioClip soundBoxpush;
    public AudioClip soundBoxBreak;
    public AudioClip soundBubbleDrop;
    public AudioClip soundBubbleAttack;
    public AudioClip soundBubblePopping;
    public AudioClip soundJump;
    public AudioClip soundPunch;
    public AudioClip soundItemPick;
    public AudioClip soundRun;
    public AudioClip buttonSound;
    public AudioClip lose, win;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        myAudio = GetComponent<AudioSource>();
    }

    public void PlayButtonSound()
    {
        myAudio.PlayOneShot(buttonSound);
    }

    public void PlayLoseSound()
    {
        myAudio.clip = null;
        myAudio.PlayOneShot(lose);
    }

    public void PlayWinSound()
    {
        myAudio.clip = null;
        myAudio.PlayOneShot(win);
    }

    public void PlayBoxPushSound()
    {
        myAudio.PlayOneShot(soundBoxpush);
    }
    public void PlayBoxBreak()
    {
        myAudio.PlayOneShot(soundBoxBreak);
    }
    public void PlayBubbleDrop()
    {
        myAudio.PlayOneShot(soundBubbleDrop);
    }
    public void PlayBubbleAttack()
    {
        myAudio.PlayOneShot(soundBubbleAttack);
    }
    public void PlayBubblePopping()
    {
        myAudio.PlayOneShot(soundBubblePopping);
    }
    public void PlayJump()
    {
        myAudio.PlayOneShot(soundJump);
    }
    public void PlayPunch()
    {
        myAudio.PlayOneShot(soundPunch);
    }
    public void PlayItemPick()
    {
        myAudio.PlayOneShot(soundItemPick);
    }
    public void PlayRun()
    {
        myAudio.PlayOneShot(soundRun);
    }
}
