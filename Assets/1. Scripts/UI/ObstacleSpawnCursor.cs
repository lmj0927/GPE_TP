using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Replaces the OS cursor with the active spawn-mode skill icon via <see cref="Cursor.SetCursor"/>.
/// </summary>
public sealed class ObstacleSpawnCursor : MonoBehaviour
{
    [SerializeField] private PlayerObstacleInput _obstacleInput;
    [SerializeField] private Sprite _fallerSprite;
    [SerializeField] private Sprite _bouncerSprite;
    [SerializeField] private Sprite _rollerSprite;
    [SerializeField, Range(0f, 1f)] private float _alpha = 0.7f;
    [SerializeField, Min(16)] private int _maxCursorSize = 64;
    [SerializeField] private Vector2 _hotspotNormalized = new(0.5f, 0.5f);

    private readonly Dictionary<ObstacleKind, Texture2D> _cursorTextures = new();
    private readonly Dictionary<ObstacleKind, Vector2> _cursorHotspots = new();
    private bool _wasBlockedByStory;

    private void Awake()
    {
        if (_obstacleInput == null)
            _obstacleInput = FindFirstObjectByType<PlayerObstacleInput>();

        BuildCursorTextures();
    }

    private void OnEnable()
    {
        if (_obstacleInput == null)
            return;

        _obstacleInput.ObstacleKindChanged += ApplyCursor;
    }

    private void Start()
    {
        if (_obstacleInput != null && !StoryUI.IsShowing)
            ApplyCursor(_obstacleInput.CurrentObstacleKind);
    }

    private void LateUpdate()
    {
        if (StoryUI.IsShowing)
        {
            ResetCursor();
            _wasBlockedByStory = true;
            return;
        }

        if (!_wasBlockedByStory || _obstacleInput == null)
            return;

        _wasBlockedByStory = false;
        ApplyCursor(_obstacleInput.CurrentObstacleKind);
    }

    private void OnDisable()
    {
        if (_obstacleInput != null)
            _obstacleInput.ObstacleKindChanged -= ApplyCursor;

        ResetCursor();
    }

    private void OnDestroy()
    {
        foreach (var texture in _cursorTextures.Values)
        {
            if (texture != null)
                Destroy(texture);
        }

        _cursorTextures.Clear();
        _cursorHotspots.Clear();
    }

    private void BuildCursorTextures()
    {
        TryBuildCursor(ObstacleKind.Faller, _fallerSprite);
        TryBuildCursor(ObstacleKind.Bouncer, _bouncerSprite);
        TryBuildCursor(ObstacleKind.Roller, _rollerSprite);
    }

    private void TryBuildCursor(ObstacleKind kind, Sprite sprite)
    {
        if (sprite == null)
            return;

        if (!CursorTextureBuilder.TryCreate(sprite, _alpha, _maxCursorSize, _hotspotNormalized, out Texture2D texture, out Vector2 hotspot))
        {
            Debug.LogWarning($"[ObstacleSpawnCursor] Failed to build cursor texture for {kind}.");
            return;
        }

        _cursorTextures[kind] = texture;
        _cursorHotspots[kind] = hotspot;
    }

    private void ApplyCursor(ObstacleKind kind)
    {
        if (StoryUI.IsShowing)
        {
            ResetCursor();
            return;
        }

        if (!_cursorTextures.TryGetValue(kind, out Texture2D texture) ||
            !_cursorHotspots.TryGetValue(kind, out Vector2 hotspot))
        {
            ResetCursor();
            return;
        }

        Cursor.SetCursor(texture, hotspot, CursorMode.Auto);
    }

    private static void ResetCursor()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    private static class CursorTextureBuilder
    {
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        public static bool TryCreate(
            Sprite sprite,
            float alpha,
            int maxCursorSize,
            Vector2 hotspotNormalized,
            out Texture2D cursorTexture,
            out Vector2 hotspot)
        {
            cursorTexture = null;
            hotspot = Vector2.zero;

            if (sprite == null || sprite.texture == null)
                return false;

            int sourceWidth = Mathf.Max(1, (int)sprite.textureRect.width);
            int sourceHeight = Mathf.Max(1, (int)sprite.textureRect.height);

            Texture2D baked = BakeSpriteTexture(sprite, alpha);
            if (baked == null)
                return false;

            Texture2D scaled = ScaleDown(baked, maxCursorSize);
            if (scaled != baked)
                Destroy(baked);

            float uniformScale = (float)scaled.width / sourceWidth;
            hotspot = new Vector2(
                sourceWidth * hotspotNormalized.x * uniformScale,
                sourceHeight * hotspotNormalized.y * uniformScale);

            cursorTexture = scaled;
            return true;
        }

        private static Texture2D BakeSpriteTexture(Sprite sprite, float alpha)
        {
            Rect rect = sprite.textureRect;
            int width = Mathf.Max(1, (int)rect.width);
            int height = Mathf.Max(1, (int)rect.height);

            if (sprite.texture.isReadable)
                return CopyReadablePixels(sprite, alpha);

            return RenderSpriteRegion(sprite, alpha, width, height);
        }

        private static Texture2D CopyReadablePixels(Sprite sprite, float alpha)
        {
            Rect rect = sprite.textureRect;
            int x = (int)rect.x;
            int y = (int)rect.y;
            int width = (int)rect.width;
            int height = (int)rect.height;

            Color[] pixels = sprite.texture.GetPixels(x, y, width, height);
            ApplyAlpha(pixels, alpha);

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D RenderSpriteRegion(Sprite sprite, float alpha, int width, int height)
        {
            Shader shader = Shader.Find("UI/Default")
                ?? Shader.Find("Sprites/Default")
                ?? Shader.Find("Unlit/Transparent");

            if (shader == null)
                return null;

            var material = new Material(shader);
            Rect rect = sprite.textureRect;
            float texWidth = sprite.texture.width;
            float texHeight = sprite.texture.height;

            material.SetTexture(MainTexId, sprite.texture);
            material.SetColor(ColorId, new Color(1f, 1f, 1f, alpha));
            material.mainTextureScale = new Vector2(rect.width / texWidth, rect.height / texHeight);
            material.mainTextureOffset = new Vector2(rect.x / texWidth, rect.y / texHeight);

            var renderTarget = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active;

            Graphics.Blit(sprite.texture, renderTarget, material);
            RenderTexture.active = renderTarget;

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            texture.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTarget);
            Destroy(material);

            return texture;
        }

        private static Texture2D ScaleDown(Texture2D source, int maxCursorSize)
        {
            int width = source.width;
            int height = source.height;
            int longestSide = Mathf.Max(width, height);

            if (longestSide <= maxCursorSize)
                return source;

            float scale = maxCursorSize / (float)longestSide;
            int targetWidth = Mathf.Max(1, Mathf.RoundToInt(width * scale));
            int targetHeight = Mathf.Max(1, Mathf.RoundToInt(height * scale));

            var renderTarget = RenderTexture.GetTemporary(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active;

            Graphics.Blit(source, renderTarget);

            RenderTexture.active = renderTarget;
            var scaled = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
            scaled.ReadPixels(new Rect(0f, 0f, targetWidth, targetHeight), 0, 0);
            scaled.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTarget);
            return scaled;
        }

        private static void ApplyAlpha(Color[] pixels, float alpha)
        {
            for (int i = 0; i < pixels.Length; i++)
                pixels[i].a *= alpha;
        }
    }
}
