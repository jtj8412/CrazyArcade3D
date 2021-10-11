using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombGenerator : MonoBehaviour
{
    public GameObject bombPrefab;           // 폭발 프리팹

    private LinkedList<GameObject> objPool; // 오브젝트 풀

    public static BombGenerator Inst { get; private set; }  // 싱글톤

    BombGenerator()
    {
        // 싱글톤 초기화
        if (Inst != null) Application.Quit();
        Inst = this;
    }

    void Awake()
    {
        // 오브젝트 풀 초기화
        objPool = new LinkedList<GameObject>();
        for (int i = 0; i < 10; ++i)
            CreateBomb();
    }

    // 물폭탄 생성
    public void CheckGenerateBomb(PlayerController playerController)
    {
        if (objPool.Count == 0)
            CreateBomb();

        Vector3 playerPosition = playerController.transform.position;
        Vector3 putPosition = new Vector3(Mathf.Round(playerPosition.x), Mathf.Round(playerPosition.y), Mathf.Round(playerPosition.z));
        Vector3 boxRaySize = new Vector3(0.99f, 0.019f, 0.99f);
        RaycastHit[] hits = Physics.BoxCastAll(putPosition + Vector3.up * 0.51f, boxRaySize / 2f, Vector3.down, Quaternion.identity, 1f);
        
        // BoxCast를 통해 설치 위치에 다른 충돌체가 있는지 확인
        for (int i = 0; i < hits.Length; ++i)
        {
            RaycastHit hit = hits[i];

            if (!hit.collider.isTrigger && !ReferenceEquals(playerController.transform, hit.transform))
                return;
        }

        // 바닥이 없을 경우(공중) 설치 불가
        if (!Physics.Raycast(putPosition, Vector3.down, 1f))
            return;

        RPCEvent.Inst.BombGenerate(playerController.ViewID, putPosition); // 물폭탄 설치
    }

    // 물폭탄 생성 (다른 클라이언트의 생성)
    public void RPCGenerateBomb(PlayerController playerController, Vector3 putPosition)
    {
        if (objPool.Count == 0)
            CreateBomb();

        // pop
        GameObject bomb = objPool.First.Value;
        objPool.RemoveFirst();

        // 물폭탄 소유주 설정
        BombController controller = bomb.GetComponent<BombController>();
        controller.Init(playerController);

        // 플레이어의 물폭탄 갯수
        playerController.CurrentBombCount++;

        // 물폭탄 배치
        bomb.transform.position = putPosition;
        bomb.SetActive(true);
    }

    // 물폭탄 제거
    public void DestroyBomb(BombController bombController)
    {
        bombController.gameObject.SetActive(false);
        objPool.AddLast(bombController.gameObject);
    }

    // 물폭탄 push
    private void CreateBomb()
    {
        GameObject bomb = Instantiate(bombPrefab, transform);
        bomb.name = "Bomb";
        bomb.SetActive(false);
        objPool.AddFirst(bomb);
    }
}
