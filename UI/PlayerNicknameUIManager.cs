using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#pragma warning disable IDE0044
#pragma warning disable CS0649

public class PlayerNicknameUIManager : MonoBehaviour
{
    [SerializeField]
    private GameObject playerNicknameTextPrefab;

    private Canvas uiCanvas;                                    // UI 캔버스
    private Camera uiCamera;                                    // UI 캔버스 렌더링 카메라
    private RectTransform uiCanvasRect;                         // UI 캔버스 Rect
    private Dictionary<int, RectTransform> nicknameRectDict;    // 플레이어 고유 번호에 따라 닉네임 RectTransform 저장
    private Dictionary<int, Text> nicknameTextDict;             // 플레이어 고유 번호에 따라 닉네임 텍스트 저장
    private Vector3 offset = new Vector3(0f, 0.52f, 0f);        // 플레이어 기준 닉네임의 위치 ( 머리 위 )

    private const float maxFontSize = 30f;  // 폰트 사이즈
    
    public static PlayerNicknameUIManager Inst { get; private set; } // 싱글톤

    PlayerNicknameUIManager()
    {
        // 싱글톤 초기화
        if (Inst != null) Application.Quit();
        Inst = this;
    }

    void Start()
    {
        // 필드 초기화
        uiCanvas = GetComponent<Canvas>();
        uiCamera = uiCanvas.worldCamera;
        uiCanvasRect = GetComponent<RectTransform>();
        nicknameRectDict = new Dictionary<int, RectTransform>();
        nicknameTextDict = new Dictionary<int, Text>();
    }

    void LateUpdate()
    {
        foreach (int viewID in nicknameRectDict.Keys)
        {
            Vector3 targetPos = RPCEvent.Inst.PlayerControllerDict[viewID].transform.position;  // 플레이어 위치
            Vector3 screenPos = Camera.main.WorldToScreenPoint(targetPos + offset);             // 플레이어의 스크린상 위치
            if (screenPos.z < 0f)
                screenPos *= -1f;   // z < 0 일 경우 위치 반전
            RectTransformUtility.ScreenPointToLocalPointInRectangle(uiCanvasRect, screenPos, uiCamera, out Vector2 localPos);
            nicknameRectDict[viewID].localPosition = localPos;  // 닉네임 텍스트의 위치 조정

            float deg = (Camera.main.transform.position - targetPos).sqrMagnitude / 4f;
            int fontSize = (int)(maxFontSize - Mathf.Sin(deg * Mathf.Deg2Rad) * maxFontSize);   // 거리에 따른 폰트 사이즈

            if (deg >= 90 || fontSize == 0)
                fontSize = -1;

            nicknameTextDict[viewID].fontSize = fontSize;
        }
    }

    public void AddPlayer(int viewID, string nickname)
    {
        GameObject playerNickname = Instantiate(playerNicknameTextPrefab, transform);
        nicknameRectDict.Add(viewID, playerNickname.GetComponent<RectTransform>());
        nicknameTextDict.Add(viewID, playerNickname.GetComponent<Text>());
        nicknameTextDict[viewID].text = nickname;
    }

    public void RemovePlayer(int viewID)
    {
        Destroy(nicknameRectDict[viewID].gameObject);
        nicknameRectDict.Remove(viewID);
        nicknameTextDict.Remove(viewID);
    }
}
