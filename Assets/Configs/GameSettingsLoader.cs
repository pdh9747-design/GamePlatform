using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class GameSettingsData
{
    public float moveSpeed = 5f;
    public float gameTime = 60f;
    public float speedIncreaseInterval = 15f;
    public float speedIncreaseAmount = 0.2f;
    public float maxSpeedMultiplier = 2f;
}

public class GameSettingsLoader : MonoBehaviour
{
    public static GameSettingsLoader Instance;

    public GameSettingsData settings = new GameSettingsData();

    public bool IsLoaded { get; private set; }

    [Header("Server Settings")]
    public string serverSettingsUrl =
        "https://localhost:7017/Content/Configs/GameSettings.json";

    private string localSettingsPath;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        localSettingsPath =
            Application.streamingAssetsPath + "/GameSettings.json";
    }

    void Start()
    {
        StartCoroutine(LoadSettings());
    }

    IEnumerator LoadSettings()
    {
        IsLoaded = false;

        // 1. HTTPS 서버에서 설정 요청
        Debug.Log("서버 JSON 설정 요청 시작!");

        using (UnityWebRequest request =
               UnityWebRequest.Get(serverSettingsUrl))
        {
            request.timeout = 10;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                GameSettingsData serverData;

                if (TryParseSettings(request.downloadHandler.text,
                                     out serverData))
                {
                    settings = serverData;

                    Debug.Log("서버 JSON 설정 적용 성공!");
                    FinishLoading();
                    yield break;
                }

                Debug.LogWarning("서버 JSON이 올바르지 않습니다.");
            }
            else
            {
                Debug.LogWarning(
                    "서버 접속 실패: " + request.error);
            }
        }

        // 2. 서버 실패 시 로컬 JSON 사용
        Debug.Log("로컬 JSON 설정 불러오기 시작!");

        using (UnityWebRequest request =
               UnityWebRequest.Get(localSettingsPath))
        {
            request.timeout = 10;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                GameSettingsData localData;

                if (TryParseSettings(request.downloadHandler.text,
                                     out localData))
                {
                    settings = localData;

                    Debug.Log("로컬 JSON 설정 적용 성공!");
                    FinishLoading();
                    yield break;
                }
            }

            Debug.LogWarning("로컬 JSON도 불러오지 못했습니다.");
        }

        // 3. 서버와 로컬 모두 실패하면 기본값 사용
        settings = new GameSettingsData();

        Debug.LogWarning("기본 설정값을 사용합니다.");

        FinishLoading();
    }

    bool TryParseSettings(string json, out GameSettingsData data)
    {
        data = null;

        try
        {
            data = JsonUtility.FromJson<GameSettingsData>(json);

            if (data == null) return false;
            if (data.moveSpeed <= 0f) return false;
            if (data.gameTime <= 0f) return false;
            if (data.speedIncreaseInterval <= 0f) return false;
            if (data.speedIncreaseAmount < 0f) return false;
            if (data.maxSpeedMultiplier < 1f) return false;

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("JSON 처리 오류: " + e.Message);
            return false;
        }
    }

    void FinishLoading()
    {
        IsLoaded = true;

        Debug.Log(
            "게임 설정 적용 준비 완료 - 이동 속도: " +
            settings.moveSpeed +
            " / 게임 시간: " +
            settings.gameTime
        );
    }
}
