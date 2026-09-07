using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Entities;

public class GameManager : MonoBehaviour
{
    public TMP_Text fps;
    public TMP_Text bit;
    public Button Button;

    private const float INTERVAL = 1.0f;
    private float _acc = 0f;
    private int _frames = 0;
    private float _left;

    private World _world;
    private bool _filter = false;

    void Start()
    {
        Debug.Log($"CPU: {SystemInfo.processorType}");
        Debug.Log($"GPU: {SystemInfo.graphicsDeviceName}");
        Debug.Log($"RAM: {SystemInfo.systemMemorySize}MB");

        _left = INTERVAL;
        _world = World.DefaultGameObjectInjectionWorld;

        InitializeBitFilterButton();
    }

    void Update()
    {
        float deltaTime = Time.unscaledDeltaTime;
        _left -= deltaTime;
        _acc += deltaTime;
        _frames++;

        if (_left <= 0.0f)
        {
            fps.text = $"FPS: {_frames / _acc:F1}";

            _left = INTERVAL;
            _acc = 0.0f;
            _frames = 0;
        }
    }

    private void InitializeBitFilterButton()
    {
        if (Button == null)
        {
            return;
        }

        Button.onClick.AddListener(ToggleBitFilter);
        ApplyBitFilter();
    }

    public void ToggleBitFilter()
    {
        _filter = !_filter;
        ApplyBitFilter();
        ResetTimer();
    }

    private void ApplyBitFilter()
    {
        var handle = _world.GetExistingSystem<CollisionSystem>();
        if (handle != SystemHandle.Null)
        {
            _world.Unmanaged.GetUnsafeSystemRef<CollisionSystem>(handle).UseBitFilter = _filter;
        }

        if (bit != null)
        {
            bit.text = $"Bit Filter: {(_filter ? "ON" : "OFF")}";
        }
    }

    private void ResetTimer()
    {
        _left = INTERVAL;
        _acc = 0.0f;
        _frames = 0;
        if (fps != null) fps.text = "FPS: --";
    }
}