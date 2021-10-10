using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

#pragma warning disable CS0649

public class WaterStreamGenerator : MonoBehaviour
{
    public GameObject waterStreamPrefab;

    private LinkedList<GameObject> objPool;                 // 오브젝트 풀
    [SerializeField] private LayerMask nonpassLayerMask;    // 통과 불가능 레이어

    public static WaterStreamGenerator Inst { get; private set; }   // 싱글톤

    WaterStreamGenerator()
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
            CreateWaterStream();
    }

    // 물줄기 생성
    public void GenerateWaterStream(Vector3 boomPosition, float power)
    {
        if (objPool.Count < 2) CreateWaterStream();

        void generate(Vector3 rotation)
        {
            // pool pop
            GameObject obj = objPool.First.Value;
            objPool.RemoveFirst();

            Transform tf = obj.transform;
            Vector3 boxRaySize = Vector3.one * 0.99f;
            float[] length = {power, power};            // 근원지로부터 물줄기가 뻗어나갈 수 있는 최대 길이 (앞, 뒤)
            int[] index = {-1, -1};                     // 물줄기와 충돌한 블록의 인덱스

            tf.rotation = Quaternion.Euler(rotation);

            // 물폭탄 앞뒤로 BoxCast 하여 물줄기가 통과하지 못하는 충돌체 감지 및 거리 측정 (블록, 벽)
            for (int i = 0; i < 2; ++i)
            {
                if (Physics.BoxCast(boomPosition, boxRaySize / 2f, tf.forward * ((i == 0) ? 1 : -1), 
                    out RaycastHit hit, tf.rotation, power, nonpassLayerMask))
                {
                    length[i] = hit.distance;
                    if (hit.collider.CompareTag("Block"))
                    {
                        index[i] = hit.collider.GetComponent<BlockController>().Index;
                    }
                }
            }

            // 벽에 막힐 경우 폭발 근원지와 팽창시 위치가 다를 수 있음
            Vector3 expandedPos = boomPosition + tf.forward * (length[0] + length[1]) * 0.5f;           // 물줄기 팽창시 위치, 앞뒤 충돌체의 위치를 반으로 나눠 중간 위치 획득
            Vector3 toExpandedVec = expandedPos - boomPosition;                                         // 팽창시 위치까지의 거리 벡터
            Vector3 expandedScale = (Vector3.one + Vector3.forward * (length[0] + length[1])) * 0.99f;  // 물줄기 최종 크기

            obj.transform.position = boomPosition;      // 폭발 근원지가 물줄기의 최초 위치
            obj.transform.localScale = Vector3.zero;    // 물줄기의 크기 0에서 시작
            obj.SetActive(true);
            obj.GetComponent<WaterStreamController>().StartCoroutine("Boom", new object[4] { expandedPos, toExpandedVec, expandedScale, index}); // 실제 물줄기 로직
        }

        generate(new Vector3(0f, 0f, 0f));  // 앞 방향 물줄기
        generate(new Vector3(0f, 90f, 0f)); // 위 방향 물줄기
        generate(new Vector3(90f, 0f, 0f)); // 옆 방향 물줄기
    }

    // 물줄기 파괴
    public void DestroyWaterStream(GameObject waterStream)
    {
        waterStream.SetActive(false);
        objPool.AddLast(waterStream);
    }

    // 물줄기 push
    private void CreateWaterStream()
    {
        for (int i = 0; i < 2; ++i)
        {
            GameObject waterStream = Instantiate(waterStreamPrefab, transform);
            waterStream.SetActive(false);
            objPool.AddLast(waterStream);
        }
    }
}
