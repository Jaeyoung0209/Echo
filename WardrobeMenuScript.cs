using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class WardrobeMenuScript : MonoBehaviour
{

    private List<string> ownedItems;
    private GameObject canvas;

    [SerializeField] private GameObject itemButtonPrefab;

    private List<GameObject> itemButtons;

    private string lastusedskin;
    private string lastusedgun;

    private List<string> skinNames;
    private List<string> gunNames;

    [SerializeField] private Button skinButton;
    [SerializeField] private Button gunButton;
    [SerializeField] private Button buyButton;
    [SerializeField] private Button startButton;
    [SerializeField] private GameObject showcaseObject;
    [SerializeField] private GameObject showcaseLock;
    [SerializeField] private GameObject gunshowcaseObject;
    [SerializeField] private TMP_Text money;
    [SerializeField] private TMP_Text price_text;
    [SerializeField] private AudioClip buttonClickAudio;

    private AudioSource audiosource;

    private Vector2 originalGunScale;

    [SerializeField] private Image darken;

    [SerializeField] private GameObject IAPShopPannel;


    IEnumerator TransitionCoroutine()
    {
        darken.color = new Color32(0, 0, 0, 255);
        byte alpha = 255;
        for (int i = 0; i < 15; i++)
        {
            alpha -= 17;
            darken.color = new Color32(0, 0, 0, alpha);
            yield return new WaitForSeconds(0.05f);
        }
        darken.gameObject.SetActive(false);
        IAPShopPannel.SetActive(false);
    }

    public void OpenIAP()
    {
        audiosource.PlayOneShot(buttonClickAudio);
        IAPShopPannel.SetActive(true);
    }
    void Start()
    {
        audiosource = GetComponent<AudioSource>();
        Debug.Log("sentpackets: " + NetworkManager.Instance.sentPackets.ToString());
        StartCoroutine(TransitionCoroutine());
        skinNames = new List<string>();
        skinNames.Add("DefaultSkin1");
        skinNames.Add("DefaultSkin2");
        skinNames.Add("DefaultSkin3");
        skinNames.Add("DefaultSkin4");
        gunNames = new List<string>();
        gunNames.Add("DefaultGun1");
        gunNames.Add("DefaultGun2");

        canvas = GameObject.Find("Canvas");
        ownedItems = new List<string>();
        itemButtons = new List<GameObject>();
        GetOwnedItems();
        skinButton.gameObject.GetComponent<Image>().color = new Color32(166, 255, 153, 255);
        buyButton.gameObject.SetActive(false);
        skinButton.onClick.AddListener(() => { CheckLock();
            skinButton.gameObject.GetComponent<Image>().color = new Color32(166, 255, 153, 255);
            gunButton.gameObject.GetComponent<Image>().color = Color.white;
            RenderItemsUI(false);
            audiosource.PlayOneShot(buttonClickAudio);
        });
        gunButton.onClick.AddListener(() => { CheckLock();
            skinButton.gameObject.GetComponent<Image>().color = Color.white;
            gunButton.gameObject.GetComponent<Image>().color = new Color32(166, 255, 153, 255);
            RenderItemsUI(true);
            audiosource.PlayOneShot(buttonClickAudio);
        });
        money.text = UserInfoManager.Instance.money.ToString();
        startButton.onClick.AddListener(() => {
            audiosource.PlayOneShot(buttonClickAudio); 
            SceneManager.LoadScene("LobbyMenuScene"); });
    }

    private void GetOwnedItems()
    {
        StartCoroutine(GetOwnedItemsCoroutine());
    }

    IEnumerator GetOwnedItemsCoroutine()
    {
        WWWForm form = new WWWForm();
        form.AddField("user_id", UserInfoManager.Instance.user_id);

        
        using (UnityWebRequest www = UnityWebRequest.Post(NetworkManager.Instance.serverurl + "/requestitem", form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log("Registration failed: " + www.error);
            }
            else
            {
                Dictionary<string, string> retrievedJson = JsonConvert.DeserializeObject<Dictionary<string, string>>(www.downloadHandler.text);

                string items = retrievedJson["items"];


                ownedItems = ParseStringToList(items);
                
                lastusedgun = retrievedJson["used_gun"];
                lastusedskin = retrievedJson["used_skin"];

                if (ownedItems.Contains(lastusedgun))
                    NetworkManager.Instance.setGun(lastusedgun);
                if (ownedItems.Contains(lastusedskin))
                    NetworkManager.Instance.setSkin(lastusedskin);

                RenderItemsUI(false);
               
                

                Debug.Log(lastusedskin);
                Debug.Log(lastusedgun);
                string relativePath = "Items/Guns/" + lastusedgun + "/gunData";

                GunData gundata = Resources.Load<GunData>(relativePath);

                CheckLock();
                CheckBuyButton(lastusedskin, false);
                ShowcaseGun(gundata);
               
            }
        }
    }

    private void RenderItemsUI(bool isGun)
    {
        for (int i = 0; i < itemButtons.Count; i++)
        {
            Destroy(itemButtons[i]);
        }

        itemButtons.Clear();


        Vector2 skinbtnPosition = new Vector2(-56, 113);
        int index = 1;
        string relativePath;
        if (!isGun)
        {


            foreach (string itemName in skinNames)
            {
                relativePath = "Items/Skins/" + itemName + "/skinData";

                SkinData skindata = Resources.Load<SkinData>(relativePath);

                GameObject itembtn = Instantiate(itemButtonPrefab, Vector2.zero, Quaternion.identity);
                itembtn.transform.SetParent(canvas.transform.GetChild(0).gameObject.transform.GetChild(0).gameObject.transform);
                itembtn.transform.localPosition = skinbtnPosition;
                itembtn.transform.GetChild(0).GetComponent<Image>().sprite = skindata.skinIcon;


                if (lastusedskin != itemName)
                    itembtn.transform.GetChild(2).gameObject.SetActive(false);
                else
                {
                    CheckBuyButton(itemName, isGun);
                    AnimationShowcase(skindata.controller, skindata.showcaseScale);
                    gunshowcaseObject.transform.GetChild(0).gameObject.GetComponent<Image>().sprite = skindata.handSprite;
                    gunshowcaseObject.transform.GetChild(1).gameObject.GetComponent<Image>().sprite = skindata.handSprite;
                    gunshowcaseObject.transform.localPosition = skindata.showcaseGunOffset + 216 * Vector2.down;
                    gunshowcaseObject.transform.localScale = originalGunScale * skindata.showcaseGunScale;
                }

                if (index % 3 == 0)
                    skinbtnPosition += new Vector2(-68 * 2, -68);
                else
                    skinbtnPosition += new Vector2(68, 0);

                if (!ownedItems.Contains(itemName))
                {
                    itembtn.transform.GetChild(0).GetComponent<Image>().color = Color.gray;
                    itembtn.GetComponent<Image>().color = Color.gray;
                    itembtn.GetComponent<Button>().onClick.AddListener(() =>
                    {
                        audiosource.PlayOneShot(buttonClickAudio);
                        AnimationShowcase(skindata.controller, skindata.showcaseScale);
                        gunshowcaseObject.transform.GetChild(0).gameObject.GetComponent<Image>().sprite = skindata.handSprite;
                        gunshowcaseObject.transform.GetChild(1).gameObject.GetComponent<Image>().sprite = skindata.handSprite;
                        gunshowcaseObject.transform.localPosition = skindata.showcaseGunOffset + 216 * Vector2.down;
                        gunshowcaseObject.transform.localScale = originalGunScale * skindata.showcaseGunScale;
                        DontHaveItem(itemName, isGun, itembtn);
                        lastusedskin = itemName;
                        CheckLock();
                        CheckBuyButton(itemName, isGun);
                        
                    });
                }
                else
                {
                    itembtn.transform.GetChild(1).gameObject.SetActive(false);
                    itembtn.GetComponent<Button>().onClick.AddListener(() =>
                    {
                        audiosource.PlayOneShot(buttonClickAudio);
                        AnimationShowcase(skindata.controller, skindata.showcaseScale);
                        gunshowcaseObject.transform.GetChild(0).gameObject.GetComponent<Image>().sprite = skindata.handSprite;
                        gunshowcaseObject.transform.GetChild(1).gameObject.GetComponent<Image>().sprite = skindata.handSprite;
                        gunshowcaseObject.transform.localPosition = skindata.showcaseGunOffset + 216 * Vector2.down;
                        gunshowcaseObject.transform.localScale = originalGunScale * skindata.showcaseGunScale;
                        HaveItem(itemName, isGun, itembtn);
                        lastusedskin = itemName;
                        CheckLock();
                        CheckBuyButton(itemName, isGun);
                    });
                }
                itemButtons.Add(itembtn);
                index++;
            }
        }


        else
        {
            foreach(string itemName in gunNames) { 
                relativePath = "Items/Guns/" + itemName + "/gunData";

                GunData gundata = Resources.Load<GunData>(relativePath);

                GameObject itembtn = Instantiate(itemButtonPrefab, Vector2.zero, Quaternion.identity);
                itembtn.transform.SetParent(canvas.transform.GetChild(0).gameObject.transform.GetChild(0).gameObject.transform);
                itembtn.transform.localPosition = skinbtnPosition;
                itembtn.transform.GetChild(0).GetComponent<Image>().sprite = gundata.showcaseSprite;
                itembtn.transform.GetChild(0).transform.localScale = gundata.wardrobeButtonScale;



                //if (NetworkManager.Instance.skin != itemName)
                if (lastusedgun != itemName)
                    itembtn.transform.GetChild(2).gameObject.SetActive(false);
                else
                {
                    CheckBuyButton(itemName, isGun);
                }


                if (index % 3 == 0)
                    skinbtnPosition += new Vector2(-68 * 2, -68);
                else
                    skinbtnPosition += new Vector2(68, 0);

                if (!ownedItems.Contains(itemName))
                {
                    itembtn.transform.GetChild(0).GetComponent<Image>().color = Color.gray;
                    itembtn.GetComponent<Image>().color = Color.gray;
                    itembtn.GetComponent<Button>().onClick.AddListener(() =>
                    {
                        audiosource.PlayOneShot(buttonClickAudio);
                        ShowcaseGun(gundata);
                        DontHaveItem(itemName, isGun, itembtn);
                        lastusedgun = itemName;
                        CheckLock();
                        CheckBuyButton(itemName, isGun);
                    });

                }
                else
                {
                    itembtn.transform.GetChild(1).gameObject.SetActive(false);
                    itembtn.GetComponent<Button>().onClick.AddListener(() =>
                    {
                        audiosource.PlayOneShot(buttonClickAudio);
                        ShowcaseGun(gundata);
                        HaveItem(itemName, isGun, itembtn);
                        lastusedgun = itemName;
                        CheckLock();
                        CheckBuyButton(itemName, isGun);
                    });

                }

                itemButtons.Add(itembtn);
                index++;
            }

           
           
            
        }

        

        
    }

    private void ShowcaseGun(GunData gundata)
    {
        string pathtoskin = "Items/Skins/" + lastusedskin + "/skinData";
        SkinData skindata = Resources.Load<SkinData>(pathtoskin);
        gunshowcaseObject.GetComponent<Image>().sprite = gundata.showcaseSprite;
        gunshowcaseObject.transform.localScale = gundata.showcaseScale;
        originalGunScale = gundata.showcaseScale;
        gunshowcaseObject.transform.GetChild(0).gameObject.GetComponent<Image>().sprite = skindata.handSprite;
        gunshowcaseObject.transform.GetChild(1).gameObject.GetComponent<Image>().sprite = skindata.handSprite;
        gunshowcaseObject.transform.localPosition = skindata.showcaseGunOffset + 216 * Vector2.down;
        gunshowcaseObject.transform.localScale *= skindata.showcaseGunScale;
    }

    private void CheckBuyButton(string itemName, bool isGun)
    {
        if (!ownedItems.Contains(itemName))
        {
            buyButton.gameObject.SetActive(true);
            int price = 0;
            if (isGun)
            {
                price = Resources.Load<GunData>("Items/Guns/" + itemName + "/gunData").price;
            }
            else
            {
                price = Resources.Load<SkinData>("Items/Skins/" + itemName + "/skinData").price;
            }
            price_text.text = price.ToString();
            if (price > UserInfoManager.Instance.money)
            {
                buyButton.enabled = false;
            }
            else
            {
                buyButton.enabled = true;
                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(() => {
                    audiosource.PlayOneShot(buttonClickAudio); 
                    BuyItem(itemName, isGun); });
            }
        }
        else
        {
            buyButton.gameObject.SetActive(false);
        }
    }

    private void CheckLock()
    {
        if (!ownedItems.Contains(lastusedskin) || !ownedItems.Contains(lastusedgun))
        {
            showcaseLock.SetActive(true);
            startButton.enabled = false;
            
        }
        else
        {
            showcaseLock.SetActive(false);
            startButton.enabled = true;
        }
    }

    private void AnimationShowcase(RuntimeAnimatorController controller, Vector2 scaling)
    {
        
        showcaseObject.GetComponent<Animator>().runtimeAnimatorController = controller;
        showcaseObject.transform.localScale = scaling;
    }

    private void DontHaveItem(string itemName, bool isGun, GameObject button)
    {
        for (int i = 0; i < itemButtons.Count; i++)
        {
            itemButtons[i].transform.GetChild(2).gameObject.SetActive(false);
        }
        button.transform.GetChild(2).gameObject.SetActive(true);
        
    }

    private void HaveItem(string itemName, bool isGun, GameObject button)
    {
        
        for (int i = 0; i < itemButtons.Count; i++)
        {
            itemButtons[i].transform.GetChild(2).gameObject.SetActive(false);
        }
        button.transform.GetChild(2).gameObject.SetActive(true);
        if (isGun)
        {

            NetworkManager.Instance.setGun(itemName);
        }
        else
        {

            NetworkManager.Instance.setSkin(itemName);
        }
    }


    private void BuyItem(string itemName, bool isGun)
    {
        StartCoroutine(BuyItemCoroutine(itemName, isGun));
    }
    IEnumerator BuyItemCoroutine(string itemName, bool isGun)
    {
        WWWForm form = new WWWForm();
        form.AddField("user_id", UserInfoManager.Instance.user_id);
        form.AddField("item_name", itemName);


        using (UnityWebRequest www = UnityWebRequest.Post(NetworkManager.Instance.serverurl + "/purchaseitem", form))
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
                    Debug.Log("purchaed " + itemName);
                    RenderItemsUI(isGun);
                    money.text = retrievedJson["change"];
                    UserInfoManager.Instance.money = int.Parse(money.text);
                    if (isGun)
                        NetworkManager.Instance.setGun(itemName);
                    else
                        NetworkManager.Instance.setSkin(itemName);

                    GetOwnedItems();
                }
                else
                {
                    Debug.Log(status);
                    Debug.Log("failed purchasing " + itemName);
                }
            }
        }


    }

    List<string> ParseStringToList(string input)
    {
        input = input.Replace("\"", "");
        input = input.Replace("\'", "");

        input = input.Trim('[', ']');

        string[] splitdata = input.Split(",");
        for (int i = 0; i < splitdata.Length; i++)
        {
            splitdata[i] = splitdata[i].Trim('\'');
            splitdata[i] = splitdata[i].Replace(" ", "");
        }

        return splitdata.ToList<string>();
    }

    public void UpdateMoney()
    {
        StartCoroutine(UpdateMoneyCoroutine());
    }

    IEnumerator UpdateMoneyCoroutine()
    {
        WWWForm form = new WWWForm();
        form.AddField("user_id", UserInfoManager.Instance.user_id);


        using (UnityWebRequest www = UnityWebRequest.Post(NetworkManager.Instance.serverurl + "/getmoney", form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log("Registration failed: " + www.error);
            }
            else
            {
                Dictionary<string, string> retrievedJson = JsonConvert.DeserializeObject<Dictionary<string, string>>(www.downloadHandler.text);

                string amount = retrievedJson["amount"];
                UserInfoManager.Instance.money = int.Parse(amount);
                money.text = amount;
            }
        }
    }

}

    
