using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class IAPScript : MonoBehaviour
{

    private string[] IAPitems;
    private Button[] ShopButtons;
    [SerializeField] private GameObject IAPBackground;
    [SerializeField] private Button BackButton;
    [SerializeField] private AudioClip buttonClickAudio;
    private AudioSource audioSource;
    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        IAPitems = new string[3];
        IAPitems[0] = "10g";
        IAPitems[1] = "70g";
        IAPitems[2] = "150g";

        ShopButtons = new Button[3];
        ShopButtons[0] = IAPBackground.transform.GetChild(0).GetComponent<Button>();
        ShopButtons[1] = IAPBackground.transform.GetChild(1).GetComponent<Button>();
        ShopButtons[2] = IAPBackground.transform.GetChild(2).GetComponent<Button>();

        for (int i = 0; i <  ShopButtons.Length; i++)
        {
            string itemName = IAPitems[i];
            ShopButtons[i].onClick.AddListener(() => { BuyIAPItem(itemName); });
        }
        BackButton.onClick.AddListener(() => {
            audioSource.PlayOneShot(buttonClickAudio);
            GameObject.Find("WardrobeScript").GetComponent<WardrobeMenuScript>().UpdateMoney();
            IAPBackground.transform.parent.gameObject.SetActive(false); });
    }
    public void BuyIAPItem(string itemName)
    {
        StartCoroutine(BuyIAPItemCoroutine(itemName));
    }

    IEnumerator BuyIAPItemCoroutine(string itemName)
    {
        Debug.Log(itemName);
        WWWForm form = new WWWForm();
        form.AddField("user_id", UserInfoManager.Instance.user_id);
        form.AddField("item_name", itemName);

        using (UnityWebRequest www = UnityWebRequest.Post(NetworkManager.Instance.serverurl + "/create-payment", form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log("Registration failed: " + www.error);
            }
            else
            {
                Dictionary<string, string> retrievedJson = JsonConvert.DeserializeObject<Dictionary<string, string>>(www.downloadHandler.text);

                string paymentURL = retrievedJson["approval_url"];

                Application.OpenURL(paymentURL);
            }
        }
    }
}
