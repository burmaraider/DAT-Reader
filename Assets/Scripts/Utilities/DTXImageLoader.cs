using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class DTXImageLoader : MonoBehaviour
{
    private static readonly string SourceRootFolder = "c:\\LOMM\\Data\\";

    public string ImagePath;
    public bool UseOriginal;

    public void Start()
    {
        var image = GetComponent<Image>();

        var dtx = DTX.LoadDTX(SourceRootFolder, ImagePath);

        Sprite newSprite = Sprite.Create(UseOriginal ? dtx.OriginalTexture2D : dtx.Texture2D, new Rect(0, 0, dtx.Texture2D.width, dtx.Texture2D.width), new Vector2(0.5f, 0.5f));

        image.sprite = newSprite;
    }
}