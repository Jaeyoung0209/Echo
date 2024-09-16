using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverScript : MonoBehaviour
{

    public TMP_Text placementtext;
    public TMP_Text amountText;
    public Button acceptbutton;

    public GameObject darken;

    private AudioSource audiosource;
    [SerializeField] AudioClip buttonClickAudio;

    void Start()
    {
        audiosource = GetComponent<AudioSource>();
        this.gameObject.SetActive(false);
        darken.gameObject.SetActive(false);
        acceptbutton.gameObject.SetActive(false);
        if (NetworkManager.Instance.placement != -1)
        {
            gameObject.SetActive(true);
            darken.gameObject.SetActive(true);
            acceptbutton.gameObject.SetActive(true);
            placementtext.text = NetworkManager.Instance.placement.ToString();
            

            amountText.text = "x " + ((4 - NetworkManager.Instance.placement) * 3).ToString();
            if (NetworkManager.Instance.placement == 1)
            {
                placementtext.text = "1st Place";
            }
            else if (NetworkManager.Instance.placement == 2)
            {
                placementtext.text = "2nd Place";
            }
            else if (NetworkManager.Instance.placement == 3)
            {
                placementtext.text = "3rd Place";
            }
            else
            {
                placementtext.text = "4th Place";
            }

            NetworkManager.Instance.placement = -1;
        }
    }


    public void OnAcceptButton()
    {
        audiosource.PlayOneShot(buttonClickAudio);
        darken.gameObject.SetActive(false);
        GameObject.Find("WardrobeScript").GetComponent<WardrobeMenuScript>().UpdateMoney();
        acceptbutton.gameObject.SetActive(false);
        gameObject.SetActive(false);
        
    }
 
}
