using UnityEngine;
using UnityEngine.UI;

public class TextureChange : MonoBehaviour
{
    [SerializeField] Button rainbow;
    [SerializeField] Button wood;
    [SerializeField] Button wall;
    [SerializeField] Button grass;
    [SerializeField] Button none;

    [SerializeField] Texture2D rainbowTexture;
    [SerializeField] Texture2D woodTexture;
    [SerializeField] Texture2D wallTexture;
    [SerializeField] Texture2D grassTexture;

    [SerializeField] Image colorImage;
    [SerializeField] Pen pen; // Reference to the Pen script

    private void Start()
    {
        rainbow.onClick.AddListener(() => OnTextureSelected(rainbow.GetComponent<Image>().sprite,rainbowTexture, new Vector2(1, 1)));
        wood.onClick.AddListener(() => OnTextureSelected(wood.GetComponent<Image>().sprite, woodTexture, new Vector2(1, 1)));
        wall.onClick.AddListener(() => OnTextureSelected(wall.GetComponent<Image>().sprite, wallTexture, new Vector2(1, 1)));
        grass.onClick.AddListener(() => OnTextureSelected(grass.GetComponent<Image>().sprite,grassTexture, new Vector2(1, 1)));
        none.onClick.AddListener(() => OnNoneSelected());
    }

    private void OnTextureSelected(Sprite sprite,Texture2D texture, Vector2 tiling)
    {
        colorImage.sprite = sprite;
        pen.ApplyTexture(texture, tiling);
    }

    private void OnNoneSelected()
    {
        colorImage.sprite = null;
        pen.ResetPen();
    }
}
