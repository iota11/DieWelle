using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TextManager : SingletonBehavior<TextManager>
{
    [SerializeField]
    private Text scoreText;                   // Text to show score
    [SerializeField]
    private Text rotationText;                // Text to show rotation achievements
    [SerializeField]
    private Text comboText;                   // Text to show current combo
    [SerializeField]
    private Text deathText;                   // Text to show on death
    [SerializeField]
    private Text jumpHeightText;              // Text to show current jump height
    [SerializeField]
    private Text livesText;                   // Text to show remaining lives

    private Dictionary<TextType, Text> textTypeDict = new Dictionary<TextType, Text>();

    protected override void Awake()
    {
        base.Awake(); // Call parent's Awake to set up singleton

        // Initialize dictionary in Awake to ensure it's ready before any Start() methods
        textTypeDict.Add(TextType.score, scoreText);
        textTypeDict.Add(TextType.rotation, rotationText);
        textTypeDict.Add(TextType.combo, comboText);
        textTypeDict.Add(TextType.death, deathText);
        textTypeDict.Add(TextType.jumpHeight, jumpHeightText);
        textTypeDict.Add(TextType.lives, livesText);
    }

    public void SetText(TextType type, string text)
    {
        if (!textTypeDict.ContainsKey(type))
        {
            Debug.LogError($"TextType {type} not found in dictionary. Make sure TextManager is initialized.");
            return;
        }

        Text textField = textTypeDict[type];

        if (textField != null)
        {
            textField.text = text;
        }
        else
        {
            Debug.LogError($"Text field of TextType {type} is null. Assign it in the Inspector.");
        }
    }

    public void SetTextFieldActive(TextType type, bool active)
    {
        if (!textTypeDict.ContainsKey(type))
        {
            Debug.LogError($"TextType {type} not found in dictionary. Make sure TextManager is initialized.");
            return;
        }

        Text textField = textTypeDict[type];

        if (textField != null)
        {
            textField.gameObject.SetActive(active);
        }
        else
        {
            Debug.LogError($"Text field of TextType {type} is null. Assign it in the Inspector.");
        }
    }
}

public enum TextType
{
    score,
    rotation,
    combo,
    death,
    jumpHeight,
    lives
}
