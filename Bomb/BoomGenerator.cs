using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoomGenerator : MonoBehaviour
{
    public GameObject boomPrefab;

    private LinkedList<GameObject> objPool;

    public static BoomGenerator Inst { get; private set; }

    BoomGenerator()
    {
        if (Inst != null) Application.Quit();
        Inst = this;
    }

    void Awake()
    {
        objPool = new LinkedList<GameObject>();
        for (int i = 0; i < 20; ++i) CreateBoom();
    }

    public void GenerateBoom(Vector3 position)
    {
        if (objPool.Count == 0) CreateBoom();

        GameObject boom = objPool.First.Value;
        objPool.RemoveFirst();
        boom.transform.position = position;
        boom.SetActive(true);
    }

    private void CreateBoom()
    {
        GameObject boom = Instantiate(boomPrefab, transform);
        boom.name = "Boom";
        boom.SetActive(false);
        objPool.AddFirst(boom);
    }

    public void DestroyBoom(GameObject waterStream)
    {
        waterStream.SetActive(false);
        objPool.AddFirst(waterStream);
    }
}
