using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System;
using UnityEngine.UI;

#pragma warning disable CS0618
#pragma warning disable CS0649
#pragma warning disable CS0109

public class PlayerController : MonoBehaviourPunCallbacks
{
    public GameObject waterPrisonPrefab;

    public float Power { set; get; }
    public float MoveSpeed { set { animator.SetFloat(moveSpeedId, value); } get { return animator.GetFloat(moveSpeedId); } }
    public float JumpPower { set; get; }
    public int BombCount { set; get; }
    public int CurrentBombCount { set; get; }
    public int ViewID { get { return photonView.ViewID; } }

    public Collider Collider { private set; get; }
    public BoxCollider BoxCollider { private set; get; }

    private bool isInputedJump;
    private bool isMenu;

    private const float GRAVITY = -20f;
    private const float DRAG = 0.15f;
    private const float DIE_TIME = 5f;//
    private Vector3 accel;
    private Vector3 velocity;
    private Vector3 movement;

    public bool IsJumping { set { animator.SetBool(isJumpingId, value); } get { return animator.GetBool(isJumpingId); } }
    public bool IsRunning { set { animator.SetBool(isRunningId, value); } get { return animator.GetBool(isRunningId); } }
    public bool IsPushing { set { animator.SetBool(isPushingId, value); } get { return animator.GetBool(isPushingId); } }
    public bool IsPunching { set { animator.SetBool(isPunchingId, value); } get { return animator.GetBool(isPunchingId); } }
    public bool IsHit { set { animator.SetBool(isHitId, value); } get { return animator.GetBool(isHitId); } }
    public bool IsInWater
    {
        set { animator.SetTrigger(inWaterId); animator.SetBool(isInWaterId, value); }
        get { return animator.GetBool(isInWaterId); }
    }
    public bool IsDie { set { animator.SetBool(isDieId, value); } get { return animator.GetBool(isDieId); } }

    public int StunCnt { set; get; }

    public bool IsMine { get { return photonView.IsMine; } }

    private CharacterController characterController;
    private Animator animator;

    private const float speedRatioOnJump = 0.8f;

    private int isJumpingId;
    private int isRunningId;
    private int isInWaterId;
    private int inWaterId;
    private int isPushingId;
    private int isPunchingId;
    private int isHitId;
    private int isDieId;

    private int moveSpeedId;

    void Awake()
    {
        cam = Camera.main;
        camTf = Camera.main.transform;
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
        Collider = characterController;
        BoxCollider = GetComponent<BoxCollider>();

        isJumpingId = Animator.StringToHash("isJumping");
        isRunningId = Animator.StringToHash("isRunning");
        isInWaterId = Animator.StringToHash("isInWater");
        inWaterId = Animator.StringToHash("inWater");
        isPushingId = Animator.StringToHash("isPushing");
        isPunchingId = Animator.StringToHash("isPunching");
        isHitId = Animator.StringToHash("isHit");
        moveSpeedId = Animator.StringToHash("moveSpeed");
        isDieId = Animator.StringToHash("isDie");

        Power = 1;
        MoveSpeed = 1f;
        JumpPower = 7f;
        BombCount = 1;
        CurrentBombCount = 0;

        transform.parent = GameObject.Find("PlayerList").transform;

        if (IsMine)
        {
            CameraInit();
            Cursor.lockState = CursorLockMode.Locked;
            RPCEvent.Inst.AddPlayer(photonView.ViewID, PhotonNetwork.LocalPlayer.ActorNumber);
        }
    }

    void FixedUpdate()
    {
        if (photonView.IsMine)
        {
            Gravity();
            Run();
            Jump();

            velocity += accel * Time.deltaTime;
            velocity.x *= 1 - DRAG;
            velocity.z *= 1 - DRAG;
            if (!IsDie)
                characterController.Move(transform.rotation * (velocity + movement) * Time.deltaTime);
            CheckPush();
        }
    }

