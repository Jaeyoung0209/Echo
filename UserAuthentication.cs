using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Newtonsoft.Json;
using UnityEngine.SceneManagement;


public class UserAuthentication : MonoBehaviour
{
    public TMP_InputField usernameField;
    public TMP_Text resultText;

    private string clientId;
    private string redirectUri = "http://localhost:8080";
    private string authUrl = "https://accounts.google.com/o/oauth2/v2/auth";

    private void Start()
    {
        clientId = UserInfoManager.Instance.clientId;
    }


    public void RegisterUser()
    {
        if (usernameField.text != "")
        {
            UserInfoManager.Instance.register_username = usernameField.text;
            UserInfoManager.Instance.isloginRequest = false;
            string url = $"{authUrl}?response_type=code&client_id={clientId}&redirect_uri={redirectUri}&scope=profile%20email&state=YOUR_STATE_PARAMETER&access_type=offline";
            Application.OpenURL(url);
        }
        else
        {
            resultText.text = "Username Cannot Be Blank!";
        }
    }


    public void LoginUser()
    {
        UserInfoManager.Instance.isloginRequest = true;
        string url = $"{authUrl}?response_type=code&client_id={clientId}&redirect_uri={redirectUri}&scope=profile%20email&state=YOUR_STATE_PARAMETER&access_type=offline";
        Application.OpenURL(url);
    }

    public void ErrorMessage(string message)
    {
        resultText.text = message;
    }
}
