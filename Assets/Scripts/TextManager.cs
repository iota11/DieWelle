using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TextManager : SingletonBehavior<TextManager>
{
    [SerializeField]
    private TextMeshProUGUI scoreText;                   // Text to show score
    [SerializeField]
    private TextMeshProUGUI rotationText;                // Text to show rotation achievements
    [SerializeField]
    private TextMeshProUGUI comboText;                   // Text to show current combo
    [SerializeField]
    private TextMeshProUGUI deathText;                   // Text to show on death
    [SerializeField]
    private TextMeshProUGUI jumpHeightText;              // Text to show current jump height
    [SerializeField]
    private TextMeshProUGUI livesText;                   // Text to show remaining lives

    private Dictionary<TextType, TextMeshProUGUI> textTypeDict = new Dictionary<TextType, TextMeshProUGUI>();

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

        TextMeshProUGUI textField = textTypeDict[type];

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

        TextMeshProUGUI textField = textTypeDict[type];

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
