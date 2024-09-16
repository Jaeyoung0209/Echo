using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyMenuScript : MonoBehaviour
{
    public TMP_InputField lobbyNameField;
    public TMP_InputField tokenField;

    private Dictionary<string, string> lobbyDictionary;
    private List<GameObject> lobbies;
    public GameObject lobbyPrefab;
    [SerializeField] private GameObject lobbyButtons;
    [SerializeField] private AudioClip buttonClickAudio;
    private AudioSource audiosource;

    private void Start()
    {
        audiosource = GetComponent<AudioSource>();
        FetchLobbyNames();
        lobbies = new List<GameObject>();
        lobbyDictionary = new Dictionary<string, string>();
    }

    public void BackButton()
    {
        audiosource.PlayOneShot(buttonClickAudio);
        SceneManager.LoadScene("WardrobeMenuScene");
    }
    public void CreateLobby()
    {
        audiosource.PlayOneShot(buttonClickAudio);
        StartCoroutine(CreateLobbyCoroutine());
    }

    public void JoinLobby()
    {
        audiosource.PlayOneShot(buttonClickAudio);
        StartCoroutine(JoinLobbyCoroutine(tokenField.text));
    }

    public void FetchLobbyNames()
    {
        StartCoroutine(FetchLobbyNamesCoroutine());
    }

    private IEnumerator CreateLobbyCoroutine()
    {
        WWWForm form = new WWWForm();
        //form.AddField("user_id", UserInfoManager.Instance.user_id);
        form.AddField("user_id", UserInfoManager.Instance.user_id);
        form.AddField("lobbyName", lobbyNameField.text);



        using (UnityWebRequest www = UnityWebRequest.Post(NetworkManager.Instance.serverurl + "/createlobby", form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log("Registration failed: " + www.error);
            }
            else
            {
                Dictionary<string, string> retrievedJson = JsonConvert.DeserializeObject<Dictionary<string, string>>(www.downloadHandler.text);

                string status = retrievedJson["status"];

                if (status == "success")
                {
                    Debug.Log(retrievedJson["token"]);
                    NetworkManager.Instance.hostIdSetter("host");
                    NetworkManager.Instance.tokenSetter(retrievedJson["token"]);
                    NetworkManager.Instance.lobbyIDSetter(retrievedJson["lobby_id"]);
                    SceneManager.LoadScene("LobbyScene");
                }
                else
                {
                    Debug.Log(status);
                }

            }

        }

    }

    private IEnumerator JoinLobbyCoroutine(string token)
    {
        Debug.Log(token);
        WWWForm form = new WWWForm();
        //form.AddField("user_id", UserInfoManager.Instance.user_id);
        form.AddField("user_id", UserInfoManager.Instance.user_id);
        form.AddField("token", token);

        using (UnityWebRequest www = UnityWebRequest.Post(NetworkManager.Instance.serverurl + "/joinlobby", form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log("Registration failed: " + www.error);
            }
            else
            {
                Dictionary<string, string> retrievedJson = JsonConvert.DeserializeObject<Dictionary<string, string>>(www.downloadHandler.text);

                string status = retrievedJson["status"];
                if (status == "success")
                {
                    Debug.Log(retrievedJson["host_id"]);
                    NetworkManager.Instance.hostIdSetter(retrievedJson["host_id"]);
                    NetworkManager.Instance.tokenSetter(token);
                    NetworkManager.Instance.lobbyIDSetter(retrievedJson["lobby_id"]);
                    SceneManager.LoadScene("LobbyScene");
                }
                else
                {
                    Debug.Log(status);
                }
            }
        }
    }

    private IEnumerator FetchLobbyNamesCoroutine()
    {
        using (UnityWebRequest www = UnityWebRequest.Get(NetworkManager.Instance.serverurl + "/fetchlobbynames"))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log("Registration failed: " + www.error);
            }
            else
            {
                Dictionary<string, string> retrievedJson = JsonConvert.DeserializeObject<Dictionary<string, string>>(www.downloadHandler.text);

                Dictionary<string,string> parsedJson = ParseStringToDictionary(retrievedJson["lobby_names"]);
                Debug.Log(www.downloadHandler.text);
                lobbyDictionary = parsedJson;
                RenderLobbyMenu();
            }
        }
    }

    private void RenderLobbyMenu()
    {
        GameObject canvas = GameObject.Find("Canvas");
        for (int i = 0; i < lobbies.Count; i++)
        {
            Destroy(lobbies[i]);
        }
        lobbies.Clear();

        Vector3 nextPosition = new Vector3(90, 50, 0);

        foreach (string lobbyName in lobbyDictionary.Keys)
        {
            GameObject newLobby = Instantiate(lobbyPrefab, Vector3.zero, Quaternion.identity);
            newLobby.GetComponent<Button>().onClick.AddListener(() => {
                
                StartCoroutine(JoinLobbyCoroutine(lobbyDictionary[lobbyName])); });
            newLobby.transform.GetChild(0).GetComponent<TMP_Text>().text = lobbyName;
            newLobby.transform.SetParent(lobbyButtons.transform);
            newLobby.GetComponent<RectTransform>().anchoredPosition = nextPosition;
            lobbies.Add(newLobby);
            nextPosition += new Vector3(0, -100, 0);
        }
    }

    public void RefreshMenu()
    {
        audiosource.PlayOneShot(buttonClickAudio);
        lobbyButtons.transform.localPosition = Vector2.zero;
        FetchLobbyNames();
    }

    void OnGUI()
    {
        Vector3 pos = lobbyButtons.transform.position;
        float scrollDelta = Input.mouseScrollDelta.y * 3;
        pos.y += scrollDelta;
       
        if (!(lobbyButtons.transform.localPosition.y < 0 && scrollDelta < 0) && !(lobbyButtons.transform.localPosition.y > 25 * lobbies.Count && scrollDelta > 0))
        {
            lobbyButtons.transform.position = pos;
        }
    }

    Dictionary<string, string> ParseStringToDictionary(string input)
    {
        input = input.Replace("\"", "'");
        input = input.Trim('[', ']');

        string[] entries = input.Split(new string[] { "), (" }, StringSplitOptions.None);


        Dictionary<string, string> result = new Dictionary<string, string>();

        foreach (string entry in entries)
        {

            string trimmedEntry = entry.Trim('(', ')');


            int separatorIndex = trimmedEntry.IndexOf("', '");

            if (separatorIndex != -1)
            {
                string key = trimmedEntry.Substring(0, separatorIndex).Trim('\'');
                string value = trimmedEntry.Substring(separatorIndex + 3).Trim('\'');
                result.Add(key, value);
            }
        }

        return result;
    }
}
