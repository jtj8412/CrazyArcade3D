using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterStreamController : MonoBehaviour
{
    private const float destroyTime = 1f;       // 물줄기 지속 시간    
    private const float transitionTime = 0.12f; // 물줄기가 퍼져나가나는 시간
    private const float startShortenTime = destroyTime - transitionTime; // 물줄기가 다시 줄어드는 시간

    IEnumerator Boom(object[] args)
    {
        Vector3 originPos = transform.position;     // 물줄기 근원지 ( 물폭탄 위치 )
        Vector3 expandedPos = (Vector3)args[0];     // 물줄기 팽창시 최종 위치
        Vector3 toExpandedVec = (Vector3)args[1];   // 물줄기 이동 벡터
        Vector3 expandedScale = (Vector3)args[2];   // 물줄기
        Vector3 tmpScale = expandedScale;
        int[] index = (int[])args[3];               // 물줄기에 닿은 블록의 인덱스
        float time = 0f;

        // 물줄기 팽창
        do {
            yield return null;

            time += Time.deltaTime;
            float progress = Mathf.Clamp01(time / transitionTime);

            transform.position = originPos + toExpandedVec * progress;
            tmpScale.z = expandedScale.z * progress;
            transform.localScale = tmpScale;
        } while (time < transitionTime);

        // 물줄기에 닿은 블록 파괴
        for (int i = 0; i < 2; ++i)
        {
            if (index[i] != -1 && BlockGenerator.Inst.HasBlock(index[i]))
                BlockGenerator.Inst.DestroyBlock(index[i]);
        }

        // 물줄기 수축까지 대기
        do {
            yield return null;
            time += Time.deltaTime;
        } while (time < startShortenTime);

        // 물줄기 수축
        do
        {
            yield return null;

            time += Time.deltaTime;
            float progress = Mathf.Clamp01((time - startShortenTime) / transitionTime);

            transform.position = expandedPos - toExpandedVec * progress;
            tmpScale.z = expandedScale.z * (1 - progress);
            transform.localScale = tmpScale;
        } while (time < destroyTime);

        WaterStreamGenerator.Inst.DestroyWaterStream(gameObject);
    }

}
