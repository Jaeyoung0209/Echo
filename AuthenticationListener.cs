using System;
using System.Net;
using System.Threading;
using UnityEngine;

public class AuthenticationListener : MonoBehaviour
{
    private HttpListener httpListener;
    private Thread listenerThread;
    private const string urlPrefix = "http://localhost:8080/";

    void Start()
    {
        StartServer();
    }

    void OnDestroy()
    {
        StopServer();
    }

    private void StartServer()
    {
        httpListener = new HttpListener();
        httpListener.Prefixes.Add(urlPrefix);
        httpListener.Start();

        listenerThread = new Thread(() =>
        {
            while (httpListener.IsListening)
            {
                var context = httpListener.GetContext();
                ProcessRequest(context);
            }
        });
        listenerThread.Start();
    }

    private void ProcessRequest(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        string responseString = "Received OAuth Redirect!";
        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
        response.ContentLength64 = buffer.Length;

        var output = response.OutputStream;
        output.Write(buffer, 0, buffer.Length);
        output.Close();

    
        var query = request.Url.Query;
        var code = System.Web.HttpUtility.ParseQueryString(query).Get("code");
        if (code != null)
        {
            Debug.Log("Authorization code received: " + code);
            UserInfoManager.Instance.setAuthenticationCode(code);
        }
    }

    private void StopServer()
    {
        if (httpListener != null)
        {
            httpListener.Stop();
            httpListener = null;
        }

        if (listenerThread != null)
        {
            listenerThread.Join();
            listenerThread = null;
        }
    }
}