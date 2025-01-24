using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuController : MonoBehaviour
{
    public GameObject menuCanvas;       // Reference to the Menu Canvas
    public GameObject colorPicker;     // Reference to the Color Picker Canvas
    public GameObject texturePicker;   // Reference to the Texture Picker Canvas

    private bool isMenuActive = false; // Track if the Menu Canvas is active

    void Start()
    {
        menuCanvas.SetActive(false);
        colorPicker.SetActive(false);
        texturePicker.SetActive(false);
    }

    void Update()
    {
        HandleMenuToggle();
      
    }

    private void HandleMenuToggle()
    {
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.LTouch))
        {
            isMenuActive = !isMenuActive;
            menuCanvas.SetActive(isMenuActive);

            if (!isMenuActive)
            {
                colorPicker.SetActive(false);
                texturePicker.SetActive(false);
            }
        }
    }

    public void HandleColorPickerToggle()
    {
        if (isMenuActive)
        {
            colorPicker.SetActive(!colorPicker.activeSelf);

            if (colorPicker.activeSelf)
            {
                texturePicker.SetActive(false);

            }
        }
    }

    public void HandleTextureColorPicker() 
    { 
        if (isMenuActive)
        {
            texturePicker.SetActive(!texturePicker.activeSelf);

            if (texturePicker.activeSelf)
            {
                colorPicker.SetActive(false);
            }
        }
    }
}
