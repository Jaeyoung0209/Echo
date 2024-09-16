using System;
using System.Collections;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using UnityEngine.UI;

public class LobbyScript : MonoBehaviour, Subscriber
{

    public TMP_Text[] usernameTexts = new TMP_Text[4];
    public Image[] cords = new Image[4];
    public TMP_Text token;
    public Image darken;
    private int stages = 3;

    void Awake()
    {
        NetworkManager.Instance.Subscribe(this);
        darken.color = new Color32(0, 0, 0, 0);
        darken.gameObject.SetActive(false);
    }

    void Start()
    {
        token.text = "token: " + NetworkManager.Instance.token;
        SetSkinandGun();
        for (int i = 0; i < cords.Length; i++)
        {
            cords[i].gameObject.SetActive(false);
        }
    }

    private void SetSkinandGun()
    {
        StartCoroutine(SetSkinandGunCoroutine());
    }

    IEnumerator SetSkinandGunCoroutine()
    {
        WWWForm form = new WWWForm();
        form.AddField("user_id", UserInfoManager.Instance.user_id);
        form.AddField("gunName", NetworkManager.Instance.gun);
        form.AddField("skinName", NetworkManager.Instance.skin);

        using (UnityWebRequest www = UnityWebRequest.Post(NetworkManager.Instance.serverurl + "/setskinandgun", form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log("Registration failed: " + www.error);
            }

            else
            {
                Dictionary<string, string> retrievedJson = JsonConvert.DeserializeObject<Dictionary<string, string>>(www.downloadHandler.text);

                Debug.Log(retrievedJson["status"]);
            }
            

        }

        NetworkManager.Instance.InitializeSocket();
    }



    public void StartButton()
    {
        if (NetworkManager.Instance.hostID == "host" && NetworkManager.Instance.users.Count >= 2)
        {
            int stagenum = UnityEngine.Random.Range(0, stages);
            NetworkManager.Instance.SendSocketMessage("start_game: " + stagenum);
            NetworkManager.Instance.stage = stagenum.ToString();
            NetworkManager.Instance.StartGame();
            StartGame();
        }
    }

    public void LeaveLobby()
    {
        StartCoroutine(LeaveLobbyCoroutine());
    }


    IEnumerator LeaveLobbyCoroutine()
    {
        WWWForm form = new WWWForm();
        form.AddField("user_id", UserInfoManager.Instance.user_id.ToString());
        form.AddField("lobby_id", NetworkManager.Instance.lobby_id);


        using (UnityWebRequest www = UnityWebRequest.Post(NetworkManager.Instance.serverurl + "/leavelobby", form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log("Leaving failed: " + www.error);
            }
            else
            {
                NetworkManager.Instance.ResetLobby();
                SceneManager.LoadScene("LobbyMenuScene");
            }

        }
    }

    void OnDestroy()
    {
        NetworkManager.Instance.UnSubscribe(this);
    }
    private void StartGame()
    {
        NetworkManager.Instance.UnSubscribe(this);
        StartCoroutine(DarkenCoroutine());
        
    }


    IEnumerator DarkenCoroutine()
    {
        darken.gameObject.SetActive(true);
        byte alpha = 0;
        for (int i = 0; i < 15; i++)
        {
            alpha += 17;
            darken.color = new Color32(0, 0, 0, alpha);
            yield return new WaitForSeconds(0.05f);

        }
        yield return StartCoroutine(NetworkManager.Instance.InitializeUDPCoroutine());
        UserInfoManager.Instance.MuteBGM(true);
        SceneManager.LoadScene("InGameScene");
    }

    private void UpdateLobbyUI()
    {
        NetworkManager.Instance.RemoveDuplicateUser();
        List<string> users = NetworkManager.Instance.usernames;
        for (int i = 0; i < users.Count; i++)
        {

            if (users[i] != null)
            {
                usernameTexts[i].text = users[i].ToString();
                cords[i].gameObject.SetActive(true);
            }

        }

        for (int i = users.Count; i < usernameTexts.Length; i++)
        {
            usernameTexts[i].text = "";
            cords[i].gameObject.SetActive(false);
        }
    }


    public void InvokeUpdate(Dictionary<string, string> retreivedmessage)
    {

        if (retreivedmessage.ContainsKey("users"))
        {
            NetworkManager.Instance.setUsers(new List<string>(
            retreivedmessage["users"].TrimStart('[')
                     .TrimEnd(']')
                     .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                     .Select(s => s.Trim())));
            NetworkManager.Instance.setUsernames(new List<string>(
            retreivedmessage["usernames"].TrimStart('[')
                    .TrimEnd(']')
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim().Trim('\''))));
           NetworkManager.Instance.setGuns(new List<string>(
           retreivedmessage["guns"].TrimStart('[')
                    .TrimEnd(']')
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim().Trim('\''))));
           NetworkManager.Instance.setSkins(new List<string>(
           retreivedmessage["skins"].TrimStart('[')
                    .TrimEnd(']')
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim().Trim('\''))));

        }
        else if (retreivedmessage.ContainsKey("message"))
        {
            if (retreivedmessage["message"] == "joined_lobby")
            {

                NetworkManager.Instance.addUser(retreivedmessage["user"]);
                NetworkManager.Instance.addUsername(retreivedmessage["username"]);
                NetworkManager.Instance.addGun(retreivedmessage["gunName"]);
                NetworkManager.Instance.addSkin(retreivedmessage["skinName"]);
            }
            else if (retreivedmessage["message"] == "disconnected")
            {
                Debug.Log("disconnected");
                NetworkManager.Instance.removeUser(retreivedmessage["user"]);
                StartCoroutine(NetworkManager.Instance.CheckHostChangeCoroutine());
            }
            else if (retreivedmessage["message"].StartsWith("start_game"))
            {
                NetworkManager.Instance.stage = retreivedmessage["message"].Substring(12);
                StartGame();
            }
        }

        UpdateLobbyUI();
    }

    public void InvokeUpdateClientFrame(HostToClientData htcData)
    {
    }

    public void InvokeUpdateHostFrame(ClientToHostData cthData)
    {
    }
}


