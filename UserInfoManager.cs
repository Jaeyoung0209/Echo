using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class UserInfoManager : Singleton<UserInfoManager>
{

    private const string tokenUrl = "https://oauth2.googleapis.com/token";
    public string clientId = "id";
    private string clientSecret = "secret";
    private string redirectUri = "http://localhost:8080";
    private string authorizationCodeClass;

    public string register_username;

    public bool isloginRequest;
    public int user_id { get; private set; }
    public string username { get; private set; }

    public int money;

    private AudioSource audiosource;
    [SerializeField] private AudioClip bgm;

    private void Start()
    {

        audiosource = GetComponent<AudioSource>();
        audiosource.volume = 0.2f;
        audiosource.clip = bgm;
        audiosource.loop = true;
        audiosource.Play();
        register_username = "";
        isloginRequest = true;
        authorizationCodeClass = "";
        DontDestroyOnLoad(this);

    }

    public void MuteBGM(bool shouldmute)
    {
        audiosource.mute = shouldmute;
    }


    public void InitializeUserID(string id)
    {
        user_id = int.Parse(id);
    }

    public void InitializeUserName(string name)
    {
        username = name;
    }

    public void setAuthenticationCode(string code)
    {
        authorizationCodeClass = code;
    }


    private void FixedUpdate()
    {
        if (authorizationCodeClass != "")
        {
            string copy = authorizationCodeClass;
            authorizationCodeClass = "";
            ExchangeCodeForToken(copy);
        }
    }



    public void ExchangeCodeForToken(string authorizationCode)
    {
        StartCoroutine(PostTokenRequest(authorizationCode));
    }

    private IEnumerator PostTokenRequest(string authorizationCode)
    {
        var form = new WWWForm();
        form.AddField("code", authorizationCode);
        form.AddField("client_id", clientId);
        form.AddField("client_secret", clientSecret);
        form.AddField("redirect_uri", redirectUri);
        form.AddField("grant_type", "authorization_code");

        using (var request = UnityWebRequest.Post(tokenUrl, form))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string token = request.downloadHandler.text;
                Debug.Log("Token response: " + token);
                if (isloginRequest)
                    StartCoroutine(SendTokenToServerLogin(token));
                else
                    StartCoroutine(SendTokenToServerRegister(token));
            }
            else
            {
                Debug.LogError("Error: " + request.error);
            }
        }
    }

    private IEnumerator SendTokenToServerLogin(string accessToken)
    {
        
        var form = new WWWForm();
        form.AddField("access_token", accessToken);
        

        using (var request = UnityWebRequest.Post(NetworkManager.Instance.serverurl + "/login", form))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Dictionary<string, string> retrievedJson = JsonConvert.DeserializeObject<Dictionary<string, string>>(request.downloadHandler.text);
                string status = retrievedJson["status"];

                if (status == "success")
                {
                    
                    InitializeUserID(retrievedJson["user_id"]);
                    InitializeUserName(retrievedJson["username"]);
                    money = int.Parse(retrievedJson["money"]);
                    SceneManager.LoadScene("WardrobeMenuScene");
                }
                else
                {
                    GameObject.Find("UserAuthentication").GetComponent<UserAuthentication>().ErrorMessage("Account doesn't exist, did you mean to register?");
                }
            }
            else
            {
                GameObject.Find("UserAuthentication").GetComponent<UserAuthentication>().ErrorMessage("Error fetching google access token");
            }
        }
    }

    private IEnumerator SendTokenToServerRegister(string accessToken)
    {

        var form = new WWWForm();
        form.AddField("username", register_username);
        form.AddField("access_token", accessToken);


        using (var request = UnityWebRequest.Post(NetworkManager.Instance.serverurl + "/register", form))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Dictionary<string, string> retrievedJson = JsonConvert.DeserializeObject<Dictionary<string, string>>(request.downloadHandler.text);
                string status = retrievedJson["status"];
                if (status == "registered")
                {
                    GameObject.Find("UserAuthentication").GetComponent<UserAuthentication>().ErrorMessage("Account Created!");
                    StartCoroutine(SendTokenToServerLogin(accessToken));
                }
                else
                {
                    GameObject.Find("UserAuthentication").GetComponent<UserAuthentication>().ErrorMessage("Error Registering, Try a different username");
                }
            }
            else
            {
                GameObject.Find("UserAuthentication").GetComponent<UserAuthentication>().ErrorMessage("Error fetching google access token");
            }
        }
    }
}
