using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

#pragma warning disable CS0649

public class DebugManager : MonoBehaviour
{
    [SerializeField] private Text text;

    private const float buttonHeight = 35f;
    private const int labelSize = 30;
    private List<string> labelList;
    private List<string> buttonNameList;
    private List<Action> actionList;

    public static DebugManager Inst { get; private set; }

    DebugManager()
    {
        if (Inst != null) Application.Quit();
        Inst = this;
    }

    void Awake()
    {
        labelList = new List<string>();
        buttonNameList = new List<string>();
        actionList = new List<Action>();

        AddButton("Button", () => {
            PlayerController player = RPCEvent.Inst.MyPlayerController;
            player.Power += 2f;
            player.BombCount += 2;
        });
    }

    public void AddLabel(string label)
    {
        labelList.Add(label);
    }

    public void AddButton(string buttonName, Action action)
    {
        buttonNameList.Add(buttonName);
        actionList.Add(action);
    }

    void OnGUI()
    {
        GUILayout.BeginHorizontal();
        for (int i = 0; i < buttonNameList.Count; ++i)
        {
            if (GUILayout.Button(buttonNameList[i], GUILayout.Width(Screen.width / buttonNameList.Count), GUILayout.Height(buttonHeight)))
                actionList[0]();
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginVertical();
        for (int i = 0; i < labelList.Count; ++i)
        {
            GUI.skin.label.fontSize = labelSize;
            GUI.color = Color.black;
            GUILayout.Label(labelList[i]);
            GUILayout.FlexibleSpace();
        }
        GUILayout.EndVertical();
    }
}
