using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoomController : MonoBehaviour
{
    private const float timeForDestroy = 2f;

    void OnEnable()
    {
        StartCoroutine("Boom");
    }

    IEnumerator Boom()
    {
        float time = 0f;

        do {
            yield return null;
            time += Time.deltaTime;
        } while (time < timeForDestroy);

        BoomGenerator.Inst.DestroyBoom(gameObject);
    }
}
