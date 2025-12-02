using UnityEngine;
using System;

/// <summary>
/// Manages player energy system
/// - Starting energy: 100
/// - Depletes 1 energy per second
/// - Collision with objects modifies energy
/// - Game over when energy reaches 0
/// </summary>
public class EnergyManager : MonoBehaviour
{
    public static EnergyManager Instance { get; private set; }

    [Header("Energy Settings")]
    [Tooltip("Starting energy amount")]
    public float maxEnergy = 100f;

    [Tooltip("Energy depletion rate per second")]
    public float depletionRate = 1f;

    [Header("Current State")]
    [Tooltip("Current energy (for debugging)")]
    public float currentEnergy;

    [Header("Events")]
    public Action<float> OnEnergyChanged; // passes current energy
    public Action OnEnergyDepleted; // called when energy reaches 0

    [Header("Debug")]
    [Tooltip("Show energy debug logs")]
    public bool showDebugLogs = false;

    private bool isGameOver = false;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple EnergyManager instances detected! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Initialize energy
        currentEnergy = maxEnergy;

        if (showDebugLogs)
        {
            Debug.Log($"EnergyManager initialized with {currentEnergy} energy");
        }

        // Notify listeners
        OnEnergyChanged?.Invoke(currentEnergy);
    }

    void Update()
    {
        if (isGameOver)
            return;

        // Deplete energy over time
        if (currentEnergy > 0)
        {
            float energyLost = depletionRate * Time.deltaTime;
            ModifyEnergy(-energyLost);
        }
    }

    /// <summary>
    /// Modifies the current energy by a given amount (positive or negative)
    /// </summary>
    /// <param name="amount">Amount to add (positive) or subtract (negative)</param>
    public void ModifyEnergy(float amount)
    {
        if (isGameOver)
            return;

        float previousEnergy = currentEnergy;
        currentEnergy += amount;
        currentEnergy = Mathf.Clamp(currentEnergy, 0f, maxEnergy);

        if (showDebugLogs && Mathf.Abs(amount) > 0.1f)
        {
            Debug.Log($"Energy modified by {amount:F1}: {previousEnergy:F1} → {currentEnergy:F1}");
        }

        // Notify listeners
        OnEnergyChanged?.Invoke(currentEnergy);

        // Check for game over
        if (currentEnergy <= 0f && !isGameOver)
        {
            TriggerGameOver();
        }
    }

    /// <summary>
    /// Adds energy (for collectibles)
    /// </summary>
    public void AddEnergy(float amount)
    {
        if (amount > 0)
        {
            ModifyEnergy(amount);

            if (showDebugLogs)
            {
                Debug.Log($"<color=green>Energy gained: +{amount:F1}</color>");
            }
        }
    }

    /// <summary>
    /// Removes energy (for damage/obstacles)
    /// </summary>
    public void RemoveEnergy(float amount)
    {
        if (amount > 0)
        {
            ModifyEnergy(-amount);

            if (showDebugLogs)
            {
                Debug.Log($"<color=red>Energy lost: -{amount:F1}</color>");
            }
        }
    }

    /// <summary>
    /// Triggers game over due to energy depletion
    /// </summary>
    void TriggerGameOver()
    {
        isGameOver = true;

        Debug.Log("<color=red>GAME OVER: Energy depleted!</color>");

        // Notify listeners
        OnEnergyDepleted?.Invoke();
    }

    /// <summary>
    /// Resets energy to max (for game restart)
    /// </summary>
    public void ResetEnergy()
    {
        currentEnergy = maxEnergy;
        isGameOver = false;

        if (showDebugLogs)
        {
            Debug.Log("Energy reset to max");
        }

        OnEnergyChanged?.Invoke(currentEnergy);
    }

    /// <summary>
    /// Gets current energy as a percentage (0-1)
    /// </summary>
    public float GetEnergyPercentage()
    {
        return currentEnergy / maxEnergy;
    }

    /// <summary>
    /// Checks if player has enough energy
    /// </summary>
    public bool HasEnergy()
    {
        return currentEnergy > 0f;
    }
}
