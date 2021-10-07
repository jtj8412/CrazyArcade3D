using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon;

public class CameraController : MonoBehaviour
{
    private Transform camTf;    // 카메라 Transform
    private Transform targetTf; // 타겟 Transform

    private int targetIdx;      // 타겟 플레이어 인덱스 ( 관전 )

    private const float mouseHorizontalSensitivity = 1.5f;  // 마우스 민감도 Y  
    private const float mouseVerticalSensitivity = 1f;      // 마우스 민감도 X
    private float cameraVerticalRotation;                   // 카메라 Y축 회전
    private const float cameraVerticalRotationMax = 35f;    // 카메라 X축 회전 최댓값
    private const float cameraVerticalRotationMin = -35f;   // 카메라 X축 회전 최솟값
    private const float cameraZoomMax = 4.5f;               // 카메라 줌 최댓값
    private const float cameraZoomMin = 0.5f;               // 카메라 줌 최솟값
    private float maxTargetToCamDist = 1.2f;                // 타겟과의 거리
    private Vector3 camTargetPivot = new Vector3(0f, 0.4f, 0f);     // 타겟 위치 조정

    void Start()
    {
        // 필드 초기화
        camTf = Camera.main.transform;      
        targetTf = RPCEvent.Inst.MyPlayerController.transform;
        targetIdx = RPCEvent.Inst.ActorNumber;
        _ChangeTarget();
    }

    void Update()
    {
        RotateCamera();     // 카메라 회전
        ChangeTarget();     // 관전용, 카메라 플레이어 타겟 변경
    }

    void ChangeTarget()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        targetIdx += 1;
        if (targetIdx > RPCEvent.Inst.PlayerCount)
            targetIdx = 1;

        targetTf = RPCEvent.Inst.PlayerControllerDictByActorNumber[targetIdx].transform; // 타겟을 다른 플레이어로 변경
        _ChangeTarget();
    }

    // 변경된 타겟으로 카메라 위치 변경
    void _ChangeTarget()
    {
        camTf.parent.position = targetTf.position + camTargetPivot;
        camTf.localPosition = -targetTf.forward * 1f;
        camTf.parent.rotation = targetTf.rotation;
        camTf.rotation = Quaternion.Euler(targetTf.forward);
        cameraVerticalRotation = camTf.rotation.ToEuler().x;
        maxTargetToCamDist = 1f;
    }

    void RotateCamera()
    {
        float rotateHorizontal = Input.GetAxis("Mouse X") * mouseHorizontalSensitivity;
        float rotateVertical = -Input.GetAxis("Mouse Y") * mouseVerticalSensitivity;
        float scroll = -Input.GetAxis("Mouse ScrollWheel");

        maxTargetToCamDist = Mathf.Clamp(maxTargetToCamDist + scroll, cameraZoomMin, cameraZoomMax);

        if (cameraVerticalRotation + rotateVertical > cameraVerticalRotationMax)
            rotateVertical = cameraVerticalRotationMax - cameraVerticalRotation;
        else if (cameraVerticalRotation + rotateVertical < cameraVerticalRotationMin)
            rotateVertical = cameraVerticalRotationMin - cameraVerticalRotation;

        cameraVerticalRotation += rotateVertical;

        camTf.Rotate(rotateVertical, 0f, 0f);
        camTf.parent.Rotate(0f, rotateHorizontal, 0f);

        camTf.position = targetTf.position + (-camTf.forward * maxTargetToCamDist);
    }
}
