using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System;
using System.Linq;

public class Minimap : MonoBehaviour
{
    [SerializeField] private RawImage minimapRawImage;    // Компонент RawImage для миникарты
    [SerializeField] private Camera mainCamera;           // Основная камера
    [SerializeField] private float revealRadius = 5f;     // Радиус открытия тумана
    [SerializeField] private float worldWidth = 100f;     // Ширина мира
    [SerializeField] private float worldHeight = 100f;    // Высота мира
    [SerializeField] private float viewScale = 1f;        // Масштаб видимой зоны
    [SerializeField] private float drawSizeMultiplier = 1f; // Множитель размера отрисовки
    [SerializeField] private List<LayerColor> layerColors = new List<LayerColor>(); // Список слоёв и цветов

    [Serializable]
    private struct LayerColor
    {
        public LayerMask layerMask;
        public Color32 color;
    }

    private Texture2D mapTexture;                         // Текстура карты
    private Texture2D overlayTexture;                     // Текстура для круга
    private Color32[] mapPixels;                          // Буфер пикселей для карты
    private Color32[] overlayPixels;                      // Буфер пикселей для оверлея
    private const int textureSize = 1024;                 // Размер текстур (1024x1024)
    private Vector2 mapCenter;                            // Центр карты
    private Rect uvRect;                                  // UV-прямоугольник для RawImage
    private float baseVisibleSize = 0.25f;                // Базовый размер видимой области
    private float updateTimer;                            // Таймер для обновления
    private const float updateInterval = 1f;            // 2 раза в секунду
    private CancellationTokenSource cts;                  // Для отмены задач

    void Start()
    {
        mapTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        overlayTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);

        mapPixels = new Color32[textureSize * textureSize];
        overlayPixels = new Color32[textureSize * textureSize];
        ClearMap();
        ClearOverlay();

        minimapRawImage.texture = mapTexture;

        mapCenter = new Vector2(0, 0);
        UpdateUVRect();
        minimapRawImage.uvRect = uvRect;

        mapTexture.SetPixels32(mapPixels);
        overlayTexture.SetPixels32(overlayPixels);
        mapTexture.Apply();
        overlayTexture.Apply();

        updateTimer = updateInterval;
        cts = new CancellationTokenSource();

