using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Function : MonoBehaviour
{
    public static Function Inst { get; private set; }   // 싱글톤

    Function()
    {
        if (Inst != null) Application.Quit();
        Inst = this;
    }
    
    public bool CompareLayer(int layer, int layer_bit)
    {
        return (layer_bit & 1 << layer) != 0;
    }
}
