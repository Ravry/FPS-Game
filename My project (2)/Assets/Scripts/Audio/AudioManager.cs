using System;
using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public Sound[] sounds;
    public static AudioManager instance;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
        {
            Destroy(this.gameObject);
            return;
        }

        DontDestroyOnLoad(this.gameObject);

        foreach (Sound sound in sounds) 
        {
            sound.source = this.gameObject.AddComponent<AudioSource>();
            sound.source.clip = sound.clip;
            sound.source.volume = sound.volume;
            sound.source.pitch = sound.pitch;
            sound.source.loop = sound.loop;
        }
    }

    public void Play(string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        s?.source.Play();
    }

    public IEnumerator PlaySoundForSeconds(string name, float seconds)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        s?.source.Play();
        yield return new WaitForSeconds(seconds);
        s?.source.Stop();
    }

    public IEnumerator PlaySoundAfterSeconds(string name, float seconds)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        yield return new WaitForSeconds(seconds);
        s?.source.Play();
    }

    public void Stop(string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        s?.source.Stop();
    }
}