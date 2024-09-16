using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : class, new()
{
    static Singleton<T> instance;

    public static T Instance
    {
        get
        {
            return instance as T;
        }
    }

    protected virtual void Awake()
    {
        instance = this;

    }
}
