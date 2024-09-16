using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Newtonsoft.Json;
using System.Xml.Linq;
using UnityEditor;

public class InGameNetworkScript : MonoBehaviour, Subscriber
{
    private List<GameObject> players = new List<GameObject>();
    public GameObject playerPrefab;
    private string shooterID;

    private float shootCoolDownTimer;
    private float shootCoolDown;

    private GameObject shotParticle;
    private GameObject maskedParticle;

    private bool ishost;

    public Image darken;

    public Dictionary<string, ClientToHostData> ReceivedData;


    private bool canshoot;

    public TMP_Text debugtext;

    private bool startReceiving;

    private AudioSource audiosource;
    [SerializeField] private AudioClip audioclip;

    IEnumerator StartGameCoroutine()
    {
        if (ishost || startReceiving)
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
        }
        else
        {
            yield return null;
        }
        
    }

    public IEnumerator EndGameCoroutine()
    {
        darken.color = new Color32(0, 0, 0, 0);
        byte alpha = 0;
        for (int i = 0; i < 15; i++)
        {
            alpha += 17;
            darken.color = new Color32(0, 0, 0, alpha);
            yield return new WaitForSeconds(0.05f);
        }

        UserInfoManager.Instance.MuteBGM(false);
        SceneManager.LoadScene("WardrobeMenuScene");
    }
    void Start()
    {

        audiosource = GetComponent<AudioSource>();
        audiosource.volume = 0.2f;
        audiosource.clip = audioclip;
        audiosource.loop = true;
        audiosource.Play();
        startReceiving = false;
        StageData stage = Resources.Load<StageData>("stages/" + NetworkManager.Instance.stage);
        Instantiate(stage.stage, Vector2.zero, Quaternion.identity);

        if (NetworkManager.Instance.stage == "1")
            Camera.main.backgroundColor = new Color32(25, 25, 25, 255);
        else
            Camera.main.backgroundColor = Color.black;

        canshoot = false;
        NetworkManager.Instance.RemoveDuplicateUser();
        
        StartCoroutine(StartGameCoroutine());
        shootCoolDown = 0.5f;
        shootCoolDownTimer = 0;

        NetworkManager.Instance.Subscribe(this);
        ishost = NetworkManager.Instance.hostID == "host"? true : false;
        NetworkManager.Instance.ishost = ishost;
        
        ReceivedData = new Dictionary<string, ClientToHostData>();


        for (int i = 0; i < NetworkManager.Instance.users.Count; i++)
        {
            GameObject player = Instantiate(playerPrefab, stage.initialPositions[i], Quaternion.identity);
            Debug.Log(NetworkManager.Instance.skins[i]);
            string animatorPath = "Items/Skins/" + NetworkManager.Instance.skins[i] + "/animator";
            string shooterAnimatorPath = "Items/Skins/" + NetworkManager.Instance.skins[i] + "/animator_shooter";
            RuntimeAnimatorController runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>(animatorPath);
            RuntimeAnimatorController runtimeShooterAnimatorController = Resources.Load<RuntimeAnimatorController>(shooterAnimatorPath);

            string maskPath = "Items/Skins/" + NetworkManager.Instance.skins[i] + "/mask";
            Sprite[] masksprite = Resources.LoadAll<Sprite>(maskPath);

            string skinDataPath = "Items/Skins/" + NetworkManager.Instance.skins[i] + "/skindata";
            SkinData skinData = Resources.Load<SkinData>(skinDataPath);

            

            string gunPath = "Items/Guns/" + NetworkManager.Instance.guns[i] + "/GunPrefab";
            GameObject gunPrefab = Resources.Load<GameObject>(gunPath);

            string particlepath1 = "Items/Guns/" + NetworkManager.Instance.guns[i] + "/Shot_Particle";
            GameObject shotParticlesPrefab = Resources.Load<GameObject> (particlepath1);
            shotParticle = shotParticlesPrefab;
            string particlepath2 = "Items/Guns/" + NetworkManager.Instance.guns[i] + "/Masked_Particle";
            GameObject maskedParticlesPrefab = Resources.Load<GameObject>(particlepath2);
            maskedParticle = maskedParticlesPrefab;

            string handPath = "Items/Skins/" + NetworkManager.Instance.skins[i] + "/hand";
            Sprite handSprite = Resources.Load<Sprite>(handPath);


            players.Add(player);
            PlayerController pc = players[i].GetComponent<PlayerController>();

            pc.SetAnimator(runtimeAnimatorController, runtimeShooterAnimatorController);
            pc.setGunObject(gunPrefab, handSprite, skinData.gunOffset);
            pc.setMaskSprite(masksprite[0], masksprite[1]);
            pc.setPlayerID(NetworkManager.Instance.users[i]);
            pc.setUsername(NetworkManager.Instance.usernames[i]);

            
        }
        if (ishost)
        {
            SetUpRigidbody();
            
            StartCoroutine(DelaySend());

        }


    }

    IEnumerator DelaySend()
    {
        yield return new WaitForSeconds(1);
        int randomUser = Random.Range(0, NetworkManager.Instance.users.Count);
        string newShooter = NetworkManager.Instance.users[randomUser];
        Debug.Log("here");
        SetNewShooter(newShooter);
        NetworkManager.Instance.htc.shooter = (byte)randomUser;
        NetworkManager.Instance.sendframe = true;
    }

    public void RemovePlayer(GameObject player)
    {
        Debug.Log(player.GetComponent<PlayerController>().player_id);
        players.Remove(player);
    }

    IEnumerator DelayShoot()
    {
        canshoot = false;
        yield return new WaitForSeconds(1.5f);
        canshoot = true;
    }

    public void SetNewShooter(string id)
    {
        StartCoroutine(DelayShoot());
        shooterID = id;
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] != null)
            {
                PlayerController pc = players[i].GetComponent<PlayerController>();
                if (pc.player_id == id)
                {
                    StartCoroutine(ShotCoroutine(pc.gameObject, 1));
                    
                }
                else
                {
                    pc.SetShooter(false);
                    pc.SetShooterUI(false);
                }
            }
        }
    }



    private void OnDestroy()
    {
        NetworkManager.Instance.UnSubscribe(this);
    }

    IEnumerator DelayCoroutine()
    {
        yield return new WaitForSeconds(1);
        StartCoroutine(AudioFadeOut());
        StartCoroutine(NetworkManager.Instance.DeadCoroutine());
    }


    public void InvokeUpdate(Dictionary<string, string> data)
    {

        if (data["message"] == "disconnected") 
        {
            string disconnectedUser = data["user"];
            NetworkManager.Instance.removeUser(data["user"]);

            for (int i = 0; i < players.Count; i++)
            {
                if (players[i] != null)
                {
                    PlayerController pc = players[i].GetComponent<PlayerController>();
                    if (pc.player_id == disconnectedUser)
                    {
                        Debug.Log(pc.username);
                        RemovePlayer(pc.gameObject);
                        StartCoroutine(pc.DeSpawnCoroutine());
                        
                        
                    }
                }
            }


            StartCoroutine(CheckHostCoroutine(disconnectedUser));
            if (NetworkManager.Instance.users.Count <= 1)
            {
                
                StartCoroutine(DelayCoroutine());
                
            }
        }

        else if (data["message"].StartsWith("shooter"))
        {
            string shooterid = NetworkManager.Instance.users[int.Parse(data["message"].Split(':')[1])];


            Debug.Log("shooterid: " + shooterid);
            SetNewShooter(shooterid);
            
        }

        else if (data["message"].StartsWith("contacts"))
        {
                
            string contacts = data["message"].Substring(9);
            List<Vector2Data> deserializedContacts = JsonConvert.DeserializeObject<List<Vector2Data>>(contacts);

            Vector2[] deserializedVectors = new Vector2[deserializedContacts.Count];
            for (int i = 0; i < deserializedContacts.Count; i++)
            {
                deserializedVectors[i] = new Vector2(deserializedContacts[i].x, deserializedContacts[i].y);

            }
            for (int i = 0; i < players.Count; i++)
            {
                PlayerController pc = players[i].GetComponent<PlayerController>();
                if (pc.player_id == shooterID)
                {
                    pc.RenderBullet(deserializedVectors);
                }
            }

        }

        else if (data["message"].StartsWith("dead"))
        {
            string dead = data["message"].Substring(5);
            Debug.Log("dead: " + dead);
            GameObject deadplayer = players[int.Parse(dead)];
            if (deadplayer.GetComponent<PlayerController>().player_id == UserInfoManager.Instance.user_id.ToString())
            {
                StartCoroutine(AudioFadeOut());
                StartCoroutine(NetworkManager.Instance.DeadCoroutine());
            }
            NetworkManager.Instance.removeUser(deadplayer.GetComponent<PlayerController>().player_id);
            RemovePlayer(deadplayer);
            StartCoroutine(deadplayer.GetComponent<PlayerController>().DeSpawnCoroutine());
        }

    }

    IEnumerator AudioFadeOut()
    {
        for (int i = 20; i < 0; i--)
        {
            audiosource.volume = i / 100;
            yield return new WaitForSeconds(0.1f);
        }
        audiosource.volume = 0;
    }

    private void SetUpRigidbody()
    {
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] != null)
            {
                if (players[i].GetComponent<Rigidbody2D>() == null)
                {
                    Rigidbody2D rb = players[i].AddComponent<Rigidbody2D>();
                    //players[i].AddComponent<BoxCollider2D>();
                    rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                    rb.gravityScale = 3;
                    rb.drag = 1;
                    rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                    players[i].GetComponent<PlayerController>().SetRigidBody(rb);

                }
            }
        }
    }

    IEnumerator CheckHostCoroutine(string disconnected)
    {
        yield return StartCoroutine(NetworkManager.Instance.CheckHostChangeCoroutine());
        ishost = NetworkManager.Instance.hostID == "host" ? true : false;
        NetworkManager.Instance.ishost = ishost;
        if (ishost)
        {
            SetUpRigidbody();

            if (disconnected == shooterID)
            {
                int randomIndex = Random.Range(0, NetworkManager.Instance.users.Count);
                string newShooter = NetworkManager.Instance.users[randomIndex];
                Debug.Log("here");
                SetNewShooter(newShooter);
                NetworkManager.Instance.htc.shooter = (byte)randomIndex;
                NetworkManager.Instance.sendframe = true;
            }
        }

    }


    private void FixedUpdate()
    {
        if (shootCoolDownTimer > 0)
        {
            shootCoolDownTimer -= Time.fixedDeltaTime;
        }
        if (ishost)
        {
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i] != null)
                {
                    PlayerController pc = players[i].GetComponent<PlayerController>();
                    if (NetworkManager.Instance.htc.leftoverHP.Length > i)
                        NetworkManager.Instance.htc.leftoverHP[i] = pc.DecreaseHealth(0);
                    if (pc.player_id == shooterID)
                    {
                        float leftover = pc.DecreaseHealth(0.1f);
                        if (NetworkManager.Instance.htc.leftoverHP.Length > i)
                            NetworkManager.Instance.htc.leftoverHP[i] = leftover;
                        if (leftover <= 0)
                        {
                            string pid = pc.player_id;
                            //NetworkManager.Instance.htc.death = (byte)i;
                            NetworkManager.Instance.SendSocketMessage("dead: " + i.ToString());
                            NetworkManager.Instance.removeUser(pc.player_id);
                            if (NetworkManager.Instance.users.Count >= 1 && pid != UserInfoManager.Instance.user_id.ToString())
                            {
                                int index = Random.Range(0, NetworkManager.Instance.users.Count);
                                string newShooter = NetworkManager.Instance.users[index];
                                Debug.Log("here");
                                NetworkManager.Instance.htc.shooter = (byte)index;
                                SetNewShooter(newShooter);
                                

                            }
                           

                           
                            if (pid == UserInfoManager.Instance.user_id.ToString())
                            {
                                StartCoroutine(AudioFadeOut());
                                StartCoroutine(NetworkManager.Instance.DeadCoroutine());
                            }
                            Debug.Log("0hp");
                            RemovePlayer(pc.gameObject);
                            if (pc.gameObject.GetComponent<PlayerController>() != null)
                                StartCoroutine(pc.gameObject.GetComponent<PlayerController>().DeSpawnCoroutine());
                        }
                    }

                    foreach (KeyValuePair<string, ClientToHostData> entry in ReceivedData)
                    {
                        pc.ProcessInput(ReceivedData[entry.Key]);
                    }
                    if (NetworkManager.Instance.htc.playerPositions.Length > i)
                    {
                        NetworkManager.Instance.htc.playerPositions[i] = pc.transform.position;
                        if (!pc.sendlandAnimation)
                            NetworkManager.Instance.htc.animations[i] = (byte)pc.gameObject.GetComponent<Animator>().GetInteger("State");
                        else
                        {
                            NetworkManager.Instance.htc.animations[i] = 4;
                            pc.sendlandAnimation = false;
                        }
                        if (pc.transform.rotation.y % 360 == 0)
                            NetworkManager.Instance.htc.facing[i] = 1;
                        else
                            NetworkManager.Instance.htc.facing[i] = 2;
                    }
                    
                }

                
            }
            NetworkManager.Instance.htc.timestamp = Time.time;
            NetworkManager.Instance.sendframe = true;

            ReceivedData.Clear();
            
        }

    }



    private void Update()
    {

        if (ishost)
        {
            HandleHostInput();
        }
        else
        {
            HandleClientInput();
        }
    }


    IEnumerator FreezePositionCoroutine(GameObject player, float duration)
    {

        player.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezePosition;
        yield return new WaitForSeconds(duration);
        player.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.None;
        player.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeRotation;

    }


    IEnumerator ShotCoroutine(GameObject player, float duration)
    {

        GameObject particle1 = Instantiate(shotParticle, player.transform.position, Quaternion.identity);
        float alpha = 1;

        if (ishost)
            StartCoroutine(FreezePositionCoroutine(player, duration));
        for (int i = 0; i < 10; i++)
        {
            alpha -= 0.1f;

            player.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, alpha);

            yield return new WaitForSeconds(0.1f);
        }
        GameObject particle2 = Instantiate(maskedParticle, player.transform.position, Quaternion.identity);
        player.GetComponent<PlayerController>().SetShooter(true);

        if (shooterID == UserInfoManager.Instance.user_id.ToString())
        {
            player.GetComponent<PlayerController>().SetShooterUI(true);

        }
        player.GetComponent<SpriteRenderer>().color = Color.white;
        yield return new WaitForSeconds(2);
        Destroy(particle1);
        Destroy(particle2);
        
    }

    private void HandleHostInput()
    {
        
        string id = UserInfoManager.Instance.user_id.ToString();
        bool wasinput = false;

        if (ReceivedData.ContainsKey(id))
        {
            ClientToHostData hostinput = ReceivedData[id];
            if (Input.GetKeyDown(KeyCode.W))
            {
                hostinput.jump = true;
            }
            if (Input.GetKey(KeyCode.A))
            {
                hostinput.horizontalInput = 1;
            }
            else if (Input.GetKey(KeyCode.D))
            {
                hostinput.horizontalInput = 2;
            }

            if (shooterID == id)
            {
                if (Input.GetMouseButton(0) && shootCoolDownTimer <= 0)
                {
                    if (canshoot)
                    {
                        shootCoolDownTimer = shootCoolDown;
                        hostinput.shot = true;
                    }
                }

                hostinput.mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            }
        }
        else
        {
            ClientToHostData hostinput = new ClientToHostData();
            hostinput.user = id;
            if (Input.GetKeyDown(KeyCode.W))
            {
                hostinput.jump = true;
                wasinput = true;
            }
            if (Input.GetKey(KeyCode.A))
            {
                hostinput.horizontalInput = 1;
                wasinput = true;
            }
            else if (Input.GetKey(KeyCode.D))
            {
                hostinput.horizontalInput = 2;
                wasinput = true;
            }

            if (shooterID == id)
            {
                wasinput = true;
                if (Input.GetMouseButton(0) && shootCoolDownTimer <= 0)
                {
                    if (canshoot)
                    {
                        shootCoolDownTimer = shootCoolDown;
                        hostinput.shot = true;
                    }

                }

                hostinput.mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            }

            if (wasinput)
                ReceivedData.Add(id, hostinput);
        }
        

    }

    private void HandleClientInput()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            NetworkManager.Instance.cth.jump = true;
            NetworkManager.Instance.sendframe = true;
        }
        if (Input.GetKey(KeyCode.A))
        {
            NetworkManager.Instance.cth.horizontalInput = 1;
            NetworkManager.Instance.sendframe = true;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            NetworkManager.Instance.cth.horizontalInput = 2;
            NetworkManager.Instance.sendframe = true;
        }
        
        if (shooterID == UserInfoManager.Instance.user_id.ToString())
        {
            if (Input.GetMouseButton(0) && shootCoolDownTimer <= 0)
            {
                if (canshoot) 
                { 
                    shootCoolDownTimer = shootCoolDown;
                    NetworkManager.Instance.cth.shot = true;
                    NetworkManager.Instance.sendframe = true;
                }
            }

            NetworkManager.Instance.cth.mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            NetworkManager.Instance.sendframe = true;
        }

    }

   

    public void InvokeUpdateClientFrame(HostToClientData htcData)
    {
        startReceiving = true;
        if (htcData != null) {
           
            if (htcData.contacts.Length > 0)
            {

                for (int i = 0; i < players.Count; i++)
                {
                    PlayerController pc = players[i].GetComponent<PlayerController>();
                    if (pc.player_id == shooterID)
                    {
                        pc.RenderBullet(htcData.contacts);
                    }
                }
            }


            if (htcData.playerPositions.Length == players.Count) {
                for (int i = 0; i < players.Count; i++)
                {
                    if (players[i] != null)
                    {
                        PlayerController pc = players[i].GetComponent<PlayerController>();
                        pc.StartInterpolation(htcData.playerPositions[i], htcData.timestamp);
                        //pc.gameObject.transform.position = htcData.playerPositions[i];
                        pc.SetHealth(htcData.leftoverHP[i]);
                        pc.gameObject.GetComponent<Animator>().SetInteger("State", htcData.animations[i]);
                        pc.gameObject.transform.rotation = Quaternion.Euler(0, htcData.facing[i] * 180 - 180, 0);
                        if (pc.player_id == shooterID)
                        {
                            if (htcData.gunAngle != Vector3.zero)
                            {
                                pc.RotateGun(htcData.gunAngle);
                            }
                        }
                    }


                }

                

                
            }
        }

    }

    public void InvokeUpdateHostFrame(ClientToHostData cthData)
    {
        if (!ReceivedData.ContainsKey(cthData.user))
        {
            ReceivedData.Add(cthData.user, cthData);
        }
    }
}
