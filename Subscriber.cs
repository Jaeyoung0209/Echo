using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface Subscriber
{
    void InvokeUpdate(Dictionary<string, string> data);

    void InvokeUpdateClientFrame(HostToClientData htcData);
    void InvokeUpdateHostFrame(ClientToHostData cthData);
}
