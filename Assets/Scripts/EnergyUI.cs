using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays player energy on screen
/// Supports both Unity UI Text and TextMeshPro
/// Optional energy bar (Image/Slider)
/// </summary>
public class EnergyUI : MonoBehaviour
{
    [Header("Text Display")]
    [Tooltip("TextMeshPro component for energy display (preferred)")]
    public TextMeshProUGUI energyTextTMP;

    [Tooltip("Legacy UI Text component (fallback)")]
    public Text energyTextLegacy;

    [Tooltip("Text format - use {0} for current energy, {1} for max energy")]
    public string textFormat = "Energy: {0:F0}";

    [Header("Energy Bar (Optional)")]
    [Tooltip("Image to use as energy bar fill")]
    public Image energyBarFill;

    [Tooltip("Slider to use as energy bar")]
    public Slider energyBarSlider;

    [Header("Color Settings")]
    [Tooltip("Color when energy is high (> 66%)")]
    public Color highEnergyColor = Color.green;

    [Tooltip("Color when energy is medium (33-66%)")]
    public Color mediumEnergyColor = Color.yellow;

    [Tooltip("Color when energy is low (< 33%)")]
    public Color lowEnergyColor = Color.red;

    [Tooltip("Apply color to text")]
    public bool colorizeText = true;

    [Tooltip("Apply color to bar fill")]
    public bool colorizeBar = true;

    [Header("Animation")]
    [Tooltip("Smooth energy bar transition")]
    public bool smoothBarTransition = true;

    [Tooltip("Bar transition speed")]
    public float barTransitionSpeed = 5f;

    private float targetFillAmount = 1f;
    private float currentFillAmount = 1f;

    void Start()
    {
        // Subscribe to energy changes
        if (EnergyManager.Instance != null)
        {
            EnergyManager.Instance.OnEnergyChanged += UpdateEnergyDisplay;

            // Initialize display with current energy
            UpdateEnergyDisplay(EnergyManager.Instance.currentEnergy);
        }
        else
        {
            Debug.LogError("EnergyUI: EnergyManager not found in scene!");
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (EnergyManager.Instance != null)
        {
            EnergyManager.Instance.OnEnergyChanged -= UpdateEnergyDisplay;
        }
    }

    void Update()
    {
        // Smooth bar transition
        if (smoothBarTransition && energyBarFill != null)
        {
            currentFillAmount = Mathf.Lerp(currentFillAmount, targetFillAmount, Time.deltaTime * barTransitionSpeed);
            energyBarFill.fillAmount = currentFillAmount;
        }
    }

    /// <summary>
    /// Updates the energy display with new value
    /// </summary>
    void UpdateEnergyDisplay(float currentEnergy)
    {
        if (EnergyManager.Instance == null)
            return;

        float maxEnergy = EnergyManager.Instance.maxEnergy;
        float percentage = currentEnergy / maxEnergy;

        // Update text
        UpdateText(currentEnergy, maxEnergy);

        // Update bar
        UpdateBar(percentage);

        // Update colors
        UpdateColors(percentage);
    }

    /// <summary>
    /// Updates the text display
    /// </summary>
    void UpdateText(float currentEnergy, float maxEnergy)
    {
        string displayText = string.Format(textFormat, currentEnergy, maxEnergy);

        // Update TextMeshPro (preferred)
        if (energyTextTMP != null)
        {
            energyTextTMP.text = displayText;
        }
        // Fallback to legacy UI Text
        else if (energyTextLegacy != null)
        {
            energyTextLegacy.text = displayText;
        }
    }

    /// <summary>
    /// Updates the energy bar fill amount
    /// </summary>
    void UpdateBar(float percentage)
    {
        targetFillAmount = Mathf.Clamp01(percentage);

        // Update Image fill
        if (energyBarFill != null)
        {
            if (!smoothBarTransition)
            {
                energyBarFill.fillAmount = targetFillAmount;
                currentFillAmount = targetFillAmount;
            }
        }

        // Update Slider
        if (energyBarSlider != null)
        {
            energyBarSlider.value = percentage;
        }
    }

    /// <summary>
    /// Updates colors based on energy percentage
    /// </summary>
    void UpdateColors(float percentage)
    {
        Color targetColor = GetColorForPercentage(percentage);

        // Colorize text
        if (colorizeText)
        {
            if (energyTextTMP != null)
            {
                energyTextTMP.color = targetColor;
            }
            else if (energyTextLegacy != null)
            {
                energyTextLegacy.color = targetColor;
            }
        }

        // Colorize bar
        if (colorizeBar && energyBarFill != null)
        {
            energyBarFill.color = targetColor;
        }
    }

    /// <summary>
    /// Gets the color for a given energy percentage
    /// </summary>
    Color GetColorForPercentage(float percentage)
    {
        if (percentage > 0.66f)
        {
            return highEnergyColor;
        }
        else if (percentage > 0.33f)
        {
            // Lerp between high and medium
            float t = (percentage - 0.33f) / 0.33f;
            return Color.Lerp(mediumEnergyColor, highEnergyColor, t);
        }
        else
        {
            // Lerp between low and medium
            float t = percentage / 0.33f;
            return Color.Lerp(lowEnergyColor, mediumEnergyColor, t);
        }
    }

    /// <summary>
    /// Manually set text format at runtime
    /// </summary>
    public void SetTextFormat(string format)
    {
        textFormat = format;
        if (EnergyManager.Instance != null)
        {
            UpdateEnergyDisplay(EnergyManager.Instance.currentEnergy);
        }
    }
}
