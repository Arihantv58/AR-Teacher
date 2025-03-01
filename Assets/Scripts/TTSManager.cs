using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class TTSManager : MonoBehaviour
{
    private static AndroidJavaObject ttsManager;
    public GameObject speakingIndicator; // Assign in the Inspector
    private bool isSpeaking = false;

    public GameObject MyObject;
    public ARRaycastManager RaycastManager;
    private CharacterAnimationController animationController;
    private bool teacher = false;

    void Start()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                ttsManager = new AndroidJavaObject("com.yourcompany.texttospeech.TextToSpeechManager", activity);
            }
        }        
    }

    public void Speak(string text)
    {
        if (ttsManager != null)
        {
            ttsManager.Call("speak", text);
            isSpeaking = true;            
        }        
    }

    public void Stop()
    {
        if (ttsManager != null)
        {
            ttsManager.Call("stop");
            isSpeaking = false;            
        }        
    }

    void Update()
    {
        // Check if TTS has finished speaking (assuming the TTSManager Java class can indicate this)
        if (isSpeaking && !ttsManager.Call<bool>("isSpeaking")) // Replace with actual check if available
        {
            isSpeaking = false;            
        }
        if (!teacher && Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            List<ARRaycastHit> touches = new List<ARRaycastHit>();

            RaycastManager.Raycast(Input.GetTouch(0).position, touches, UnityEngine.XR.ARSubsystems.TrackableType.Planes);

            if (touches.Count > 0)
            {
                GameObject teacherInstance = GameObject.Instantiate(MyObject, touches[0].pose.position, touches[0].pose.rotation * Quaternion.Euler(0, 180, 0));
                animationController = teacherInstance.GetComponent<CharacterAnimationController>();
                teacher = true;
            }
        }
        if (isSpeaking && animationController != null)
        {
            animationController.StartTalkingAnimation();
        }
        if (!isSpeaking &&  animationController != null)
        {
            animationController.StopTalkingAnimation();
        }
    }

    void OnApplicationQuit()
    {
        if (ttsManager != null)
        {
            ttsManager.Call("shutdown");
        }
    }
}
