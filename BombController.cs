using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class BombController : MonoBehaviour
{
    private BoxCollider boxCollider;
    private PlayerController owner;                     // 물폭탄을 설치한 플레이어
    private Transform graphicTf;                        // 물폭탄 메쉬 Transform
    private Vector3 originScale;                        // 물폭탄 원래 크기 ( 애니메이션 효과로 커졌다 작아졌다 함)
    private const float minZoomRatio = 0.95f;           // 축소 애니메이션 크기 비율
    private const float maxZoomRatio = 1.05f;           // 확대 애니메이션 크기 비율
    private const float zoomSpeed = 0.15f;              // 확대/축소 애니메이션 속도
    private static readonly float timeForBoom = 2.5f;   // 폭발 시간

    void Awake()
    {
        // 필드 초기화
        graphicTf = GetComponentsInChildren<Transform>()[1];
        originScale = graphicTf.localScale;
        boxCollider = GetComponent<BoxCollider>();
    }

    void OnEnable()
    {
        StartCoroutine("Zoom");     // 확대/축소 애니메이션
        StartCoroutine("Boom");     // 폭발 타이머
    }

    // 물줄기에 충돌 될 경우 연쇄폭발
    void OnTriggerEnter(Collider collider)
    {
        if (collider.CompareTag("WaterStream"))
            DestroyBomb();
    }

    // 플레이어가 물폭탄을 벗어나면 충돌 재허용
    void OnTriggerExit(Collider collider)
    {
        if (collider.gameObject.CompareTag("Player"))
        {
            if (ReferenceEquals(collider.gameObject, owner.gameObject) && owner.IsMine)
                IgnoreOwner(false);
        }
    }

    // 물폭탄 소유 플레이어 설정
    public void Init(PlayerController owner)
    {
        this.owner = owner;
        IgnoreOwner(true);
    }

    // 폭탄 제거
    public void DestroyBomb()
    {
        SoundManager.instance.PlayBubblePopping();  // 사운드 재생
        IgnoreOwner(false);         // 충돌 무시 해제
        owner.CurrentBombCount--;   // 소유 플레이어의 폭탄 개수 1 감소
        WaterStreamGenerator.Inst.GenerateWaterStream(transform.position, owner.Power); // 물줄기 생성
        owner = null;               
        StopAllCoroutines();        
        BombGenerator.Inst.DestroyBomb(this);       // 오브젝트 풀 pop
    }

    // 소유자가 폭탄을 설치할 경우 겹칠 수 있도록 충돌 무시 설정, 폭탄을 벗어날 경우 충돌 무시 해제
    private void IgnoreOwner(bool isIgnore)
    {
        Physics.IgnoreCollision(owner.Collider, boxCollider, isIgnore);
        Physics.IgnoreCollision(owner.BoxCollider, boxCollider, isIgnore);
    }

    // 폭발 타이머
    IEnumerator Boom()
    {
        float boomTime = 0f;

        do {
            yield return null;
            boomTime += Time.deltaTime;
        } while (boomTime < timeForBoom);
        DestroyBomb();
    }

    // 확대/축소 애니메이션
    IEnumerator Zoom()
    {
        float ratio = minZoomRatio;

        graphicTf.localScale = originScale * minZoomRatio;

        while (true)
        {
            while (ratio < maxZoomRatio)
            {
                yield return null;
                ratio += zoomSpeed * Time.deltaTime;
                graphicTf.localScale = originScale * ratio;
            }
            graphicTf.localScale = originScale * maxZoomRatio;
            ratio = maxZoomRatio;


            while (ratio > minZoomRatio)
            {
                yield return null;
                ratio -= zoomSpeed * Time.deltaTime;
                graphicTf.localScale = originScale * ratio;
            }
            graphicTf.localScale = originScale * minZoomRatio;
            ratio = minZoomRatio;
        }
    }


}
