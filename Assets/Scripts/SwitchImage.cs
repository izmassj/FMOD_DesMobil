using UnityEngine;
using UnityEngine.UI;

public class SwitchImage : MonoBehaviour
{
    [SerializeField] Image image;
    [SerializeField] Sprite imageA;
    [SerializeField] Sprite imageB;

    public void UISwitchImage()
    {
        if (image.sprite == imageB)
        {
            image.sprite = imageA;
        }
        else
        {
            image.sprite = imageB;
        }
    }
}
