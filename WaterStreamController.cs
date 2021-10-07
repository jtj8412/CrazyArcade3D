using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterStreamController : MonoBehaviour
{
    private const float requiredDestroyTime = 1f;       // 지속 시간    
    private const float requiredTransitionTime = 0.12f; // 물줄기가 퍼져나가나는 시간
    private const float startShortenTime = requiredDestroyTime - requiredTransitionTime; // 물줄기가 다시 줄어드는 시간

    IEnumerator Boom(object[] args)
    {
        Vector3 originPos = transform.position;     // 위치
        Vector3 desiredPos = (Vector3)args[0];      
        Vector3 desiredMoveVec = (Vector3)args[1];
        Vector3 desiredScale = (Vector3)args[2];
        Vector3 tmpScale = desiredScale;
        int[] index = (int[])args[3];

        float time = 0f;
        float ratio;

        do {
            yield return null;

            time += Time.deltaTime;
            ratio = time / requiredTransitionTime;

            transform.position = originPos + desiredMoveVec * ratio;
            tmpScale.z = desiredScale.z * ratio;
            transform.localScale = tmpScale;
        } while (time < requiredTransitionTime);

        transform.position = desiredPos;
        transform.localScale = desiredScale;

        for (int i = 0; i < 2; ++i)
        {
            if (index[i] != -1 && BlockGenerator.Inst.HasBlock(index[i]))
                BlockGenerator.Inst.DestroyBlock(index[i]);
        }

        do {
            yield return null;
            time += Time.deltaTime;
        } while (time < startShortenTime);

        do
        {
            yield return null;
            time += Time.deltaTime;
            ratio = (time - startShortenTime) / requiredTransitionTime;

            transform.position = desiredPos - desiredMoveVec * ratio;
            tmpScale.z = desiredScale.z * (1 - ratio);
            transform.localScale = tmpScale;
        } while (time < requiredDestroyTime);

        WaterStreamGenerator.Inst.DestroyWaterStream(gameObject);
    }

}
