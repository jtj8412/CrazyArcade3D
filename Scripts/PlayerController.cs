using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System;
using UnityEngine.UI;

public class PlayerController : MonoBehaviourPunCallbacks
{
    public GameObject waterPrisonPrefab;    // 물감옥 프리팹                    

    // Network
    public bool IsMine { get { return photonView.IsMine; } }    // 스크립트를 가진 오브젝트의 소유권이 해당 클라이언트인지 여부
    public int ViewID { get { return photonView.ViewID; } }     // 플레이어 고유 번호 ( 멀티 )

    // Game Status
    public float Power { set; get; }                        // 물줄기
    public float MoveSpeed { set { animator.SetFloat(moveSpeedId, value); } get { return animator.GetFloat(moveSpeedId); } }    // 이동속도
    public float JumpPower { set; get; }                    // 점프력
    public int BombCount { set; get; }                      // 물폭탄 최대 설치 갯수
    public int CurrentBombCount { set; get; }               // 현재 물폭탄 설치 갯수

    // Physics
    private const float Gravity = -20f; // 중력
    private const float DieTime = 5f;  // 물감옥에 갇히고 죽기까지의 시간
    private const float AeroDrag = 0.15f;   // 공기 저항
    private const float speedRatioOnJump = 0.8f;
    private float speedByGravity = 0f;
    private Vector3 velocity;   // 속도
    private Vector3 movement;

    // State
    private bool isInputedJump;         // 이전 프레임에서 점프키가 눌렸는지 여부
    public bool IsJumping { set { animator.SetBool(isJumpingId, value); } get { return animator.GetBool(isJumpingId); } }
    public bool IsRunning { set { animator.SetBool(isRunningId, value); } get { return animator.GetBool(isRunningId); } }
    public bool IsPushing { set { animator.SetBool(isPushingId, value); } get { return animator.GetBool(isPushingId); } }
    public bool IsPunching { set { animator.SetBool(isPunchingId, value); } get { return animator.GetBool(isPunchingId); } }
    public bool IsHit { set { animator.SetBool(isHitId, value); } get { return animator.GetBool(isHitId); } }
    public bool IsInWater { set { animator.SetTrigger(inWaterId); animator.SetBool(isInWaterId, value); } get { return animator.GetBool(isInWaterId); } }
    public bool IsDie { set { animator.SetBool(isDieId, value); } get { return animator.GetBool(isDieId); } }

    // State Hash
    private int isJumpingId;
    private int isRunningId;
    private int isInWaterId;
    private int inWaterId;
    private int isPushingId;
    private int isPunchingId;
    private int isHitId;
    private int isDieId;
    private int moveSpeedId;

    // Component
    private CharacterController characterController;
    private Animator animator;
    public Collider Collider { private set; get; }              
    public BoxCollider BoxCollider { private set; get; }

    void Awake()
    {
        // 필드 초기화
        cam = Camera.main;
        camTf = Camera.main.transform;
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
        Collider = characterController;
        BoxCollider = GetComponent<BoxCollider>();

        // 애니메이션 해시 초기화
        isJumpingId = Animator.StringToHash("isJumping");
        isRunningId = Animator.StringToHash("isRunning");
        isInWaterId = Animator.StringToHash("isInWater");
        inWaterId = Animator.StringToHash("inWater");
        isPushingId = Animator.StringToHash("isPushing");
        isPunchingId = Animator.StringToHash("isPunching");
        isHitId = Animator.StringToHash("isHit");
        moveSpeedId = Animator.StringToHash("moveSpeed");
        isDieId = Animator.StringToHash("isDie");

        // 능력치 초기화
        Power = 1;
        MoveSpeed = 1f;
        JumpPower = 7f;
        BombCount = 1;
        CurrentBombCount = 0;

        transform.parent = GameObject.Find("PlayerList").transform;

        // 해당 클라이언트가 컨트롤하는 플레이어에게 카메라 부착 및 플레이어 리스트에 추가
        if (IsMine)
        {
            CameraInit();
            Cursor.lockState = CursorLockMode.Locked;
            RPCEvent.Inst.AddPlayer(photonView.ViewID, PhotonNetwork.LocalPlayer.ActorNumber);
        }
    }

