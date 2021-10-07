using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#pragma warning disable CS0649

public class ItemGenerator : MonoBehaviour
{
    [System.Serializable]
    class Item
    {
        public ItemController itemController;   // 아이템 스크립트
        public int chance;                      // 등장 확률 (정수)
    }

    [SerializeField] private Item[] items;      // 아이템 리스트
    private LinkedList<GameObject>[] objPools;  // 오브젝트 풀
    private Dictionary<int, ItemController> controllerDict; // 아이템 고유의 Index 값에 따라 인스턴스 저장
    private int chanceSum;      // 등장 확률 합계
    public static ItemGenerator Inst { get; private set; }  // 싱글톤

    ItemGenerator()
    {
        // 싱글톤 초기화
        if (Inst != null) Application.Quit();
        Inst = this;
    }

    void Awake()
    {
        // 오브젝트 풀 초기화
        objPools = new LinkedList<GameObject>[items.Length];
        controllerDict = new Dictionary<int, ItemController>();

        for (int i = 0; i < items.Length; ++i)
        {
            objPools[i] = new LinkedList<GameObject>();
            CreateItem(i, 6);
        }

        for (int i = 0; i < items.Length; ++i)
            chanceSum += items[i].chance;
    }
    
    // 아이템 push
    private void CreateItem(int itemNum, int num = 1)
    {
        if (items[itemNum].itemController == null)
            return;

        string itemType = items[itemNum].itemController.GetType().ToString();
        for (int i = 0; i < num; ++i)
        {
            GameObject item = Instantiate(items[itemNum].itemController.gameObject, transform);
            ItemController controller = (ItemController)item.GetComponent(itemType);
            controller.ItemNum = itemNum;
            controller.Index = i++;
            item.name = "item";
            item.SetActive(false);
            objPools[itemNum].AddFirst(item);
            controllerDict.Add(controller.Index, controller);
        }
        
    }

    // 무작위 아이템 번호
    public int RandomItemNum()
    {
        int random = Random.Range(0, chanceSum);
        int sum = 0;

        for (int i = 0; i < items.Length; ++i)
        {
            if (random >= sum && random < sum + items[i].chance)
                return i;
            sum += items[i].chance;
        }
        return 0;
    }

    // 아이템 생성
    public void GenerateItem(int itemNum, Vector3 position)
    {
        if (items[itemNum].itemController == null)
            return;

        if (objPools[itemNum].Count == 0)
            CreateItem(itemNum);

        // pop
        GameObject item = objPools[itemNum].First.Value;
        objPools[itemNum].RemoveFirst();

        // 아이템 배치
        item.transform.position = position;
        item.SetActive(true);
        item.GetComponent<ItemController>().StartCoroutine("Jump");
    }

    public void GenerateItem(Vector3 position)
    {
        GenerateItem(RandomItemNum(), position);
    }

    // 아이템 파괴
    public void DestroyItem(int index)
    {
        ItemController controller = controllerDict[index];
        controller.Destroy();
        controller.gameObject.SetActive(false);
        objPools[controller.ItemNum].AddLast(controller.gameObject);
    }

    public ItemController GetItem(int index)
    {
        return controllerDict[index];
    }
}
