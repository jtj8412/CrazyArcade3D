using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#pragma warning disable IDE0044

// 패시브 아이템 ( 능력치 증가 )
public class PassiveItemController : ItemController
{
    [SerializeField] private float power = 0;       // 설치한 물폭탄의 물줄기 크기
    [SerializeField] private float moveSpeed = 0;   // 이동 속도
    [SerializeField] private int bombCount = 0;     // 설치 가능 물폭탄 갯수

    public override void RPCGainItem(int viewID)
    {
        PlayerController playerController = RPCEvent.Inst.PlayerControllerDict[viewID];
        playerController.Power += power;
        playerController.MoveSpeed += moveSpeed;
        playerController.BombCount += bombCount;
        RPCEvent.Inst.ItemDestroy(Index);
    }
}
