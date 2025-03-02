using UnityEngine;

public class CameraShake : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform; // Трансформ камеры, которую нужно трясти
    [SerializeField] public float shakeMagnitude = 0f; // Магнитуда тряски (сила)
    [SerializeField] private float shakeFrequency = 1f;  // Частота тряски

    private Vector3 initialPosition;  // Начальная позиция камеры

    private void Start()
    {
        // Сохраняем начальное положение камеры
        initialPosition = cameraTransform.localPosition;
    }

    private void Update()
    {
        // Если сила тряски больше 0
        if (shakeMagnitude > 0)
        {
            // Вычисляем смещение с использованием шума Перлина
            float xOffset = (Mathf.PerlinNoise(Time.time * shakeFrequency, 0f) - 0.5f) * 2 * shakeMagnitude;
            float yOffset = (Mathf.PerlinNoise(0f, Time.time * shakeFrequency) - 0.5f) * 2 * shakeMagnitude;

            // Применяем смещение к позиции камеры
            cameraTransform.localPosition = initialPosition + new Vector3(xOffset, yOffset, 0f);
        }
        else
        {
            // Возвращаем камеру в исходное положение, если тряска не активна
            cameraTransform.localPosition = initialPosition;
        }
    }

    // Метод для обновления силы тряски
    public void SetShakeMagnitude(float magnitude)
    {
        shakeMagnitude = magnitude;
    }
}