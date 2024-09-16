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
            // Start the coroutine and indicate that we want to wait for it to complete
            StartCoroutine(NetworkManager.Instance.QuitCoroutine());
            return false; // Cancel the quit request for now
        }
        return true; // Allow the quit request
    }


}