using System.Collections;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using static DataFetcher;

public class DataFetcher : MonoBehaviour
{
    public string url = "https://ar-teacher-backend.vercel.app/search"; // Server API endpoint to send and recieve request
    public Text displayText; // UI Text element
    public InputField inputField;
    public TTSManager ttsManager; // Text to speech manager

    [System.Serializable]
    public class ResponseData // Class to accept Json response from the server
    {
        public string response;        
    }

    public void OnSendRequest()
    {
        string userInput = inputField.text; // Get the text from the input field
        StartCoroutine(FetchDataFromServer(userInput));
    }

    IEnumerator FetchDataFromServer(string userInput)
    {
        // Append user input as a query parameter
        string requestUrl = url + "?prompt=" + UnityWebRequest.EscapeURL(userInput);
        Debug.Log(requestUrl);

        UnityWebRequest request = UnityWebRequest.Get(requestUrl);

        // Send the request and wait for the response
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            //displayText.text = "Error: " + request.error;
            ttsManager.Speak("Error: " + request.error);
        }
        else
        {
            // Parse and display the response data
            string json = request.downloadHandler.text;
            ResponseData res = JsonUtility.FromJson<ResponseData>(json);

            //displayText.text = "Response: " + res.response;

            // Use Android TTS to speak the message
            ttsManager.Speak(res.response);
        }
    }
}

