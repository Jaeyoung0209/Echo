using UnityEngine;
using System.Collections;

public class ApplicationQuitHandler : MonoBehaviour
{
    public bool coroutineCompleted = false;

    private void Awake()
    {
        Application.wantsToQuit += WantsToQuit;
    }

    private void OnDestroy()
    {
        Application.wantsToQuit -= WantsToQuit;
    }

    private bool WantsToQuit()
    {
        if (!coroutineCompleted)
        {
            StartCoroutine(NetworkManager.Instance.QuitCoroutine());
            return false;
        }
        return true; 
    }


}
