using UnityEngine;

public class MicInput : MonoBehaviour
{
    void Start()
    {
        var device = Microphone.devices[0];

        AudioSource audioSource = GetComponent<AudioSource>();
        audioSource.clip = Microphone.Start(device, true, 10, 44100);
        audioSource.Play();
    }
}
