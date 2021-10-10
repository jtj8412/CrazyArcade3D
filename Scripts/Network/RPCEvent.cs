using Photon.Pun;
using Photon.Pun.Demo.Procedural;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class RPCEvent : MonoBehaviourPunCallbacks
{
    public Dictionary<int, Player> PlayerDict { get; private set; }
    public Dictionary<int, Player> PlayerDictByActorNumber { get; private set; }
    public Dictionary<int, PlayerController> PlayerControllerDict { get; private set; }
    public Dictionary<int, PlayerController> PlayerControllerDictByActorNumber { get; private set; }
    public PlayerController MyPlayerController { get { return PlayerControllerDict[ViewID]; } }
    public bool IsMasterClient { get { return PhotonNetwork.IsMasterClient; } }
    public int ViewID { private set; get; }
    public int ActorNumber { get { return PhotonNetwork.LocalPlayer.ActorNumber; } }
    public int PlayerCount { get { return PlayerDict.Count; } }

    public static RPCEvent Inst { get; private set; }

    RPCEvent()
    {
        if (Inst != null) Application.Quit();
        Inst = this;
    }

    void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = false;

        PlayerDictByActorNumber = PhotonNetwork.CurrentRoom.Players;
        PlayerDict = new Dictionary<int, Player>();
        PlayerControllerDict = new Dictionary<int, PlayerController>();
        PlayerControllerDictByActorNumber = new Dictionary<int, PlayerController>();
    }

    void Start()
    {
        GameObject cam = Camera.main.gameObject;
        GameObject player = null;
        if (ActorNumber == 1)
        {
            player = PhotonNetwork.Instantiate("Player_Red", new Vector3(-2, 0, -4), Quaternion.identity);
        }
        else if (ActorNumber == 2)
        {
            player = PhotonNetwork.Instantiate("Player_Blue", new Vector3(8, 0, -4), Quaternion.identity);
        }
        else if (ActorNumber == 3)
        {
            player = PhotonNetwork.Instantiate("Player_Green", new Vector3(8, 0, 10), Quaternion.Euler(0f, 180f, 0f));
        }
        else
        {
            player = PhotonNetwork.Instantiate("Player_Yellow", new Vector3(-2, 0, 10), Quaternion.Euler(0f, 180f, 0f));
        }

        cam.transform.parent = player.transform;
        cam.transform.rotation = player.transform.rotation;

        if (PhotonNetwork.IsMasterClient)
            BlockGenerator.Inst.MasterClientInit();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        RemovePlayer(otherPlayer.ActorNumber);
    }

    public void AddPlayer(int viewID, int actorNumber)
    {
        ViewID = viewID;
        photonView.RPC("RPCAddPlayer", RpcTarget.All, viewID, actorNumber);
    }

    private void RemovePlayer(int actorNumber)
    {
        photonView.RPC("RPCRemovePlayer", RpcTarget.All, actorNumber);
    }

    private void TeleportPlayer(int viewID, Vector3 playerPos, Quaternion playerRot)
    {
        photonView.RPC("RPCTeleportPlayer", RpcTarget.All, viewID, playerPos, playerRot);
    }

    public void GainItem(int viewID, int itemIndex)
    {
        photonView.RPC("RPCGainItem", RpcTarget.All, viewID, itemIndex);
    }

    public void HitPunch(int viewID, Quaternion hitDir)
    {
        photonView.RPC("RPCHitPunch", RpcTarget.All, viewID, hitDir);
    }

    public void BlockDestory(int index)
    {
        photonView.RPC("RPCBlockDestory", RpcTarget.All, index);
    }

    public void BlockMove(int index, Vector3 moveDir)
    {
        photonView.RPC("RPCBlockMove", RpcTarget.All, index, moveDir);
    }

    public void BlockMaterial(int index, int materialNum)
    {
        photonView.RPC("RPCBlockMaterial", RpcTarget.All, index, materialNum);
    }

    public void BombGenerate(int viewID, Vector3 putPosition)
    {
        photonView.RPC("RPCBombGenerate", RpcTarget.All, viewID, putPosition);
    }

    public void BlockItemSet(int index, int itemNum)
    {
        photonView.RPC("RPCBlockItemSet", RpcTarget.AllBuffered, index, itemNum);
    }

    public void ItemDestroy(int index)
    {
        photonView.RPC("RPCItemDestroy", RpcTarget.AllBuffered, index);
    }

    public void WaterStreamGenerate(Vector3 boomPosition, float power)
    {
        photonView.RPC("RPCWaterStreamGenerate", RpcTarget.AllBuffered, boomPosition, power);
    }

    public void PlayerDie(int viewID)
    {
        photonView.RPC("RPCPlayerDie", RpcTarget.AllBuffered, viewID);
    }

    [PunRPC]
    private void RPCPlayerDie(int viewID)
    {
        PlayerControllerDict[viewID].Die();
    }

    [PunRPC]
    private void RPCAddPlayer(int viewID, int actorNumber)
    {
        Transform playerObjList = GameObject.Find("PlayerList").transform;
        int playerNum = playerObjList.childCount;

        for (int i = 0; i < playerNum; ++i)
        {
            PlayerController playerController = playerObjList.GetChild(i).GetComponent<PlayerController>();

            if (playerController.ViewID == viewID)
            {
                PlayerControllerDict.Add(viewID, playerController);
                PlayerDict.Add(viewID, PlayerDictByActorNumber[actorNumber]);
                PlayerControllerDictByActorNumber.Add(actorNumber, playerController);
                PlayerNicknameUIManager.Inst.AddPlayer(viewID, PlayerDict[viewID].NickName);
                return;
            }
        }

        print("[ERROR] RPCInitPlayerID : PhotonInit");
    }

    public void PlayerInWaterPrison(int viewID)
    {
        photonView.RPC("RPCPlayerInWaterPrison", RpcTarget.AllBuffered, viewID);
    }

    [PunRPC]
    public void RPCPlayerInWaterPrison(int viewID)
    {
        PlayerControllerDict[viewID].HitWaterStream();
    }

    [PunRPC]
    private void RPCGainItem(int viewID, int itemIndex)
    {
        ItemController itemController = ItemGenerator.Inst.GetItem(itemIndex);
        itemController.RPCGainItem(viewID);
    }

    [PunRPC]
    private void RPCHitPunch(int viewID, Quaternion hitDir)
    {
        if (ViewID == viewID)
            PlayerControllerDict[viewID].StartCoroutine("HitPunch", hitDir);
    }

    [PunRPC]
    private void RPCBlockDestory(int index)
    {
        BlockGenerator.Inst.DestroyBlock(index);
    }

    [PunRPC]
    private void RPCTeleportPlayer(int viewID, Vector3 playerPos, Quaternion playerRot)
    {
        PlayerController playerController = PlayerControllerDict[viewID];
        playerController.transform.position = playerPos;
        playerController.transform.rotation = playerRot;
    }

    [PunRPC]
    private void RPCBlockMove(int index, Vector3 moveDir)
    {
        BlockGenerator.Inst.MoveBlock(index, moveDir);
        BlockGenerator.Inst.GenerateSpaceBlock(index, moveDir);
    }

    [PunRPC]
    private void RPCBlockMaterial(int index, int materialNum)
    {
        BlockGenerator.Inst.SetBlockMaterial(index, materialNum);
    }

    [PunRPC]
    private void RPCBombGenerate(int viewID, Vector3 putPosition)
    {
        BombGenerator.Inst.RPCGenerateBomb(PlayerControllerDict[viewID], putPosition);
    }

    [PunRPC]
    private void RPCBlockItemSet(int index, int itemNum)
    {
        BlockGenerator.Inst.SetBlockItem(index, itemNum);
    }

    [PunRPC]
    private void RPCItemDestroy(int index)
    {
        ItemGenerator.Inst.DestroyItem(index);
    }

    [PunRPC]
    private void RPCWaterStreamGenerate(Vector3 boomPosition, float power)
    {
        WaterStreamGenerator.Inst.GenerateWaterStream(boomPosition, power);
    }

    [PunRPC]
    private void RPCRemovePlayer(int actorNumber)
    {
        int viewID = PlayerControllerDictByActorNumber[actorNumber].ViewID;

        PlayerDict.Remove(viewID);
        PlayerControllerDict.Remove(viewID);
        PlayerDictByActorNumber.Remove(actorNumber);
        PlayerControllerDictByActorNumber.Remove(actorNumber);

        PlayerNicknameUIManager.Inst.RemovePlayer(viewID);
    }
}
