using UnityEngine;
using UnityEngine.UI;

// Pastille « nouveau contenu » au coin haut-droit des boutons d'explanation opt-in
// (DEC-057). Le bouton disparait une fois clique (AdviceExplanationUIBase) : la
// pastille, enfant du bouton, signale donc toujours une explanation pas encore lue,
// differente de celle qu'on vient eventuellement de fermer.
// Creee par code pour couvrir les trois boutons sans toucher aux prefabs partages.
public static class NewContentBadge
{
    const string BADGE_NAME = "NewContentBadge";
    const float SIZE_RATIO = 0.4f;
    const float MIN_SIZE = 14f;
    const float MAX_SIZE = 40f;
    // Les sprites des boutons du kit Sci-Fi ont une marge transparente (~13 % de la
    // hauteur, ex. 17 px sur 134 pour Btn03Cyan) : on centre la pastille sur le coin
    // du cadre visible, pour qu'elle soit a cheval dessus, pas sur le coin du rect.
    const float FRAME_INSET_RATIO = 0.13f;
    static readonly Color BADGE_COLOR = new(1f, 0.33f, 0.2f);

    static Sprite _circleSprite;

    public static void AttachTo(RectTransform button)
    {
        if (button == null || button.Find(BADGE_NAME) != null)
            return;

        var badge = new GameObject(BADGE_NAME, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        var rect = badge.GetComponent<RectTransform>();
        rect.SetParent(button, false);
        rect.anchorMin = rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        float size = Mathf.Clamp(button.rect.height * SIZE_RATIO, MIN_SIZE, MAX_SIZE);
        float inset = button.rect.height * FRAME_INSET_RATIO;
        rect.anchoredPosition = new Vector2(-inset, -inset);
        rect.sizeDelta = new Vector2(size, size);

        badge.GetComponent<LayoutElement>().ignoreLayout = true;

        var image = badge.GetComponent<Image>();
        image.sprite = GetCircleSprite();
        image.color = BADGE_COLOR;
        image.raycastTarget = false;
    }

    // Disque anti-aliase genere une fois : pas de dependance a un asset.
    static Sprite GetCircleSprite()
    {
        if (_circleSprite != null)
            return _circleSprite;

        const int resolution = 64;
        var texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        float radius = resolution / 2f;
        var pixels = new Color32[resolution * resolution];
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radius, radius));
                byte alpha = (byte)(Mathf.Clamp01(radius - distance) * 255f);
                pixels[y * resolution + x] = new Color32(255, 255, 255, alpha);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        _circleSprite = Sprite.Create(texture, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), 100f);
        return _circleSprite;
    }
}
