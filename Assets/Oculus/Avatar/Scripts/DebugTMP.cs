using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DebugTMP : MonoBehaviour
{
    public TextMeshProUGUI debugText;

    public void Debug(string text) 
    {
        debugText.text = text;
    }
}
