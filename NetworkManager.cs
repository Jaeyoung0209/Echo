using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using NativeWebSocket;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System;
using System.Linq;
using Google.Protobuf;
using System.IO;
using Google.Protobuf.Collections;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;


public class NetworkManager : Singleton<NetworkManager>
{
    public string serverurl { get; private set; }
    public string relayurl { get; private set; }
    public string publicIP { get; private set; }

    public string defaultskin;


    public string hostID { get; private set; } 
    public string token { get; private set; }
    public List<string> users { get; private set; } = new List<string>();
    public List<string> usernames { get; private set; } = new List<string>();
    public List<string> guns { get; private set; } = new List<string>();
    public List<string> skins { get; private set; } = new List<string>();


    public WebSocket websocket { get; private set; }

    public string lobby_id { get; private set; }


    public string gun { get; private set; }
    public string skin { get; private set; }

    public HostToClientData htc;
    public ClientToHostData cth;

    public bool sendframe;
    public bool ishost;

    public int placement;
    public string stage;

    private UdpClient udpClient;
    private IPEndPoint serverEndPoint;
    private int send_sequenceNumber = 0;
    private int receive_sequenceNumber = 0;
    private Thread udpThread;
    private bool isRunning = false;

    private bool executeinfo;
    private byte[] received_data;
    private bool udp_connected;

    public int receivedPackets;
    public int sentPackets;


    private List<Subscriber> subscribers = new List<Subscriber>();
    void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 200;
        receivedPackets = 0;
        sentPackets = 0;

        udp_connected = false;
        executeinfo = false;
        placement = -1;
        htc = SetHostToClient(new HostToClientData());
        cth = SetClientToHost(new ClientToHostData());
        sendframe = false;
        DontDestroyOnLoad(this);
        serverurl = "http://64.23.156.219:5000";
        //serverurl = "localhost:5000";
        relayurl = "ws://64.23.156.219:8989";
        //relayurl = "ws://localhost:8989";

