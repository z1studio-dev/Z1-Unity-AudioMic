using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using UnityEngine.Android;
public struct AudioLoundnessData{
    public bool isOn;
    public float loudness;
    public float pitch;

    public AudioLoundnessData(bool isOn, float loudness, float pitch){
        this.isOn = isOn;
        this.loudness = loudness;
        this.pitch = pitch;
    }
}

public class AudioLoudnessDetection : MonoBehaviour
{
    public int sampleWindow = 64;
    private AudioClip microphoneClip;

    public AudioSource audioSource;
    [Tooltip("Threshold (0–100) at/above which we consider 'on'.")]
    [Range(0f, 100f)]
    public float threshold = 30f;
    private bool previousIsOn = false;
    
    public float  loudness { get; private set; }
    
    public UnityEvent<AudioLoundnessData> audioDetected;
    void Start()
    {
        StartCoroutine(RequestMicrophonePermission());
        //StartMicCapture();
    }

    private IEnumerator RequestMicrophonePermission()
    {
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
            yield return new WaitUntil(() => Permission.HasUserAuthorizedPermission(Permission.Microphone));
        }
        StartMicCapture();
    }

    public static AudioLoudnessDetection instance;

    void Awake(){
        instance = this;
    }

    public void Update()
    {        
        loudness = GetLoudnessFromMicrophone();
        bool currentIsOn = loudness >= threshold;

        // If state changed, fire event with the new bool + loudness
        if (currentIsOn != previousIsOn)
        {
            audioDetected?.Invoke(new AudioLoundnessData(currentIsOn, loudness, 0f));
            //Debug.Log($"Audio Detected: {currentIsOn}, Loudness: {loudness}");
        }
    }

    private void StartMicCapture(){
        string microphoneName = Microphone.devices[0];
        microphoneClip = Microphone.Start(microphoneName, true, 1, 44100);
        audioSource.clip = microphoneClip;
        audioSource.loop = true;
        audioSource.Play();
    }
    private float GetLoudnessFromMicrophone(){
        return GetLoudnessFromAudioClip(Microphone.GetPosition(Microphone.devices[0]), microphoneClip);
    }

    private float GetLoudnessFromAudioClip(int clipPosition, AudioClip clip){
        int startPosition = clipPosition - sampleWindow;
        float[] waveData = new float[sampleWindow];
        if(startPosition < 0)
            return 0;

        clip.GetData(waveData, startPosition);

        if (startPosition <0)
            return 0;

        //compute loudness
        float totalLoudness = 0;

        for (int i = 0; i< sampleWindow; i++)
        {
            totalLoudness += Mathf.Abs(waveData[i]);
        }

        float average = totalLoudness / sampleWindow;
        
        return average * 1000f;
    }
}
