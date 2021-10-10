using System.Collections;
using UnityEngine;
using Photon.Pun;

// 아이템 추상 클래스
abstract public class ItemController : MonoBehaviourPun
{
    private FallingObject fallingObject;    // 중력 스크립트
    public int ItemNum { set; get; }        // 아이템 번호
    public int Index { set; get; }          // 아이템 고유 인덱스

    private static readonly float rotateSpeed = 30f;        // 애니메이션 회전 속도
    private static readonly float jumpDuration = 1.2f;      // 애니메이션 점프 시간
    private static readonly float jumpDist = 0.2f;          // 애니메이션 점프 거리
    private static readonly float slope = -jumpDist / (jumpDuration * jumpDuration / 4); // 기울기(a)값, y = ax^2

    void Awake()
    {
        // 필드 초기화
        fallingObject = GetComponent<FallingObject>();
    }

    void OnEnable()
    {   
        transform.Rotate(0f, Random.Range(0f, 90f), 0f);    // 시작시 랜덤 y축 회전
    }

    void OnTriggerEnter(Collider collider)
    {
        // 아이템이 물줄기에 닿을 경우 파괴 ( MasterClient 전용 )
        if (collider.CompareTag("WaterStream") && RPCEvent.Inst.IsMasterClient)
        {
            RPCEvent.Inst.ItemDestroy(Index);
        }
        // 아이템에 플레이어(Mine)가 닿을 경우 획득
        else if (collider.CompareTag("Player") && collider.GetComponent<PlayerController>().IsMine) // Mine
        {
            SoundManager.instance.PlayItemPick();
            GainByPlayer(collider);
        }
    }

    void OnTriggerStay(Collider collider)
    {
        // 블록과 겹칠 경우 파괴 ( MasterClient 전용 )
        if (collider.CompareTag("Block"))
        {
            if (Vector3.Distance(transform.position, collider.transform.position) < 0.4f && RPCEvent.Inst.IsMasterClient)
            {
                RPCEvent.Inst.ItemDestroy(Index);
            }
        }
    }

    /////////////////////////////////////////////

    abstract public void RPCGainItem(int viewID);

    private void GainByPlayer(Collider collider)
    {
        RPCEvent.Inst.GainItem(collider.GetComponent<PlayerController>().ViewID, Index);
    }

    public void Destroy()
    {
        StopAllCoroutines();
    }

    // 아이템 등장시 점프 애니메이션
    private IEnumerator Jump()
    {
        float time, result, resultPrev;
        Vector3 originPos, pos;

        pos = originPos = transform.position;
        time = result = resultPrev = 0f;

        fallingObject.IsEnable = false;
        do
        {
            time += Time.deltaTime;
            result = slope * time * (time - jumpDuration);
            if (result < resultPrev)
                break;
            pos.y = originPos.y + result;
            transform.position = pos;
            resultPrev = result;
            yield return null;
        } while (time <= jumpDuration);
        fallingObject.IsEnable = true;

        while (!fallingObject.IsLanding)
            yield return null;

        StartCoroutine("Rotate");
    }

    // 아이템 회전 애니메이션
    private IEnumerator Rotate()
    {
        while (true)
        {
            transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f);
            yield return null;
        }
    }
}

