using UnityEngine;
using TMPro;
using Unity.Entities;
using System.Collections.Generic;
using System;

// Inspector에서 관리할 시스템 데이터 구조체
[System.Serializable]
public struct CollisionSystemEntry
{
    public string displayName;       // UI 드롭다운에 표시될 이름 (예: "Best")
    public string systemClassName;   // 실제 ECS 시스템 클래스 이름 (예: "BestCollisionSystem")
}

public class GameManager : MonoBehaviour
{
    public TMP_Text fpsText;
    public TMP_Text avgText;
    public TMP_Text currentSystemText;

    [Header("System Dropdown Settings")]
    public TMP_Dropdown systemDropdown; // UI의 Dropdown 컴포넌트 연결
    public List<CollisionSystemEntry> systemList = new List<CollisionSystemEntry>();

    private float deltaTime = 0.0f;
    private float updateInterval = 0.5f;
    private float accum = 0.0f;
    private int frames = 0;
    private float timeLeft;

    private Queue<float> recentFpsValues = new Queue<float>();
    private const int MAX_FPS_SAMPLES = 10;

    private World _world;
    private int _currentIndex = -1;

#if UNITY_EDITOR
    private float elapsedTime = 0.0f;
    private bool hasPaused = false;
    private int frameCount = 0;
    public bool freezeActive = false;
#endif

    void Start()
    {
        Debug.Log($"CPU: {SystemInfo.processorType}");
        Debug.Log($"GPU: {SystemInfo.graphicsDeviceName}");
        Debug.Log($"RAM: {SystemInfo.systemMemorySize}MB");

        timeLeft = updateInterval;
        _world = World.DefaultGameObjectInjectionWorld;

        InitializeDropdown();
        if (systemList.Count > 0)
        {
            ChangeSystem(0);
        }
    }

    void Update()
    {
#if UNITY_EDITOR
        if (!hasPaused && freezeActive)
        {
            frameCount++;
            if (frameCount > 5)
            {
                elapsedTime += Time.unscaledDeltaTime;
                if (elapsedTime >= 3.0f)
                {
                    UnityEditor.EditorApplication.isPaused = true;
                    hasPaused = true;
                }
            }
        }
#endif

        deltaTime = Time.unscaledDeltaTime;
        timeLeft -= deltaTime;
        accum += 1.0f / deltaTime;
        frames++;

        if (timeLeft <= 0.0f)
        {
            float fps = accum / frames;
            fpsText.text = $"FPS: {fps:F1}";

            recentFpsValues.Enqueue(fps);
            if (recentFpsValues.Count > MAX_FPS_SAMPLES)
                recentFpsValues.Dequeue();

            float avgFps = CalculateAverageFps();
            avgText.text = $"AVG: {avgFps:F1}";

            timeLeft = updateInterval;
            accum = 0.0f;
            frames = 0;
        }
    }

    private float CalculateAverageFps()
    {
        if (recentFpsValues.Count == 0) return 0f;
        float sum = 0f;
        foreach (float fps in recentFpsValues) sum += fps;
        return sum / recentFpsValues.Count;
    }

    private void InitializeDropdown()
    {
        if (systemDropdown == null)
        {
            Debug.LogWarning("System Dropdown이 할당되지 않았습니다.");
            return;
        }

        systemDropdown.ClearOptions();
        List<string> options = new List<string>();

        foreach (var sys in systemList)
        {
            options.Add(sys.displayName);
        }

        systemDropdown.AddOptions(options);

        systemDropdown.onValueChanged.AddListener(ChangeSystem);
    }

    public void ChangeSystem(int index)
    {
        if (index < 0 || index >= systemList.Count) return;
        if (_currentIndex == index) return;

        _currentIndex = index;
        string targetClassName = systemList[index].systemClassName;

        foreach (var sysEntry in systemList)
        {
            Type systemType = Type.GetType(sysEntry.systemClassName);

            if (systemType != null)
            {
                bool isTarget = (sysEntry.systemClassName == targetClassName);
                SetSystemEnabled(systemType, isTarget);
            }
            else
            {
                Debug.LogError($"'{sysEntry.systemClassName}' 클래스를 찾을 수 없습니다. 오타나 네임스페이스를 확인하세요.");
            }
        }

        if (currentSystemText != null)
        {
            currentSystemText.text = $"System: {systemList[index].displayName}";
        }
        ResetAverage();
        Debug.Log($"Switched to {systemList[index].displayName} Collision System");
    }

    private void SetSystemEnabled(Type systemType, bool enabled)
    {
        SystemHandle systemHandle = _world.GetExistingSystem(systemType);

        if (systemHandle != SystemHandle.Null)
        {
            ref var state = ref _world.Unmanaged.ResolveSystemStateRef(systemHandle);
            state.Enabled = enabled;
        }
    }

    private void ResetAverage()
    {
        recentFpsValues.Clear();
        if (avgText != null) avgText.text = "AVG: --";
    }
}