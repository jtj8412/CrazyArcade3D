using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using System.Diagnostics;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    private readonly string gameVersion = "1";
    private const int maxPlayerNum = 4;
    private const int maxReconnectNum = 3;
    private const int maxRoomNum = 4;
   
    private GameObject home;
    private GameObject lobby;
    private GameObject anteRoom;
    private Text connectionInfoText;

    private Button joinLobbyButton;

    private Button createRoomButton;
    private InputField roomNameInputField; 
    private InputField nicknameInputField;
        
    private Button startGameButton;
    private Button exitRoomButton;


    [SerializeField] private Text inputText;
    [SerializeField] private Text chatText;

    [PunRPC]
    void RPCGoChat(string playerName, string message)
    {
        chatText.text += "[" + playerName + "] : " + message + "\n";
    }

    public void GoChat()
    {
        string playerName = PhotonNetwork.NickName;
        string message = inputText.text;
        inputText.text = "";
        photonView.RPC("RPCGoChat", RpcTarget.All, playerName, message);
    }

    void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = false;
        Transform canvas = GameObject.Find("Canvas").transform;
        // Panel
        home = canvas.Find("Home").gameObject;
        lobby = canvas.Find("Lobby").gameObject;
        anteRoom = canvas.Find("AnteRoom").gameObject;

        // ConnectionInfoText
        connectionInfoText = canvas.Find("ConnectionInfoText").GetComponent<Text>();

        // Home
        joinLobbyButton = home.transform.Find("JoinLobbyButton").GetComponent<Button>();

        joinLobbyButton.onClick.AddListener(JoinLobby);

        // Lobby
        roomNameInputField = lobby.transform.Find("RoomNameInputField").GetComponent<InputField>();
        nicknameInputField = lobby.transform.Find("NicknameInputField").GetComponent<InputField>();
        createRoomButton = lobby.transform.Find("CreateRoomButton").GetComponent<Button>();

        createRoomButton.onClick.AddListener(CreateRoom_);

        // AnteRoom
        startGameButton = anteRoom.transform.Find("StartGameButton").GetComponent<Button>();
        exitRoomButton = anteRoom.transform.Find("ExitRoomButton").GetComponent<Button>();
        startGameButton.interactable = false;

        startGameButton.onClick.AddListener(StartGame);
        exitRoomButton.onClick.AddListener(ExitRoom);
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Connect();
    }

    float lobbyTime = 0f;
    void Update()
    {
        if (PhotonNetwork.InLobby)
        {
            lobbyTime += Time.deltaTime;
            if (lobbyTime >= 3f)
            {
                lobbyTime = 0f;
                PhotonNetwork.LeaveLobby();
                PhotonNetwork.JoinLobby();
            }
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        print("OnDisconnected");
    }

    private void Connect()
    {
        print("[Connect Button] : Lobby");
        Message("마스터 서버에 접속 중");

        int reconnectCount = 0;

        PhotonNetwork.GameVersion = gameVersion;
        PhotonNetwork.ConnectUsingSettings();
        joinLobbyButton.interactable = false;

        while (!PhotonNetwork.IsConnected)
        {
            if (reconnectCount++ > maxReconnectNum)
                return;

            Message("오프라인 : 마스터 서버와 연결되지 않음");
            AddMessage("접속 재시도 중 ({0})", reconnectCount);

            PhotonNetwork.ConnectUsingSettings();
        }

        Message("로비 서버에 접속");
    }

    public override void OnConnectedToMaster()
    {
        print("OnConnectedToMaster");
        Message("온라인 : 마스터 서버와 연결됨");
        JoinLobby();//
        joinLobbyButton.interactable = true;
        startGameButton.interactable = true;
    }

    private void JoinLobby()
    {
        print("[JoinLobby Button] : Lobby");

        lobby.SetActive(true);
        home.SetActive(false);

        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        print("OnJoinedLobby");
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomInfoList)
    {
        print("OnRoomListUpdate");

        Transform roomListUI = lobby.transform.Find("RoomList");
        for (int i = 0; i < roomListUI.childCount; ++i)
        {
            Transform roomUI = roomListUI.GetChild(i);

            Text titleText = roomUI.Find("TitleText").GetComponent<Text>();
            Text masterNicknameText = roomUI.Find("MasterNicknameText").GetComponent<Text>();
            Button enterRoomButton = roomUI.GetComponent<Button>();

            if (i < roomInfoList.Count && roomInfoList[i].IsVisible)
            {
                RoomInfo roomInfo = roomInfoList[i];
                print(roomInfo.Name);
                titleText.text = roomInfo.Name;
                masterNicknameText.text = (string)roomInfo.CustomProperties["MasterNickname"];
                enterRoomButton.onClick.RemoveAllListeners();

                if (masterNicknameText.text == "")
                {
                    titleText.text = "";
                    enterRoomButton.onClick.AddListener(() => Message("빈 방"));
                }
                else
                {
                    enterRoomButton.onClick.AddListener(() => JoinRoom(roomInfo.Name));
                }
            }
            else
            {
                titleText.text = "";
                masterNicknameText.text = "";
                enterRoomButton.onClick.RemoveAllListeners();
                enterRoomButton.onClick.AddListener(() => Message("빈 방"));
            }
        }

    }

    private ExitGames.Client.Photon.Hashtable GetRoomPropertiesForLobby(string nickname)
    {
        return new ExitGames.Client.Photon.Hashtable() {
            { "MasterNickname", nickname }
        };
    }

    private void CreateRoom_()
    {
        print("[CreateRoom_ Button] : RoomList");

        if (PhotonNetwork.CountOfRooms >= maxRoomNum)
        {
            Message("방 개수 초과");
            return;
        }

        string roomName = roomNameInputField.text;
        string nickname = nicknameInputField.text;

        if (roomName == "")
        {
            Message("방 제목 입력");
            return;
        }

        if (nickname == "")
        {
            Message("닉네임 입력");
            return;
        }

        ExitGames.Client.Photon.Hashtable customRoomProperties = GetRoomPropertiesForLobby(nickname);
        string[] propertyKeys = new string[customRoomProperties.Keys.Count];
        int keyCount = 0;

        foreach (string key in customRoomProperties.Keys)
            propertyKeys[keyCount++] = key;

        RoomOptions roomOptions = new RoomOptions
        {
            IsOpen = true,
            IsVisible = true,
            MaxPlayers = maxPlayerNum,
            CustomRoomProperties = customRoomProperties,
            CustomRoomPropertiesForLobby = propertyKeys
        };
        PhotonNetwork.CreateRoom(roomName, roomOptions, TypedLobby.Default);
        PhotonNetwork.NickName = nickname;
        // Set RoomName
    }

    private void JoinRoom(string roomName)
    {
        print("[JoinRoom Button] : RoomList");

        string nickname = nicknameInputField.text;

        if (nickname == "")
        {
            Message("닉네임 입력");
            return;
        }

        PhotonNetwork.NickName = nickname;
        PhotonNetwork.JoinRoom(roomName);
        // Set RoomName
    }

    public override void OnJoinedRoom()
    {
        print("OnJoinedRoom");
        PhotonNetwork.AutomaticallySyncScene = true;

        UpdatePlayerListInRoom();
        anteRoom.SetActive(true);
        lobby.SetActive(false);
    }

    public override void OnLeftRoom()
    {
        print("OnLeftRoom");
        PhotonNetwork.AutomaticallySyncScene = false;
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        print("OnPlayerEnterRoom");

        UpdatePlayerListInRoom();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        print("OnPlayerLeftRoom");

        UpdatePlayerListInRoom();
    }

    private void UpdatePlayerListInRoom()
    {
        Transform playerListUI = anteRoom.transform.Find("PlayerList");
        Player[] playerList = PhotonNetwork.PlayerList;

        for (int i = 0; i < playerListUI.childCount; ++i)
        {
            Transform playerUI = playerListUI.GetChild(i);
            Text nicknameText = playerUI.Find("NicknameText").GetComponent<Text>();

            if (i < playerList.Length)
            {
                Player player = playerList[i];
                nicknameText.text = player.NickName;
            }
            else
            {
                nicknameText.text = "";
            }
        }
    }

    private void StartGame()
    {
        print("[StartGame Button] : AnteRoom");

        startGameButton.interactable = false;

        if (PhotonNetwork.IsMasterClient)
        {
            startGameButton.interactable = true;
            PhotonNetwork.LoadLevel("Main");
            PhotonNetwork.CurrentRoom.IsVisible = false;
        }

    }
    private void ExitRoom()
    {
        print("[ExitRoom Button] : AnteRoom");

        PhotonNetwork.LeaveRoom();
        anteRoom.SetActive(false);
        lobby.SetActive(true);
    }

    public void Message(string message, params object[] args)
    {
        connectionInfoText.text = string.Format(message, args);
    }

    public void AddMessage(string message, params object[] args)
    {
        connectionInfoText.text = "\n" + string.Format(message, args);
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        base.OnPlayerPropertiesUpdate(targetPlayer, changedProps);

        print("OnPlayerPropertiesUpdate");
    }
    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        base.OnRoomPropertiesUpdate(propertiesThatChanged);

        print("OnRoomPropertiesUpdate");
    }
}