    void OnTriggerEnter(Collider collider)
    {
        //if (photonView.IsMine) -> 클라이언트 리스트 성공시 추가
        if (photonView.IsMine && collider.CompareTag("WaterStream"))
        {
            RPCEvent.Inst.PlayerInWaterPrison(ViewID);
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (IsInWater && collision.gameObject.CompareTag("Player"))
        {
            // GameOver
        }
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (photonView.IsMine)
        {
            if (hit.gameObject.CompareTag("Block"))
                Pushing(hit.gameObject, hit.point);
            if (IsInWater && hit.gameObject.CompareTag("Player"))
                _Die();
        }
    }

    void Update()
    {
        if (!photonView.IsMine) return;

        InputMove();
        InputJump();
        InputPutBomb();
        InputPunch();
        InputReverse();
        InputMenu();

        if (transform.position.y < -5f)
            _Die();
    }

    private void InputMenu()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        isMenu = !isMenu;

        if (isMenu)
        {
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    void LateUpdate()
    {
        if (photonView.IsMine)
        {
            RotateCamera();
        }
    }

    ///////////////////////////////////////////////////////////////////////

    public void AddForce(Vector3 force)
    {
        velocity += force;
    }

    public void AddForce(float x, float y, float z)
    {
        AddForce(new Vector3(x, y, z));
    }

    public void Stop()
    {
        velocity = Vector3.zero;
    }

    private float speedSumByGravity = 0f;
    private void Gravity()
    {
        if ((characterController.collisionFlags & CollisionFlags.Below) != 0)
        {
            if (velocity.y < 0f)
                velocity.y = 0f;
            if (IsJumping)
                IsJumping = false;
        }
        else
        {
            velocity.y += GRAVITY * Time.deltaTime;
            speedSumByGravity += GRAVITY * Time.deltaTime;
        }
    }

    private void Jump()
    {
        if (isInputedJump)
        {
            AddForce(Vector3.up * JumpPower);
            SoundManager.instance.PlayJump();

            IsJumping = true;
            isInputedJump = false;
            IsPushing = false;
        }
    }

    public void HitWaterStream()
    {
        if (!IsInWater)
        {
            Instantiate(waterPrisonPrefab, transform).name = "WaterPrison";
            IsInWater = true;
            StunCnt++;
            SoundManager.instance.PlayBubbleAttack();
            StartCoroutine("InWater");
        }
    }

    IEnumerator InWater()
    {
        float time = 0f;
        do
        {
            yield return null;
            time += Time.deltaTime;
        } while (time < DIE_TIME);
        _Die();
    }

    private void _Die()
    {
        if (!IsMine) return;
        Transform camParentTf = new GameObject("Camera").transform;
        camParentTf.rotation = Quaternion.Euler(Vector3.zero);
        camTf.SetParent(camParentTf);
        camTf.gameObject.AddComponent<CameraController>();
        RPCEvent.Inst.PlayerDie(ViewID);
        Cursor.lockState = CursorLockMode.None;
    }

    public void Die()
    {
        IsDie = true;
        if (IsInWater)
        {
            GameObject obj = transform.FindChild("WaterPrison").gameObject;
            Destroy(obj);
        }
        IsInWater = false;

        transform.position += Vector3.down * 0.3f;
        GetComponent<BoxCollider>().enabled = false;
        GetComponent<CharacterController>().enabled = false;
        SoundManager.instance.PlayBubblePopping();

        int alive = 0;
        foreach (PlayerController p in RPCEvent.Inst.PlayerControllerDict.Values)
        {
            if (!p.IsDie) alive++;
        }

        if (alive < 2)
        {
            StartCoroutine("GameOver");
        }
    }

    IEnumerator GameOver()
    {
        float time = 0f;
        Image image = null;
        if (!RPCEvent.Inst.MyPlayerController.IsDie)
        {
            image = GameObject.Find("Victory").GetComponent<Image>(); ;
            SoundManager.instance.PlayWinSound();
        }
        else
        {
            image = GameObject.Find("Defeat").GetComponent<Image>();
            SoundManager.instance.PlayLoseSound();
        }

        image.enabled = true;

        do
        {
            image.color = new Color(1, 1, 1, time / 6f);
            yield return null;
            time += Time.deltaTime;
        } while (time <= 8f);

        PhotonNetwork.LoadLevel("Lobby");
        PhotonNetwork.LeaveRoom();
    }


    private void InputMove()
    {
        inputHorizontal = Input.GetAxisRaw("Horizontal") * camDir;
        inputVertical = Input.GetAxisRaw("Vertical");


    }

    private void InputJump()
    {
        if (!Input.GetButtonDown("Jump")) return;
        if (IsJumping || StunCnt != 0) return;

        isInputedJump = true;
    }

    private void InputPutBomb()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (IsJumping || StunCnt != 0) return;
        if (velocity.y < -0.41f || velocity.y > 0.41f) return;
        if (CurrentBombCount + 1 > BombCount) return;
        if (isMenu) return;

        BombGenerator.Inst.GenerateBomb(this);
        SoundManager.instance.PlayBubbleDrop();
    }

    private void InputReverse()
    {
        if (!Input.GetKeyDown(KeyCode.R)) return;

        Vector3 rotation = camTf.rotation.ToEuler() * Mathf.Rad2Deg;
        rotation.y += 180f;
        camTf.rotation = Quaternion.Euler(rotation);

        camTargetPivot.z *= -1f;
        camDir *= -1;

    }

    private void InputPunch()
    {
        if (!Input.GetMouseButtonDown(1)) return;
        if (IsPushing || StunCnt != 0) return;

        StartCoroutine("Punch");
    }

    private IEnumerator Punch()
    {
        AnimatorStateInfo animStateInfo = animator.GetCurrentAnimatorStateInfo(0);
        bool isAttack = false;
        const float hitDist = 0.2f;

        IsPunching = true;
        while (!animStateInfo.IsName("Punch"))
        {
            yield return null;
            animStateInfo = animator.GetCurrentAnimatorStateInfo(0);

        }

        while (animStateInfo.IsName("Punch") && animStateInfo.normalizedTime < animStateInfo.length)
        {
            yield return null;
            animStateInfo = animator.GetCurrentAnimatorStateInfo(0);
            float animTime = animStateInfo.normalizedTime;
            if (!isAttack && animTime > animStateInfo.length * 0.5f && animTime < animStateInfo.length * 0.6f)
            {
                Vector3 boxRaySize = new Vector3(characterController.radius * 2f, characterController.height / 2f, 0.01f);
                if (Physics.BoxCast(transform.position, boxRaySize / 2f, transform.forward, out RaycastHit hit, transform.rotation, BoxCollider.size.z + hitDist,
                    LayerMask.GetMask("Player")))
                {
                    PlayerController playerController = hit.collider.GetComponent<PlayerController>();
                    Quaternion hitDir = Quaternion.LookRotation(transform.position - playerController.transform.position);
                    hitDir.x = hitDir.z = 0f;
                    SoundManager.instance.PlayPunch();
                    RPCEvent.Inst.HitPunch(playerController.ViewID, hitDir);
                    isAttack = true;

                }
            }
        }

        IsPunching = false;
    }

    private void Run()
    {

        if (StunCnt != 0)
        {
            movement.Set(0f, 0f, 0f);
            return;
        }

        if ((inputVertical != 0f || inputHorizontal != 0f) && !IsJumping)
        {
            IsRunning = true;

        }
        else
            IsRunning = false;

        if (IsPushing)
        {
            inputHorizontal = 0f;
            if (inputVertical <= 0f)
            {
                IsPushing = false;
            }
        }
        movement = (Vector3.forward * inputVertical + Vector3.right * inputHorizontal) * MoveSpeed * 1.5f;

        if (IsJumping)
            movement *= speedRatioOnJump;
    }

    IEnumerator HitPunch(Quaternion hitDir)
    {
        AnimatorStateInfo animStateInfo = animator.GetCurrentAnimatorStateInfo(0);

        IsHit = true;
        StunCnt++;
        transform.rotation = hitDir;

        while (!animStateInfo.IsName("Hit"))
        {
            yield return null;
            animStateInfo = animator.GetCurrentAnimatorStateInfo(0);
        }

        Stop();
        AddForce(0f, 2.5f, -4f);

        while (animStateInfo.IsName("Hit") && animStateInfo.normalizedTime < animStateInfo.length)
        {
            yield return null;
            animStateInfo = animator.GetCurrentAnimatorStateInfo(0);
        }

        IsHit = false;
        StunCnt--;
    }


    //////////////////// Block ////////////////////

    private GameObject pushBlock;
    private BlockController pushBlockController;
    private Vector3 pushBlockDir;
    private float pushBlockTime;
    private float prevPushBlockTime;
    private const float requiredPushStartTime = 0.3f;
    private const float requiredPushTime = 1f;
    private const float pushRotationSpeed = 0.1f; // 0 ~ 1
    private const float limitPushAngle = 30f;

    private void CheckPush()
    {
        if (prevPushBlockTime == pushBlockTime)
        {
            pushBlockTime = 0f;
            IsPushing = false;
        }
        prevPushBlockTime = pushBlockTime;
    }

    private bool IsCorrectPushAngle()
    {
        float requiredHorizontalRotation = Quaternion.LookRotation(pushBlockDir).ToEuler().y * Mathf.Rad2Deg;
        float horizontalRotation = transform.rotation.ToEuler().y * Mathf.Rad2Deg;

        if (requiredHorizontalRotation + limitPushAngle > horizontalRotation
            && requiredHorizontalRotation - limitPushAngle < horizontalRotation)
            return true;

        if ((int)requiredHorizontalRotation == 180
            && limitPushAngle - 180f > horizontalRotation)
            return true;

        return false;
    }

    private void Pushing(GameObject block, Vector3 hitPoint)
    {
        if (inputVertical <= 0f || Mathf.Abs(transform.position.y - block.transform.position.y) > 0.1f)
            return;

        if (pushBlock != block)
        {
            pushBlockTime = 0f;
            pushBlock = block;
            pushBlockController = block.GetComponent<BlockController>();
            pushBlockDir = new Vector3(0f, 0f, 0f);
        }

        Vector3 hitToBlockDist = pushBlock.transform.position - hitPoint;
        Vector3 dir;

        if (Math.Abs(hitToBlockDist.x) > Math.Abs(hitToBlockDist.z))
        {
            hitToBlockDist.Set(hitToBlockDist.x, 0f, 0f);
        }
        else
        {
            hitToBlockDist.Set(0f, 0f, hitToBlockDist.z);
        }

        dir = hitToBlockDist.normalized;

        if (pushBlockDir != dir) pushBlockTime = 0f;
        pushBlockDir = dir;

        if (pushBlockTime > requiredPushStartTime && !IsJumping)
        {
            IsPushing = true;
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(pushBlockDir), pushRotationSpeed);
        }

        if (!IsCorrectPushAngle())
        {
            pushBlockTime = 0f;
            return;
        }

        pushBlockTime += Time.deltaTime;

        if (pushBlockTime > requiredPushTime && pushBlockController.CanMove(pushBlockDir))
        {
            RPCEvent.Inst.BlockMove(pushBlockController.Index, pushBlockDir);
            SoundManager.instance.PlayBoxPushSound();
        }
    }