        if (skin == null)
        {
            skin = defaultskin;
        }
        if (gun == null)
        {
            gun = "DefaultGun1";
        }
    }

    public void setGun(string gunName)
    {
        gun = gunName;
    }
    public void setSkin(string skinName)
    {
        skin = skinName;
        Debug.Log(skin);
    }

    public void addGun(string gunName)
    {
        guns.Add(gunName);
    }

    public void addSkin(string skinName)
    {
        skins.Add(skinName);
    }
    public void addUser(string user)
    {
        users.Add(user);
    }

    public void addUsername(string user)
    {
        usernames.Add(user);
    }

    public void removeUser(string user)
    {
        if (users.Contains(user))
        {
            int index = users.IndexOf(user);
            usernames.RemoveAt(index);
            guns.RemoveAt(index);
            skins.RemoveAt(index);
            users.Remove(user);
            
        }
        
    }

    public void hostIdSetter(string id) {
        hostID = id;
        Debug.Log(id);
    }
    public void tokenSetter(string t)
    {
        token = t;
    }
    public void lobbyIDSetter(string id)
    {
        Debug.Log("lobby_id: " + id);
        lobby_id = id;
    }

    public void webSocketSetter(WebSocket ws)
    {
        websocket = ws;
    }

    public void setGuns(List<string> guns)
    {
        this.guns = guns;
        for (int i = 0; i < guns.Count; i++)
        {
            Debug.Log(guns[i]);
        }
    }

    public void setSkins(List<string> skins)
    {
        this.skins = skins;
        for (int i = 0; i < skins.Count; i++)
        {
            Debug.Log(skins[i]);
        }
    }

    public void setUsers(List<string> us)
    {
        users = us;
    }

    public void setUsernames(List<string> us)
    {
        usernames = us;
    }

    public void Subscribe(Subscriber s)
    {
        subscribers.Add(s);
    }

    public void UnSubscribe(Subscriber s)
    {
        subscribers.Remove(s);
    }

    async public void ResetLobby()
    {
        hostID = "";
        lobby_id = "";
        token = "";
        users.Clear();
        usernames.Clear();
        guns.Clear();
        skins.Clear();
        send_sequenceNumber = 0;
        receive_sequenceNumber = 0;
        if (udpClient != null)
        {
            udpClient.Close();
            udpThread.Join();
            udpClient = null;
        }
       
        if (websocket != null)
        {
            await websocket.Close();
            websocket = null;
        }
    }

    public void RemoveDuplicateUser()
    {
        var seen = new HashSet<string>();
        for (int i = 0; i < users.Count; i++)
        {
            string combinedKey = $"{users[i]}|{usernames[i]}|{skins[i]}|{guns[i]}";

            if (seen.Contains(combinedKey))
            {
                users.RemoveAt(i);
                usernames.RemoveAt(i);
                skins.RemoveAt(i);
                guns.RemoveAt(i);
                i--; // Adjust index due to removal
            }
            else
            {
                seen.Add(combinedKey);
            }
        }
    }

    public IEnumerator InitializeUDPCoroutine()
    {
        while (udp_connected == false)
        {
            if (udpClient != null)
                udpClient.Close();
            udpClient = new UdpClient(9898); 
            serverEndPoint = new IPEndPoint(IPAddress.Parse("64.23.156.219"), 8989);

            string jsonMessage = JsonUtility.ToJson(new PlayerInfo(UserInfoManager.Instance.user_id.ToString(), UserInfoManager.Instance.username, lobby_id));
            byte[] data = Encoding.UTF8.GetBytes(jsonMessage);

            udpClient.Send(data, data.Length, serverEndPoint);
            StartReceiving();
            yield return new WaitForSeconds(0.1f);
        }
    }


    void StartReceiving()
    {
        isRunning = true;
        udpThread = new Thread(new ThreadStart(ReceiveData));
        udpThread.IsBackground = true;
        udpThread.Start();

        //isRunning = true;
        //await ReceiveData();
    }

    //async Task ReceiveData()
    //{
    //    IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

    //    while (isRunning)
    //    {
    //        try
    //        {
    //            var receivedResult = await udpClient.ReceiveAsync();
    //            byte[] receivedBytes = receivedResult.Buffer;

    //            try
    //            {
    //                string receivedMessage = Encoding.UTF8.GetString(receivedBytes);
    //                if (receivedMessage == "connected")
    //                    udp_connected = true;
    //                else
    //                {
    //                    executeinfo = true;
    //                    received_data = receivedBytes;
    //                }
    //            }
    //            catch (Exception e)
    //            {
    //                executeinfo = true;
    //                received_data = receivedBytes;
    //            }

    //            //Debug.Log("Received udp async");

    //            IPEndPoint senderEndpoint = receivedResult.RemoteEndPoint;
    //        }
    //        catch (Exception e)
    //        {
    //            Debug.LogError("Error receiving UDP data: " + e.Message);
    //        }
    //    }
    //}

    void ReceiveData()
    {
        try
        {
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            while (isRunning)
            {
                byte[] bytes = udpClient.Receive(ref remoteEndPoint);
                try
                {
                    string receivedMessage = Encoding.UTF8.GetString(bytes);
                    if (receivedMessage == "connected")
                        udp_connected = true;
                    else
                    {
                        executeinfo = true;
                        received_data = bytes;
                    }
                }
                catch (Exception e)
                {
                    executeinfo = true;
                    received_data = bytes;
                }

            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error receiving UDP data: " + e.Message);
        }
    }


    async public void InitializeSocket()
    {
        string playerId = UserInfoManager.Instance.user_id.ToString();



        websocket = new WebSocket(relayurl);

        

        websocket.OnOpen += async () =>
        {
            Debug.Log("Connection opened.");

            string jsonMessage = JsonUtility.ToJson(new PlayerInfo(playerId, UserInfoManager.Instance.username, lobby_id));
            Debug.Log(jsonMessage);
            await websocket.SendText(jsonMessage);
            Debug.Log("Player ID sent to server.");
        };

        websocket.OnMessage += (bytes) =>
        {

            string message = Encoding.UTF8.GetString(bytes);
            Dictionary<string, string> retreivedmessage = JsonConvert.DeserializeObject<Dictionary<string, string>>(message);

            for (int i = 0; i < subscribers.Count; i++)
            {
                subscribers[i].InvokeUpdate(retreivedmessage);
            }
           
        };

        websocket.OnError += (error) =>
        {
            Debug.LogError($"Error: {error}");
        };

        websocket.OnClose += (WebSocketCloseCode closeCode) =>
        {
            if (udpClient != null)
                udpClient.Close();

            Debug.Log($"Connection closed with code: {closeCode}");
            if (closeCode.ToString() == "ServerError")
            {
                InitializeSocket();
            }
        };

        // Connect to the server
        
        await websocket.Connect();
        
    }

    public async void SendSocketMessage(string message)
    {
        if (websocket.State == WebSocketState.Open)
        {
            byte[] serializedData = SerializeString(message);
            
            await websocket.Send(serializedData);

        }

    }

    private void FixedUpdate()
    {
        //if (websocket != null && websocket.State == WebSocketState.Open && sendframe)
        if (udpClient != null && sendframe)
        {
            sendframe = false;
            if (ishost)
            {
                byte[] serializedData;
                using (var stream = new MemoryStream())
                {
                    ConvertToProto(htc).WriteTo(stream);
                    serializedData = stream.ToArray();
                }

                udpClient.Send(serializedData, serializedData.Length, serverEndPoint);
                sentPackets++;
                if (htc.shooter != 100)
                {
                    SendSocketMessage("shooter: " + htc.shooter.ToString());
                }
                if (htc.contacts.Length > 0)
                    SendSocketMessage("contacts: " + SerializeContactsToJson(htc.contacts));
                    
                //await websocket.Send(serializedData);
                htc = new HostToClientData();
                htc = SetHostToClient(htc);
                htc.user = UserInfoManager.Instance.user_id.ToString();
                
            }
            else
            {
                byte[] serializedData;
                using (var stream = new MemoryStream())
                {
                    ConvertToProto(cth).WriteTo(stream);
                    serializedData = stream.ToArray();
                }
                udpClient.Send(serializedData, serializedData.Length, serverEndPoint);
                //await websocket.Send(serializedData);
                cth = new ClientToHostData();
                cth = SetClientToHost(new ClientToHostData());
                cth.user = UserInfoManager.Instance.user_id.ToString();
            }


        }

        if (executeinfo && received_data != null)
        {
            executeinfo = false;
            if (ishost)
            {
                ClientToHostSchema receivedData;
                using (var stream = new MemoryStream(received_data))
                {
                    receivedData = ClientToHostSchema.Parser.ParseFrom(stream);
                    ClientToHostData cthData = ConvertToClientToHostData(receivedData);

                    for (int i = 0; i < subscribers.Count; i++)
                    {
                        subscribers[i].InvokeUpdateHostFrame(cthData);
                    }
                }

            }
            else
            {
                HostToClientSchema receivedData;

                using (var stream = new MemoryStream(received_data))
                {
                    receivedData = HostToClientSchema.Parser.ParseFrom(stream);
                    HostToClientData htcData = ConvertToHostToClientData(receivedData);

                    
                    for (int i = 0; i < subscribers.Count; i++)
                    {
                        subscribers[i].InvokeUpdateClientFrame(htcData);
                    }
                    receivedPackets++;
                    
                }
            }
            received_data = null;
        }

    }




    async Task CloseSocketConnectionAsync()
    {
        if (websocket != null)
        {
            await websocket.Close();
            udpClient.Close();
            udpThread.Join();
        }
            
    }

    public IEnumerator CloseSocket()
    {
        if (websocket != null)
            yield return CloseSocketConnectionAsync().AsIEnumerator();
    }

    public IEnumerator LeaveLobbyCoroutine(bool quitgame)
    {
        WWWForm form = new WWWForm();
        form.AddField("user_id", UserInfoManager.Instance.user_id.ToString());
        form.AddField("lobby_id", lobby_id);
        using (UnityWebRequest www = UnityWebRequest.Post(serverurl + "/leavelobby", form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error leaving lobby: " + www.error);
            }
            else
            {
                Debug.Log("Successfully left lobby.");
                Dictionary<string, string> retrievedJson = JsonConvert.DeserializeObject<Dictionary<string, string>>(www.downloadHandler.text);

                placement = int.Parse(retrievedJson["placement"]);

            }

        }

        if (websocket != null)
            yield return StartCoroutine(CloseSocketConnectionAsync().AsIEnumerator());
        if (!quitgame)
        {
            StartCoroutine(GameObject.Find("InGameScript").GetComponent<InGameNetworkScript>().EndGameCoroutine());
            udp_connected = false;
            ResetLobby();
        }
        else
        {
            GetComponent<ApplicationQuitHandler>().coroutineCompleted = true;
            yield return new WaitForSeconds(1);
            Application.Quit();
        }
    }

    public IEnumerator DeadCoroutine()
    {
        if (websocket != null)
        {

            
            
            
            yield return StartCoroutine(LeaveLobbyCoroutine(false));



        }

        
    }

    public IEnumerator QuitCoroutine()
    {
        if (websocket != null)
        {


            yield return StartCoroutine(LeaveLobbyCoroutine(true));




        }
        else
        {
            GetComponent<ApplicationQuitHandler>().coroutineCompleted = true;
            yield return new WaitForSeconds(1);
            Application.Quit();
        }

    }

    private void Update()
    {
        if (websocket != null)
        {
            websocket.DispatchMessageQueue();
        }
        
    }


    public IEnumerator CheckHostChangeCoroutine()
    {
        WWWForm form = new WWWForm();
        form.AddField("lobby_id", lobby_id);
        using (UnityWebRequest www = UnityWebRequest.Post(serverurl + "/checkhostchange", form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log("Registration failed: " + www.error);
            }
            else
            {
                Dictionary<string, string> retrievedJson = JsonConvert.DeserializeObject<Dictionary<string, string>>(www.downloadHandler.text);
                if (hostID != retrievedJson["host_id"])
                {
                    hostIdSetter(retrievedJson["host_id"]);
                    SendSocketMessage("host_change");
                    if (retrievedJson["host_id"] == UserInfoManager.Instance.user_id.ToString())
                    {
                        
                        hostIdSetter("host");
                        Debug.Log("host_change");
                    }
                }
            }
        }
    }

    private HostToClientData SetHostToClient(HostToClientData htcData)
    {
        int usernumber = users.Count;
        htcData.user = UserInfoManager.Instance.user_id.ToString();
        htcData.playerPositions = new Vector2[usernumber];
        htcData.animations = new byte[usernumber];
        htcData.facing = new byte[usernumber];
        htcData.leftoverHP = new float[usernumber];
        htcData.timestamp = 0;
        htcData.shooter = 100;
        htcData.gunAngle = Vector3.zero;
        htcData.contacts = new Vector2[0];
        return htcData;
    }

    private ClientToHostData SetClientToHost(ClientToHostData cthData)
    {
        cthData.user = "";
        cthData.jump = false;
        cthData.horizontalInput = 0;
        cthData.shot = false;
        cthData.mousePosition = Vector2.zero;
        return cthData;
    }

    public void StartGame()
    {
        htc = SetHostToClient(htc);
    }

    private HostToClientSchema ConvertToProto(HostToClientData data)
    {

        HostToClientSchema proto = new HostToClientSchema
        {
            User = data.user,
            Timestamp = data.timestamp, 
            Shooter = (uint)data.shooter,  
            GunAngle = new ProtobufVector3
            {
                X = data.gunAngle.x,
                Y = data.gunAngle.y,
                Z = data.gunAngle.z
            }
        };


        foreach (Vector2 pos in data.playerPositions)
        {
            proto.PlayerPositions.Add(new ProtobufVector2
            {
                X = pos.x,
                Y = pos.y
            });
        }


        proto.Animations = ByteString.CopyFrom(data.animations);
        proto.Facing = ByteString.CopyFrom(data.facing);


        proto.LeftoverHP.AddRange(data.leftoverHP);


        foreach (Vector2 contact in data.contacts)
        {
            proto.Contacts.Add(new ProtobufVector2
            {
                X = contact.x,
                Y = contact.y
            });
        }

        return proto;
    }
    private static ClientToHostSchema ConvertToProto(ClientToHostData data)
    {

        ClientToHostSchema proto = new ClientToHostSchema
        {
            User = data.user,
            HorizontalInput = (uint)data.horizontalInput,  
            Jump = data.jump,
            Shot = data.shot,
            MousePosition = new ProtobufVector2
            {
                X = data.mousePosition.x,
                Y = data.mousePosition.y
            }
        };

        return proto;
    }
    private static HostToClientData ConvertToHostToClientData(HostToClientSchema proto)
    {
        if (proto == null)
        {
            return null;
        }

        Vector2[] ConvertVector2Array(RepeatedField<ProtobufVector2> protoArray)
        {
            return protoArray.Select(v => new Vector2(v.X, v.Y)).ToArray();
        }

        Vector3 ConvertVector3(ProtobufVector3 proto)
        {
            if (proto != null)
                return new Vector3(proto.X, proto.Y, proto.Z);
            return Vector3.zero;
        }

        return new HostToClientData
        {
            user = proto.User,
            playerPositions = ConvertVector2Array(proto.PlayerPositions),
            animations = proto.Animations.ToByteArray(),
            facing = proto.Facing.ToByteArray(),
            leftoverHP = proto.LeftoverHP.ToArray(),
            contacts = ConvertVector2Array(proto.Contacts),
            timestamp = proto.Timestamp,
            shooter = (byte)proto.Shooter,
            gunAngle = ConvertVector3(proto.GunAngle)
        };
    }
    private static ClientToHostData ConvertToClientToHostData(ClientToHostSchema proto)
    {
        if (proto == null)
        {
            return null;
        }

        Vector2 ConvertVector2(ProtobufVector2 proto)
        {
            return new Vector2(proto.X, proto.Y);
        }

        return new ClientToHostData
        {
            user = proto.User,
            horizontalInput = (byte)proto.HorizontalInput,  
            jump = proto.Jump,
            mousePosition = ConvertVector2(proto.MousePosition),
            shot = proto.Shot
        };
    }
    public static byte[] SerializeString(string input)
    {

        ByteString byteString = ByteString.CopyFromUtf8(input);

        return byteString.ToByteArray();
    }

    public static string DeserializeString(byte[] data)
    {
        ByteString byteString = ByteString.CopyFrom(data);

        return byteString.ToStringUtf8();
    }

    
    private string SerializeContactsToJson(Vector2[] contacts)
    {
        List<Vector2Data> vector2DataList = new List<Vector2Data>();
        foreach (var vec in contacts)
        {
            vector2DataList.Add(new Vector2Data(vec.x, vec.y));
        }

        return JsonConvert.SerializeObject(vector2DataList);
    }
}

public class PlayerInfo
{
    public string player_id;
    public string username;
    public string lobby_id;


    public PlayerInfo(string pid, string name, string lid)
    {
        player_id = pid;
        username = name;
        lobby_id = lid;
    }

}

public class Vector2Data
{
    public float x;
    public float y;

    public Vector2Data(float x, float y)
    {
        this.x = x;
        this.y = y;
    }
}