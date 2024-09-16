using Google.Protobuf.WellKnownTypes;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public string player_id { get; private set; }
    public string username;
    private float speed = 6f;
    [SerializeField]
    private bool isGrounded;
    [SerializeField]
    private bool wasgrounded;
    private Transform groundCheck;
    private LayerMask groundLayer;
    private LineRenderer scopeRenderer;
    private LineRenderer bulletRenderer;
    private Animator animator;

    public bool sendlandAnimation;

    [SerializeField]
    private float health;

    private string hitPlayerID = "";

    [SerializeField]
    private float jumpForce = 16f;
    [SerializeField]
    private float coyoteTime = 0.2f;  // Duration of coyote timeseta
    private float coyoteTimeCounter;

    [SerializeField]
    private float jumpBufferTime = 0.2f;  // Duration of jump buffer
    private float jumpBufferCounter;

    private bool isShooter;
    [SerializeField]
    private bool shooterUI;

    private GameObject gun;
    private Rigidbody2D rb;
    RuntimeAnimatorController rac;
    RuntimeAnimatorController srac;

    private SpriteRenderer mask;
    private Sprite maskbackward;
    private Sprite maskforward;

    [SerializeField]
    private GameObject ingameUIPrefab;
    private GameObject ingameUIObject;
    private GameObject hpbar;
    private Transform laserStartTransform;
    private TMP_Text usernameText;

    private GameObject shooterParticles;

    private GameObject shotParitcles;
    private GameObject maskedParticles;

    private bool isControlled;


    private Material dissolveMaterial;
    private int dissolveAmount = Shader.PropertyToID("_DissolveAmount");
    private int verticalDissolve = Shader.PropertyToID("_VerticalDissolve");

    public float smoothing = 0.1f; 
    private Queue<(Vector2 position, float timestamp)> positionBuffer = new Queue<(Vector2, float)>(); 
    private Vector2 targetPosition; 
    private float elapsedTime = 0; 

    private float interpolationDuration;
    private float previousTime;
    private Vector2 currentPosition;
    private float interpolationStartTime;


    private AudioSource audiosource;
    [SerializeField] AudioClip deadClip;
    [SerializeField] AudioClip shootClip;




    public void StartInterpolation(Vector2 newTargetPosition, float time)
    {


        interpolationDuration = time - previousTime;
        previousTime = time;
        currentPosition = transform.position;
        targetPosition = newTargetPosition;

        interpolationStartTime = Time.time;
        //positionBuffer.Enqueue((newTargetPosition, time));

    }

    public void setPlayerID(string id)
    {
        player_id = id;
        if (UserInfoManager.Instance.user_id.ToString() == player_id)
        {
            ingameUIObject.transform.GetChild(2).gameObject.SetActive(true);
            isControlled = true;
        }
        else
        {
            ingameUIObject.transform.GetChild(2).gameObject.SetActive(false);
            isControlled = false;
        }
    }

    public void setMaskSprite(Sprite left, Sprite right)
    {
        maskbackward = left;
        maskforward = right;
        mask.sprite = right;
    }
    public void setUsername(string name)
    {
        username = name;
        usernameText.text = username;
    }


    public float DecreaseHealth(float amount)
    {
        health -= amount;
        UpdateHPUI();
        return health;
    }

    public void SetHealth(float health)
    {
        this.health = health;
        UpdateHPUI();
    }

    private void UpdateHPUI()
    {
        hpbar.transform.localScale = new Vector3(health / 100, hpbar.transform.localScale.y, hpbar.transform.localScale.z);
        hpbar.transform.localPosition = new Vector2(-health / 200 + 0.5f, 0);
    }

    public void RotateGun(Vector3 rotation)
    {
        gun.transform.rotation = Quaternion.Euler(rotation);
        if (gun.transform.rotation.x == 0)
        {
            if (transform.rotation.y == 0)
                mask.sprite = maskforward;
            else
                mask.sprite = maskbackward;
        }
        else
        {
            if (transform.rotation.y == 0)
                mask.sprite = maskbackward;
            else
                mask.sprite = maskforward;
        }
    }

    public void SetAnimator(RuntimeAnimatorController runtimeanimator, RuntimeAnimatorController runtimeshooteranimator)
    {
        animator.runtimeAnimatorController = runtimeanimator;
        rac = runtimeanimator;
        srac = runtimeshooteranimator;
    }

    private void Awake()
    {
        audiosource = GetComponent<AudioSource>();
        targetPosition = transform.position;
        Material materialprefab = Resources.Load<Material>("ShaderEffects/DissolveMaterial");
        dissolveMaterial = new Material(materialprefab);
        GetComponent<SpriteRenderer>().material = dissolveMaterial;
        dissolveMaterial.SetFloat(dissolveAmount, 1f);
        dissolveMaterial.SetFloat(verticalDissolve, 0);
        StartCoroutine(SpawnCoroutine());
        wasgrounded = false;
        animator = GetComponent<Animator>();
        health = 100f;
        groundCheck = transform.GetChild(0).GetComponent<Transform>();
        groundLayer = LayerMask.GetMask("Ground");
        scopeRenderer = transform.GetChild(1).GetComponent<LineRenderer>();
        scopeRenderer.positionCount = 4;
        scopeRenderer.gameObject.SetActive(false);

        bulletRenderer = transform.GetChild(2).GetComponent<LineRenderer>();
        bulletRenderer.positionCount = 4;
        bulletRenderer.material = new Material(Shader.Find("Sprites/Default"));
        bulletRenderer.material.color = Color.red;
        //gun = transform.GetChild(3).gameObject;
        mask = transform.GetChild(3).gameObject.GetComponent<SpriteRenderer>();
        ingameUIObject = Instantiate(ingameUIPrefab, transform.position, Quaternion.identity);

        hpbar = ingameUIObject.transform.GetChild(0).gameObject.transform.GetChild(0).gameObject;
        usernameText = ingameUIObject.transform.GetChild(1).gameObject.GetComponent<TMP_Text>();
        shooterParticles = transform.GetChild(4).gameObject;
        shooterParticles.SetActive(false);
        isControlled = false;
    }

    public void setGunObject(GameObject gunPrefab, Sprite handSprite, Vector2 gunOffset)
    {
        GameObject gunObject = Instantiate(gunPrefab);
        gunObject.transform.SetParent(this.transform);
        gunObject.transform.localPosition = gunOffset;
        gunObject.transform.GetChild(1).GetComponent<SpriteRenderer>().sprite = handSprite;
        gunObject.transform.GetChild(2).GetComponent<SpriteRenderer>().sprite = handSprite;
        laserStartTransform = gunObject.transform.GetChild(3).transform;
        gun = gunObject;

    }

    public void SetRigidBody(Rigidbody2D body)
    {
        rb = body;
    }

    private void Update()
    {
        ingameUIObject.transform.position = transform.position;
        if (isControlled)
            Camera.main.transform.position = transform.position + new Vector3(0, 0, -10);
        if (shooterUI)
        {
            Vector3[] contacts = CalculateBullet(Camera.main.ScreenToWorldPoint(Input.mousePosition));
            scopeRenderer.SetPositions(contacts);
        }

        if (Time.time - interpolationStartTime < interpolationDuration)
        {
            elapsedTime = Time.time - interpolationStartTime;

            float t = Mathf.Clamp01(elapsedTime / interpolationDuration);
        
            transform.position = Vector2.Lerp(currentPosition, targetPosition, t);
        }

        
    }

    private Vector3[] CalculateBullet(Vector3 initialVector)
    {
        
        int layer = (1 << 7) | (1 << 8);
        layer = ~layer;
        Vector3[] contacts = new Vector3[4];
        contacts[0] = laserStartTransform.position;

        Vector3 targetPosition = contacts[0] + (laserStartTransform.position - gun.transform.position).normalized;
        targetPosition.z = 0;

        float offsetDistance = 0.1f;
        int validContactCount = 1;

        for (int i = 0; i < 3; i++)
        {
            Vector2 rayVector = targetPosition - contacts[i];
            RaycastHit2D hit = Physics2D.Raycast(contacts[i], rayVector, Mathf.Infinity, layer);

            if (hit.collider != null)
            {
                Vector2 hitNormal = hit.normal;
                Vector2 reflectedVector = Vector2.Reflect(rayVector, hitNormal);


                Vector3 offsetPoint = hit.point + hitNormal * offsetDistance;
                targetPosition = (Vector3)reflectedVector + offsetPoint;
                contacts[i + 1] = offsetPoint;
                validContactCount++;
            }
            else
            {
                break;
            }
        }

        scopeRenderer.positionCount = validContactCount;
        Vector3[] validContacts = new Vector3[validContactCount];
        System.Array.Copy(contacts, validContacts, validContactCount);
        return validContacts;
    }
    private Vector3[] CalculateServerBullet(Vector3 initialVector)
    {
       
        int layer = 1 << 8;
        layer = ~layer;
        Vector3[] contacts = new Vector3[4];
        contacts[0] = laserStartTransform.position;

        Vector3 targetPosition = contacts[0] + (laserStartTransform.position - gun.transform.position).normalized;
        targetPosition.z = 0;

        float offsetDistance = 0.1f;
        int validContactCount = 1;

        for (int i = 0; i < 3; i++)
        {
            Vector2 rayVector = targetPosition - contacts[i];
            RaycastHit2D hit = Physics2D.Raycast(contacts[i], rayVector, Mathf.Infinity, layer);

            if (hit.collider != null)
            {
                Vector2 hitNormal = hit.normal;
                Vector2 reflectedVector = Vector2.Reflect(rayVector, hitNormal);


                Vector3 offsetPoint = hit.point + hitNormal * offsetDistance;
                targetPosition = (Vector3)reflectedVector + offsetPoint;
                contacts[i + 1] = offsetPoint;
                validContactCount++;
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Player"))
                {
                    hitPlayerID = hit.collider.gameObject.GetComponent<PlayerController>().player_id;
                    break;
                }
            }
            else
            {
                hitPlayerID = "";
                break;
            }
        }

        bulletRenderer.positionCount = validContactCount;
        Vector3[] validContacts = new Vector3[validContactCount];
        System.Array.Copy(contacts, validContacts, validContactCount);
        return validContacts;
    }

    private void FixedUpdate()
    {
        if (rb != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);


            if (rb.velocity == Vector2.zero)
                animator.SetInteger("State", 0);
            else if (rb.velocity.y < -4)
                animator.SetInteger("State", 3);



            if (wasgrounded == false && isGrounded == true)
            {
                animator.SetInteger("State", 4);
                sendlandAnimation = true;
                Debug.Log("landed");
            }
            wasgrounded = isGrounded;
            if (isGrounded)
            {
                coyoteTimeCounter = coyoteTime;
            }
            else
            {
                coyoteTimeCounter -= Time.fixedDeltaTime;
            }

            jumpBufferCounter -= Time.fixedDeltaTime;


            if (jumpBufferCounter > 0 && coyoteTimeCounter > 0)
            {

                rb.velocity = new Vector2(rb.velocity.x, 0);
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                jumpBufferCounter = 0;
                coyoteTimeCounter = 0;
                animator.SetInteger("State", 2);

            }
        }
        
    }

    public void SetShooter(bool shooter)
    {
        if (shooter)
        {
            isShooter = true;
            mask.gameObject.SetActive(true);
            shooterParticles.SetActive(true);
            animator.runtimeAnimatorController = srac;
            gameObject.layer = LayerMask.NameToLayer("Shooter");
            if (gun != null)
                gun.SetActive(true);
            usernameText.color = Color.red;

        }
        else
        {
            isShooter = false;
            mask.gameObject.SetActive(false);
            shooterParticles.SetActive(false);
            animator.runtimeAnimatorController = rac;
            gameObject.layer = LayerMask.NameToLayer("Player");
            if (gun != null)
                gun.SetActive(false);
            usernameText.color = Color.white;
        }
    }

    public void SetShooterUI(bool shooter)
    {
        if (shooter)
        {
            shooterUI = true;
            scopeRenderer.enabled = true;
            scopeRenderer.gameObject.SetActive(true);
        }
        else
        {
            shooterUI = false;
            scopeRenderer.enabled = false;
            scopeRenderer.gameObject.SetActive(false);
        }
    }


    public void ProcessInput(ClientToHostData cthData)
    {
        if (cthData.user == player_id)
        {
            if (cthData.jump)
            {
                jumpBufferCounter = jumpBufferTime;
            }
            if (cthData.horizontalInput == 1)
            {
                rb.velocity = new Vector3(-speed, rb.velocity.y, 0);
                transform.rotation = Quaternion.Euler(0, 180, 0);
                if (isGrounded && !sendlandAnimation)
                    animator.SetInteger("State", 1);

            }
            else if (cthData.horizontalInput == 2)
            {
                rb.velocity = new Vector3(speed, rb.velocity.y, 0);
                transform.rotation = Quaternion.Euler(0, 0, 0);

                if (isGrounded && !sendlandAnimation)
                    animator.SetInteger("State", 1);

            }

            Vector3 mouseVector = new Vector3(cthData.mousePosition.x, cthData.mousePosition.y, 0);
            
            if (cthData.shot == true)
            {
                Vector3[] contacts = CalculateServerBullet(mouseVector);
                

                if (rb != null)
                {
                    rb.velocity = Vector2.zero;
                    Vector2 facing = transform.position - mouseVector;

                    rb.AddForce(4 * facing.normalized, ForceMode2D.Impulse);
                }

                bulletRenderer.SetPositions(contacts);
                StartCoroutine(BulletCoroutine());

                Vector2[] flatcontacts = new Vector2[contacts.Length];
                for (int i = 0; i < contacts.Length; i++)
                {
                    flatcontacts[i] = new Vector2(contacts[i].x, contacts[i].y);
                    
                }
                NetworkManager.Instance.htc.contacts = flatcontacts;
                if (hitPlayerID != "")
                {

                    GameObject.Find("InGameScript").GetComponent<InGameNetworkScript>().SetNewShooter(hitPlayerID);
                    NetworkManager.Instance.htc.shooter = (byte)NetworkManager.Instance.users.IndexOf(hitPlayerID);
                    hitPlayerID = "";
                }
            }


            if (isShooter)
            {
                //face characeter based on point
                Vector2 direction = mouseVector - transform.position;
                direction = direction.normalized;
                float angle = Mathf.Atan2(direction.y, direction.x);

                if (transform.rotation == Quaternion.Euler(0, 0, 0))
                {
                    if (mouseVector.x > transform.position.x)
                    {
                        mask.sprite = maskforward;
                        angle = angle * 180 / Mathf.PI;
                        if (gun != null)
                        {

                            gun.transform.rotation = Quaternion.Euler(0, 0, angle);
                            NetworkManager.Instance.htc.gunAngle = new Vector3(0, 0, angle);
                        }
                    }
                    else
                    {
                        mask.sprite = maskbackward;
                        angle = angle * 180 / Mathf.PI;
                        if (gun != null)
                        {
                            gun.transform.rotation = Quaternion.Euler(180, 0, -angle);
                            NetworkManager.Instance.htc.gunAngle = new Vector3(180, 0, -angle);
                        }
                    }
                }
                else
                {
                    if (mouseVector.x < transform.position.x)
                    {
                        mask.sprite = maskforward;
                        angle = -angle * 180 / Mathf.PI;
                        if (gun != null)
                        {
                            gun.transform.rotation = Quaternion.Euler(180, 0, angle);
                            NetworkManager.Instance.htc.gunAngle = new Vector3(180, 0, angle);
                        }

                    }
                    else
                    {
                        mask.sprite = maskbackward;
                        angle = -angle * 180 / Mathf.PI;
                        if (gun != null)
                        {
                            gun.transform.rotation = Quaternion.Euler(0, 0, -angle);
                            NetworkManager.Instance.htc.gunAngle = new Vector3(0, 0, -angle);
                        }
                    }

                }
            }



            
        }
    }

    public void RenderBullet(Vector2[] contacts)
    {
        audiosource.PlayOneShot(shootClip);
        int validpositions = 0;
        List<Vector3> contactlist = new List<Vector3>();
        for (int i = 0; i < contacts.Length; i++) 
        { 
            if (contacts[i] != Vector2.zero)
            {
                contactlist.Add(new Vector3(contacts[i].x, contacts[i].y , 0));
                validpositions++;
            }
        }
        bulletRenderer.positionCount = validpositions;
        bulletRenderer.SetPositions(contactlist.ToArray());
        StartCoroutine(BulletCoroutine());
    }

    IEnumerator BulletCoroutine()
    {
        Gradient gradient = new Gradient();
        GradientColorKey[] colorKey = new GradientColorKey[2];
        colorKey[0].color = bulletRenderer.startColor;
        colorKey[0].time = 0.0f;
        colorKey[1].color = bulletRenderer.endColor;
        colorKey[1].time = 1.0f;

        GradientAlphaKey[] alphaKey = new GradientAlphaKey[2];
        alphaKey[0].time = 0.0f;
        alphaKey[1].time = 1.0f;

        float duration = 1.0f; 
        float elapsedTime = 0.0f;

        while (elapsedTime < duration)
        {
            float alpha = Mathf.Lerp(1.0f, 0.0f, elapsedTime / duration);
            alphaKey[0].alpha = alpha;
            alphaKey[1].alpha = alpha;

            gradient.SetKeys(colorKey, alphaKey);
            bulletRenderer.colorGradient = gradient;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        alphaKey[0].alpha = 0.0f;
        alphaKey[1].alpha = 0.0f;
        gradient.SetKeys(colorKey, alphaKey);
        bulletRenderer.colorGradient = gradient;
    }

    

    public static Vector3 StringToVector3(string str)
    {

        str = str.Trim('(', ')');


        string[] sArray = str.Split(',');


        try
        {
            float x = float.Parse(sArray[0]);
            float y = float.Parse(sArray[1]);
            float z = float.Parse(sArray[2]);
            return new Vector3(x, y, z);
        } catch
        {
            //Debug.Log(str);
            return new Vector3(0, 0, 0);
        }


        
    }

    IEnumerator SpawnCoroutine()
    {
        float dissolve = 1;
        dissolveMaterial.SetFloat(dissolveAmount, 1);
        dissolveMaterial.SetFloat(verticalDissolve, 0);
        for (int i = 0; i < 10; i++)
        {
            dissolve -= 0.1f;
            dissolveMaterial.SetFloat(dissolveAmount, dissolve);
            yield return new WaitForSeconds(0.1f);
        }
    }

    public IEnumerator DeSpawnCoroutine()
    {
        audiosource.PlayOneShot(deadClip);
        float vertical = 0;
        dissolveMaterial.SetFloat(dissolveAmount, 0);
        dissolveMaterial.SetFloat(verticalDissolve, 0);
        for (int i = 0; i < 10 ; i++)
        {
            vertical += 0.1f;
            dissolveMaterial.SetFloat(verticalDissolve, vertical);
            yield return new WaitForSeconds(0.1f);
        }
        if (this != null)
        {
            Destroy(this.gameObject);
            Destroy(ingameUIObject);
        }
    }

}