        UpdateMinimapAsync().Forget();
    }

    void OnDestroy()
    {
        cts?.Cancel();
        cts?.Dispose();
    }

    private void ClearMap()
    {
        for (int i = 0; i < mapPixels.Length; i++)
        {
            mapPixels[i] = Color.black;
        }
    }

    private void ClearOverlay()
    {
        for (int i = 0; i < overlayPixels.Length; i++)
        {
            overlayPixels[i] = Color.clear;
        }
    }

    private async UniTaskVoid UpdateMinimapAsync()
    {
        while (!cts.IsCancellationRequested)
        {
            // Собираем данные в основном потоке
            Vector3 camPos = mainCamera.transform.position;
            int combinedLayerMask = 0;
            foreach (var layerColor in layerColors)
            {
                combinedLayerMask |= layerColor.layerMask.value;
            }
            Collider[] colliders = Physics.OverlapSphere(camPos, revealRadius, combinedLayerMask).OrderBy(c=>c.gameObject.layer).ToArray();
            List<(Vector3 pos, Vector3 boundsSize, int layer)> objectData = new List<(Vector3, Vector3, int)>(colliders.Length);

            foreach (Collider col in colliders)
            {
                objectData.Add((col.transform.position, col.bounds.size, col.gameObject.layer));
            }

            // Выполняем расчёты в отдельном потоке
            await UniTask.RunOnThreadPool(() =>
            {
                RevealMap(camPos, objectData);
                UpdateOverlay(camPos);
            }, configureAwait: false);

            // Применяем изменения в основном потоке
            await UniTask.SwitchToMainThread();
            mapTexture.SetPixels32(mapPixels);
            overlayTexture.SetPixels32(overlayPixels);
            mapTexture.Apply();
            overlayTexture.Apply();

            // Ждём интервал (0.5 сек)
            await UniTask.Delay(System.TimeSpan.FromSeconds(updateInterval), cancellationToken: cts.Token);
        }
    }

    private void RevealMap(Vector3 camPos, List<(Vector3 pos, Vector3 boundsSize, int layer)> objectData)
    {
        float scaleX = textureSize / worldWidth;
        float scaleY = textureSize / worldHeight;

        int camPixelX = (int)((camPos.x + worldWidth / 2) * scaleX);
        int camPixelY = (int)((camPos.z + worldHeight / 2) * scaleY);
        int pixelRadius = (int)(revealRadius * scaleX);

        int objectCount = objectData.Count;
        if (objectCount == 0) return;

        List<(int index, Color32 color)> pixelData = new List<(int, Color32)>(objectCount * 100);

        foreach (var (pos, boundsSize, layer) in objectData)
        {
            int centerX = (int)((pos.x + worldWidth / 2) * scaleX);
            int centerY = (int)((pos.z + worldHeight / 2) * scaleY);

            int drawRadiusX = (int)(boundsSize.x * scaleX * viewScale * drawSizeMultiplier * 0.5f);
            int drawRadiusY = (int)(boundsSize.z * scaleY * viewScale * drawSizeMultiplier * 0.5f);

            int minX = Mathf.Max(0, centerX - drawRadiusX);
            int maxX = Mathf.Min(textureSize - 1, centerX + drawRadiusX);
            int minY = Mathf.Max(0, centerY - drawRadiusY);
            int maxY = Mathf.Min(textureSize - 1, centerY + drawRadiusY);

            // Определяем цвет для текущего слоя
            Color32 objectColor = Color.white; // По умолчанию белый
            foreach (var layerColor in layerColors)
            {
                if (((1 << layer) & layerColor.layerMask) != 0)
                {
                    objectColor = layerColor.color;
                    break;
                }
            }

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    float distance = Vector2.Distance(new Vector2(camPixelX, camPixelY), new Vector2(x, y));
                    if (distance <= pixelRadius) // Рисуем только пиксели в радиусе
                    {
                        int index = y * textureSize + x;
                        pixelData.Add((index, objectColor));
                    }
                }
            }
        }

        foreach (var (index, color) in pixelData)
        {
            if (index >= 0 && index < mapPixels.Length)
            {
                mapPixels[index] = color;
            }
        }
    }

    private void UpdateOverlay(Vector3 camPos)
    {
        ClearOverlay();

        float scaleX = textureSize / worldWidth;
        float scaleY = textureSize / worldHeight;

        int camPixelX = (int)((camPos.x + worldWidth / 2) * scaleX);
        int camPixelY = (int)((camPos.z + worldHeight / 2) * scaleY);
        int pixelRadius = (int)(revealRadius * scaleX);

        int minX = Mathf.Max(0, camPixelX - pixelRadius);
        int maxX = Mathf.Min(textureSize - 1, camPixelX + pixelRadius);
        int minY = Mathf.Max(0, camPixelY - pixelRadius);
        int maxY = Mathf.Min(textureSize - 1, camPixelY + pixelRadius);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                float distance = Vector2.Distance(new Vector2(camPixelX, camPixelY), new Vector2(x, y));
                if (distance <= pixelRadius && Mathf.Abs(distance - pixelRadius) < 1f)
                {
                    overlayPixels[y * textureSize + x] = Color.red;
                }
            }
        }
    }

    private void Update()
    {
        UpdateVisibleArea();
    }

    private void UpdateVisibleArea()
    {
        UpdateUVRect();

        Vector3 camPos = mainCamera.transform.position;
        float uvX = (camPos.x + worldWidth / 2) / worldWidth;
        float uvY = (camPos.z + worldHeight / 2) / worldHeight;

        uvRect.x = uvX - uvRect.width / 2;
        uvRect.y = uvY - uvRect.height / 2;

        uvRect.x = Mathf.Clamp01(uvRect.x);
        uvRect.y = Mathf.Clamp01(uvRect.y);
        if (uvRect.xMax > 1f) uvRect.x = 1f - uvRect.width;
        if (uvRect.yMax > 1f) uvRect.y = 1f - uvRect.height;

        minimapRawImage.uvRect = uvRect;
    }

    private void UpdateUVRect()
    {
        float scaledSize = baseVisibleSize / viewScale;
        scaledSize = Mathf.Clamp(scaledSize, 0.01f, 1f);
        uvRect.width = scaledSize;
        uvRect.height = scaledSize;
    }

    void OnValidate()
    {
        if (minimapRawImage != null)
        {
            UpdateUVRect();
            minimapRawImage.uvRect = uvRect;
        }
    }
}