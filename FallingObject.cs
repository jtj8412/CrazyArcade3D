using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 중력 및 충돌 구현 클래스
public class FallingObject : MonoBehaviour
{
    [SerializeField] private float accel;               // 가속도
    [SerializeField] private Vector3 pivot;             // 루트 pivot 
    [SerializeField] private LayerMask ignoreLayerMask; // 충돌 무시할 레이어
    public bool IsLanding { private set; get; }     // 착지 여부
    public bool IsEnable { set; get; }
    private float fallSpeed;                        // 낙하 속도
    private Vector3 boxRaySize;                     
    private BoxCollider boxCollider;
    private new Rigidbody rigidbody;

    void Awake()
    {
        IsEnable = true;
    }

    void OnEnable()
    {
        fallSpeed = 0f;
    }

    void Start()
    {
        // 필드 초기화
        boxCollider = GetComponent<BoxCollider>();
        rigidbody = GetComponent<Rigidbody>();
        boxRaySize = boxCollider.size * 0.99f;
        boxRaySize.y /= 2f;
    }

    void FixedUpdate()
    {
        if (IsEnable)
            Fall();
    }

    private void Fall()
    {
        // 속도에 중력 가속도 더하기
        fallSpeed += -accel * Time.deltaTime;
        float fallDist = Mathf.Abs(fallSpeed * Time.deltaTime);

        // 바닥 확인
        RaycastHit[] hits = Physics.BoxCastAll(transform.position + pivot, boxRaySize / 2f, Vector3.down, Quaternion.identity,
            fallDist + boxCollider.size.y / 4f, ~ignoreLayerMask);
        float maxY = float.MinValue;

        for (int i = 0; i < hits.Length; ++i)
        {
            RaycastHit hit = hits[i];

            if (hit.transform == transform || hit.collider.isTrigger || Physics.GetIgnoreCollision(boxCollider, hit.collider))
                continue;
            
            maxY = Mathf.Max(maxY, transform.position.y - hit.point.y);
        }
        
        // 가장 높은 바닥으로 이동
        if (maxY == float.MinValue)
        {
            rigidbody.MovePosition(transform.position + Vector3.down * fallDist);
            IsLanding = false;
        }
        else
        {
            rigidbody.MovePosition(transform.position + Vector3.down * (maxY - (boxCollider.size.y / 2f)) - pivot);
            fallSpeed = 0f;
            IsLanding = true;
        }

    }
}
