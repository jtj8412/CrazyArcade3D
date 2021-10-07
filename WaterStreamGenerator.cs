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
            // pop
            GameObject obj = objPool.First.Value;
            objPool.RemoveFirst();

            Transform tf = obj.transform;
            Vector3 boxRaySize = Vector3.one * 0.99f;
            float[] length = new float[2];
            int[] index = new int[2];
            index[0] = index[1] = -1;

            tf.rotation = Quaternion.Euler(rotation);

            // 물폭탄 앞뒤로 BoxCast,
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
                else
                {
                    length[i] = power;
                }
            }

            Vector3 desiredPos = boomPosition + tf.forward * length[0];
            desiredPos += boomPosition + -tf.forward * length[1];
            desiredPos *= 0.5f;

            Vector3 desiredMoveVec = desiredPos - boomPosition;

            Vector3 desiredScale = Vector3.one + Vector3.forward * (length[0] + length[1]);
            desiredScale *= 0.99f;

            obj.transform.position = boomPosition;
            obj.transform.localScale = Vector3.zero;
            obj.SetActive(true);
            obj.GetComponent<WaterStreamController>().StartCoroutine("Boom", new object[4] { desiredPos, desiredMoveVec, desiredScale, index});
        }

        generate(new Vector3(0f, 0f, 0f));
        generate(new Vector3(0f, 90f, 0f));
        generate(new Vector3(90f, 0f, 0f));
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
