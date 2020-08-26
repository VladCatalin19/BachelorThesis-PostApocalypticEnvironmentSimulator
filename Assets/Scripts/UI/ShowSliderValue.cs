using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShowSliderValue : MonoBehaviour
{
    TextMeshProUGUI numberText;
    // Start is called before the first frame update
    void Start()
    {
        numberText = GetComponent<TextMeshProUGUI>();
    }

    // Update is called once per frame
    public void TextUpdate(int value)
    {
        numberText.text = value.ToString();
    }
}