    //////////////////// Camera ////////////////////

    private float maxTargetToCamDist = 1.2f;
    private const float cameraZoomMax = 4.5f; // 2.5f
    private const float cameraZoomMin = 0.5f;

    private Vector3 camTargetPivot;
    private Camera cam;
    private Transform camTf;
    [SerializeField] private LayerMask cameraIgnoreLayerMask;
    private int camDir = 1;
    private float inputHorizontal;
    private float inputVertical;
    private float cameraVerticalRotation;
    private float screenHalfWidth;
    private float screenHalfHeight;
    private const float mouseHorizontalSensitivity = 1.5f;
    private const float mouseVerticalSensitivity = 1f;
    private const float cameraVerticalRotationMax = 35f;
    private const float cameraVerticalRotationMin = -35f;

    void CameraInit()
    {
        cameraVerticalRotation = camTf.rotation.ToEuler().x * 57.3f;
        screenHalfWidth = Mathf.Tan(Mathf.Deg2Rad * 36f) * cam.nearClipPlane;
        screenHalfHeight = screenHalfWidth / cam.aspect;
        camTargetPivot = new Vector3(0f, 0.4f, 0f);
    }

    private void RotateCamera()
    {
        if (IsPushing || isMenu || IsDie)
            return;

        float rotateHorizontal = Input.GetAxis("Mouse X") * mouseHorizontalSensitivity;
        float rotateVertical = -Input.GetAxis("Mouse Y") * mouseVerticalSensitivity;
        float scroll = -Input.GetAxis("Mouse ScrollWheel");

        maxTargetToCamDist = Mathf.Clamp(maxTargetToCamDist + scroll, cameraZoomMin, cameraZoomMax);

        if (cameraVerticalRotation + rotateVertical > cameraVerticalRotationMax)
            rotateVertical = cameraVerticalRotationMax - cameraVerticalRotation;
        else if (cameraVerticalRotation + rotateVertical < cameraVerticalRotationMin)
            rotateVertical = cameraVerticalRotationMin - cameraVerticalRotation;

        cameraVerticalRotation += rotateVertical;

        transform.Rotate(0f, rotateHorizontal, 0f);
        camTf.Rotate(rotateVertical, 0f, 0f);

        //
        Vector3 targetPos = transform.position + transform.rotation * camTargetPivot;
        float targetDist = maxTargetToCamDist;
        Vector3[] rayEndPos = new Vector3[5];

        rayEndPos[0] = camTf.position;
        rayEndPos[1] = camTf.position + camTf.right * screenHalfWidth;
        rayEndPos[1] += (rayEndPos[1] - targetPos).normalized;
        rayEndPos[2] = camTf.position + -camTf.right * screenHalfWidth;
        rayEndPos[2] += (rayEndPos[2] - targetPos).normalized;
        rayEndPos[3] = camTf.position + camTf.up * screenHalfHeight;
        rayEndPos[3] += (rayEndPos[3] - targetPos).normalized;
        rayEndPos[4] = camTf.position + -camTf.up * screenHalfHeight;
        rayEndPos[4] += (rayEndPos[4] - targetPos).normalized;

        for (int i = 0; i < rayEndPos.Length; ++i)
        {
            if (Physics.Linecast(targetPos, rayEndPos[i], out RaycastHit hit, ~cameraIgnoreLayerMask))
            {
                targetDist = Mathf.Min(hit.distance, targetDist);
            }
        }

        camTf.position = targetPos + (-camTf.forward * targetDist);
    }
}
