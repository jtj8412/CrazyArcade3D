using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using System;

public class BlockController : MonoBehaviour
{
    public int Index { set; get; }                      // 블록 고유 인덱스 ( 멀티 환경 위해 구분 )
    public int ItemNum { set; get; }                    // 블록을 파괴시 드랍하는 아이템 번호
    public BoxCollider BoxCollider { set; get; }        
    private FallingObject fallingObject;                // 중력 스크립트
    public GameObject SpaceBlock { set; private get; }
    private Vector3 boxRaySize;                         
    private float moveDist;                             // 블록이 밀리는중일때, 현 프레임까지 움직인 거리
    public bool IsMoving { set; get; }                  // 블록이 밀리는 중인지 여부

    private static readonly float moveSpeed = 1.4f;     // 블록이 움직이는 속도
    public static readonly float moveDistLimit = 1f;    // 블록이 한 번 밀릴 때 움직이는 거리

    void Start()
    {
        // 필드 초기화
        BoxCollider = GetComponent<BoxCollider>();
        fallingObject = GetComponent<FallingObject>();
        boxRaySize = BoxCollider.size * 0.99f;
    }

    void OnTriggerEnter(Collider collider)
    {
        // 물줄기에 닿을 경우 파괴 ( MasterClient 전용 )
        if (collider.transform.CompareTag("WaterStream") && RPCEvent.Inst.IsMasterClient)
        {
            if (SpaceBlock != null)
                BlockGenerator.Inst.DestroySpaceBlock(Index, SpaceBlock);
            RPCEvent.Inst.BlockDestory(Index);
        }
    }
    
    // 블록이 밀릴 수 있는지 확인
    public bool CanMove(Vector3 moveDir)
    {
        // BoxCast를 통해 밀릴 방향에 다른 충돌체가 있는지 확인
        if (Physics.BoxCast(transform.position, boxRaySize / 2f, moveDir, transform.rotation, moveDistLimit, ~BlockGenerator.Inst.IgnoreLayerMask)
            || IsMoving                     // 움직이는 중인지 확인
            || !fallingObject.IsLanding)    // 바닥에 붙어있는지 확인
            return false;
        return true;
    }

    // 블록 파괴
    public void Destroy()
    {
        BoomGenerator.Inst.GenerateBoom(transform.position);            // 파티클 생성
        gameObject.SetActive(false);                                    
        Destroy(gameObject);                                            
        ItemGenerator.Inst.GenerateItem(ItemNum, transform.position);   // 아이템 생성
        StopAllCoroutines();                                           
        SoundManager.instance.PlayBoxBreak();                           // 사운드 재생
    }

    // 이동 트리거 및 코루틴 ON
    public void Move(Vector3 moveDir)
    {
        moveDist = 0f;
        IsMoving = true;
        fallingObject.IsEnable = false;     // 중력 OFF
        StartCoroutine("_Move", moveDir);
    }

    // 이동 코루틴
    private IEnumerator _Move(Vector3 moveDir)
    {
        float resultDist;

        while ((resultDist = moveDist + moveSpeed * Time.deltaTime) < moveDistLimit)    // 이동을 마칠 때 까지 이동
        {
            moveDist = resultDist;
            transform.position = (transform.position + moveDir * moveSpeed * Time.deltaTime);
            yield return new WaitForFixedUpdate();
        }
        transform.position = (transform.position + moveDir * (moveDistLimit - moveDist));       

        transform.position = new Vector3(Mathf.Round(transform.position.x), Mathf.Round(transform.position.y), 
                                        Mathf.Round(transform.position.z));             // 이동을 마칠 경우 위치에 소수점 제거   
        IsMoving = false;
        fallingObject.IsEnable = true;  // 중력 ON
        BlockGenerator.Inst.DestroySpaceBlock(Index, SpaceBlock);   // 접근 불가 공간 삭제
        SpaceBlock = null;
    }
}