    void FixedUpdate()
    {
        if (!IsMine || IsDie) return;

        GravityUpdate();
        Run();
        Jump();

        // 공기 저항
        velocity.x *= 1 - AeroDrag;
        velocity.z *= 1 - AeroDrag;

        characterController.Move(transform.rotation * (velocity + movement) * Time.deltaTime);

        CheckPush();
    }

    // 물줄기 피격
    void OnTriggerEnter(Collider collider)
    {
        if (!IsMine) return;

        if (collider.CompareTag("WaterStream"))
        {
            RPCEvent.Inst.PlayerInWaterPrison(ViewID);
        }
    }

    // 블록 푸쉬
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!IsMine) return;
        
        if (hit.gameObject.CompareTag("Block"))
            Pushing(hit.gameObject, hit.point);
        if (IsInWater && hit.gameObject.CompareTag("Player"))
            _Die();
    }

    void Update()
    {
        if (!IsMine) return;

        InputMove();
        InputJump();
        InputPutBomb();
        InputPunch();
        InputReverse();

        if (transform.position.y < -5f)
            _Die();
    }

    void LateUpdate()
    {
        if (!IsMine) return;
        
        RotateCamera();
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

    // 중력 업데이트
    private void GravityUpdate()
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
            velocity.y += Gravity * Time.deltaTime;
            speedByGravity += Gravity * Time.deltaTime;
        }
    }

    // 점프
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

    // 물줄기 피격시
    public void HitWaterStream()
    {
        if (!IsInWater)
        {
            Instantiate(waterPrisonPrefab, transform).name = "WaterPrison";
            IsInWater = true;
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
        } while (time < DieTime);
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

    // 이동 방향키 입력 확인
    private void InputMove()
    {
        inputHorizontal = Input.GetAxisRaw("Horizontal") * camDir;
        inputVertical = Input.GetAxisRaw("Vertical");


    }

    // 점프키 입력 확인
    private void InputJump()
    {
        if (!Input.GetButtonDown("Jump")) return;
        if (IsJumping || IsInWater || IsHit) return;

        isInputedJump = true;
    }

    // 물폭탄 설치키 입력 확인
    private void InputPutBomb()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (IsJumping || IsInWater || IsHit) return;
        if (velocity.y < -0.41f || velocity.y > 0.41f) return;
        if (CurrentBombCount + 1 > BombCount) return;

        BombGenerator.Inst.CheckGenerateBomb(this);
        SoundManager.instance.PlayBubbleDrop();
    }

    // 카메라 회전키 입력 확인
    private void InputReverse()
    {
        if (!Input.GetKeyDown(KeyCode.R)) return;

        Vector3 rotation = camTf.rotation.ToEuler() * Mathf.Rad2Deg;
        rotation.y += 180f;
        camTf.rotation = Quaternion.Euler(rotation);

        camTargetPivot.z *= -1f;
        camDir *= -1;

    }

    // 펀치키 입력 확인
    private void InputPunch()
    {
        if (!Input.GetMouseButtonDown(1)) return;
        if (IsPushing || IsInWater || IsHit) return;

        StartCoroutine("Punch");
    }

    private IEnumerator Punch()
    {
        AnimatorStateInfo animStateInfo = animator.GetCurrentAnimatorStateInfo(0);  // 펀치 애니메이션 획득
        bool isAttack = false;
        const float hitDist = 0.2f;

        IsPunching = true;
        while (!animStateInfo.IsName("Punch"))  // 펀치 애니메이션으로 변경 까지 대기
        {
            yield return null;
            animStateInfo = animator.GetCurrentAnimatorStateInfo(0);

        }

        while (animStateInfo.IsName("Punch") && animStateInfo.normalizedTime < animStateInfo.length)
        {
            yield return null;
            animStateInfo = animator.GetCurrentAnimatorStateInfo(0);
            float animTime = animStateInfo.normalizedTime;
            if (!isAttack && animTime > animStateInfo.length * 0.5f && animTime < animStateInfo.length * 0.6f)  // 애니메이션 50~60% 진행시에 펀치 로직 진행
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

        if (IsInWater || IsHit)
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

        // 허용되는 회전각 안에 드는지 확인
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
        // 동일한 높이에 있는지 확인
        if (inputVertical <= 0f || Mathf.Abs(transform.position.y - block.transform.position.y) > 0.1f) return;
        
        // 밀던 블록이 아닐 경우
        if (pushBlock != block)
        {
            pushBlockTime = 0f;
            pushBlock = block;
            pushBlockController = block.GetComponent<BlockController>();
            pushBlockDir = new Vector3(0f, 0f, 0f);
        }

        Vector3 hitToBlockDist = pushBlock.transform.position - hitPoint;
        Vector3 dir;

        // x축 방향으로 블록이 밀릴 것인지, z축 방향으로 밀릴 것인지 확인
        if (Math.Abs(hitToBlockDist.x) > Math.Abs(hitToBlockDist.z))
            hitToBlockDist.Set(hitToBlockDist.x, 0f, 0f);
        else
            hitToBlockDist.Set(0f, 0f, hitToBlockDist.z);
        dir = hitToBlockDist.normalized;

        // 밀던 방향과 다를 경우 초기화
        if (pushBlockDir != dir) pushBlockTime = 0f;
        pushBlockDir = dir;

        // 밀기가 시작될 경우, 미는 방향에 맞춰 플레이어 회전
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

        // 효과음 재생
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
        if (IsPushing || IsDie) return;

        float rotateHorizontal = Input.GetAxis("Mouse X") * mouseHorizontalSensitivity;
        float rotateVertical = -Input.GetAxis("Mouse Y") * mouseVerticalSensitivity;
        float scroll = -Input.GetAxis("Mouse ScrollWheel");

        maxTargetToCamDist = Mathf.Clamp(maxTargetToCamDist + scroll, cameraZoomMin, cameraZoomMax);    // 카메라줌 한계치로 제한
        cameraVerticalRotation = Mathf.Clamp(cameraVerticalRotation + rotateVertical, cameraVerticalRotationMin, cameraVerticalRotationMax); // 카메라 수직 회전 한계치로 제한

        transform.Rotate(0f, rotateHorizontal, 0f);
        camTf.eulerAngles = new Vector3(cameraVerticalRotation, camTf.eulerAngles.y, camTf.eulerAngles.z);

        Vector3 targetPos = transform.position + transform.rotation * camTargetPivot;
        float targetDist = maxTargetToCamDist;
        Vector3[] rayEndPos = new Vector3[5];

        // 카메라 벽 통과 방지
        rayEndPos[0] = camTf.position;
        rayEndPos[1] = camTf.position + camTf.right * screenHalfWidth;
        rayEndPos[1] += (rayEndPos[1] - targetPos).normalized;
        rayEndPos[2] = camTf.position + -camTf.right * screenHalfWidth;
        rayEndPos[2] += (rayEndPos[2] - targetPos).normalized;
        rayEndPos[3] = camTf.position + camTf.up * screenHalfHeight;
        rayEndPos[3] += (rayEndPos[3] - targetPos).normalized;
        rayEndPos[4] = camTf.position + -camTf.up * screenHalfHeight;
        rayEndPos[4] += (rayEndPos[4] - targetPos).normalized;

        // 카메라를 가리는 충돌체 중 가장 멀리있는 쪽으로 카메라 확대
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
