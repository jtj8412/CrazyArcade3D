using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

#pragma warning disable CS0649  // ignore never assigned    

public class BlockGenerator : MonoBehaviour
{
    [SerializeField] private Material[] woodBoxMaterials;   // 블록 머티리얼 배열
    [SerializeField] private GameObject spaceBlockPrefab;   // 접근 불가 공간 ( 블록 이동 중 해당 위치에 접근을 막도록 설치 )
    [SerializeField] private LayerMask ignoreLayerMask;     // 블록 이동시 무시할 레이어

    public LayerMask IgnoreLayerMask { get { return ignoreLayerMask; } }

    private Dictionary<int, BlockController> blockDict;     // 블록 고유의 Index 값에 따라 인스턴스 저장
    private LinkedList<GameObject> spaceBlockPool;          // 접근 불가 공간 오브젝트 풀

    public static BlockGenerator Inst { get; private set; } // 싱글톤

    BlockGenerator()
    {
        // 싱글톤 초기화
        if (Inst != null) Application.Quit();
        Inst = this;
    }

    void Awake()
    {
        // 필드 초기화
        blockDict = new Dictionary<int, BlockController>();
        spaceBlockPool = new LinkedList<GameObject>();

        // 블록 인스턴스에 Index 값 부여
        BlockController[] controllers = GetComponentsInChildren<BlockController>();
        for (int i = 0; i < controllers.Length; ++i)
        {
            controllers[i].Index = i;
            blockDict.Add(i, controllers[i]);
        }

        // 접근 불가 공간 오브젝트 풀 초기화
        for (int i = 0; i < 8; ++i)
        {
            CreateSpaceBlock();
        }
    }
  
    // 접근 불가 공간 배치
    public void GenerateSpaceBlock(int index, Vector3 moveDir)
    {
        GameObject spaceBlock;
        Vector3 generatePos;

        // pop
        spaceBlock = spaceBlockPool.First.Value;
        spaceBlockPool.RemoveFirst();

        // 블록이 밀리는 방향에 접근 불가 공간 배치
        generatePos = blockDict[index].transform.position + moveDir * BlockController.moveDistLimit;
        generatePos.Set(Mathf.Round(generatePos.x), Mathf.Round(generatePos.y), Mathf.Round(generatePos.z));
        spaceBlock.transform.position = generatePos;
        spaceBlock.SetActive(true);

        // 움직일 블록의 collider와 접근 불가 공간의 collider의 충돌을 서로 무시
        Physics.IgnoreCollision(spaceBlock.GetComponent<BoxCollider>(), blockDict[index].BoxCollider, true);
        blockDict[index].SpaceBlock = spaceBlock;
    }
    
    // 접근 불가 공간 제거
    public void DestroySpaceBlock(int index, GameObject spaceBlock)
    {
        // 앞서 배치할 때 설정했던 충돌 무시를 해제
        Physics.IgnoreCollision(spaceBlock.GetComponent<BoxCollider>(), blockDict[index].BoxCollider, false);
        // push
        spaceBlockPool.AddLast(spaceBlock);
        spaceBlock.SetActive(false);
    }

    // 마스터 클라이언트일 경우, 블록 초기화 관리
    public void MasterClientInit()
    {
        foreach (int index in blockDict.Keys)
        {
            RPCEvent.Inst.BlockMaterial(index, Random.Range(0, woodBoxMaterials.Length));
            RPCEvent.Inst.BlockItemSet(index, ItemGenerator.Inst.RandomItemNum());
        }
    }

    public BlockController GetBlock(int index)
    {
        return blockDict[index];
    }

    public bool HasBlock(int index)
    {
        return blockDict.ContainsKey(index);
    }

    public void SetBlockMaterial(int index, int materialNum)
    {
        blockDict[index].GetComponentInChildren<MeshRenderer>().material = woodBoxMaterials[materialNum];
    }
    public void DestroyBlock(int index)
    {
        blockDict[index].Destroy();
        blockDict.Remove(index);
    }

    public void MoveBlock(int index, Vector3 moveVec)
    {
        blockDict[index].Move(moveVec);
    }

    public void SetBlockItem(int index, int itemNum)
    {
        blockDict[index].ItemNum = itemNum;
    }

    // 접근 불가 공간 인스턴스 생성 ( 오브젝트 풀에 저장 )
    private void CreateSpaceBlock()
    {
        GameObject spaceBlock = Instantiate(spaceBlockPrefab, transform);
        spaceBlock.name = "SpaceBlock";
        spaceBlock.SetActive(false);
        spaceBlockPool.AddLast(spaceBlock);
    }
}
