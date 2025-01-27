using TMPro;
using UnityEngine;
using UnityEngine.UI; // For Slider and UI elements

public class BasketBallHoopControl : MonoBehaviour
{
    [Header("Movement Settings")]
    [Range(0, 3.5f)]
    public float distanceFromPlayer = 0f;

    public float heightMultiplier = 0.5f;
    public float baseHeight = 1f;

    [Header("References")]
    public Transform hoopTransform;
    public Slider distanceSlider;
    public TMP_Text distanceValueText;

    private Vector3 initialPosition;

    void Start()
    {
        initialPosition = hoopTransform.position;
        distanceFromPlayer = 0.5f;
        distanceSlider.minValue = 0f; 
        distanceSlider.maxValue = 3.5f; 
        distanceSlider.value = distanceFromPlayer;
        distanceSlider.onValueChanged.AddListener(UpdateDistanceFromSlider);

        UpdateDistanceText();
    }

    void Update()
    {
        float zPosition = initialPosition.z + distanceFromPlayer;

        float yPosition = initialPosition.y + heightMultiplier * distanceFromPlayer;

        hoopTransform.position = new Vector3(initialPosition.x, yPosition, zPosition);
    }

    public void UpdateDistanceFromSlider(float value)
    {
        distanceFromPlayer = value; 
        UpdateDistanceText();
    }

    private void UpdateDistanceText()
    {
        if (distanceValueText != null)
            distanceValueText.text = distanceFromPlayer.ToString();
    }
}
