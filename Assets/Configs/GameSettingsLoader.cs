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

    private string settingsPath;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        settingsPath = Application.streamingAssetsPath
            + "/GameSettings.json";
    }

    void Start()
    {
        StartCoroutine(LoadSettings());
    }

    IEnumerator LoadSettings()
    {
        IsLoaded = false;

        using (UnityWebRequest request =
            UnityWebRequest.Get(settingsPath))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning(
                    "JSON 설정 파일 읽기 실패. 기본값을 사용합니다. "
                    + request.error
                );

                settings = new GameSettingsData();
            }
            else
            {
                try
                {
                    string json = request.downloadHandler.text;

                    GameSettingsData loadedSettings =
                        JsonUtility.FromJson<GameSettingsData>(json);

                    if (IsValidSettings(loadedSettings))
                    {
                        settings = loadedSettings;
                        Debug.Log("JSON 설정 불러오기 성공!");
                    }
                    else
                    {
                        settings = new GameSettingsData();

                        Debug.LogWarning(
                            "JSON 설정값이 올바르지 않습니다. 기본값을 사용합니다."
                        );
                    }
                }
                catch (Exception e)
                {
                    settings = new GameSettingsData();

                    Debug.LogError(
                        "JSON 설정 처리 실패: " + e.Message
                    );
                }
            }
        }

        IsLoaded = true;

        Debug.Log(
            "게임 설정 적용 준비 완료 - 이동 속도: "
            + settings.moveSpeed
            + " / 게임 시간: "
            + settings.gameTime
        );
    }

    bool IsValidSettings(GameSettingsData data)
    {
        if (data == null)
            return false;

        if (data.moveSpeed <= 0f)
            return false;

        if (data.gameTime <= 0f)
            return false;

        if (data.speedIncreaseInterval <= 0f)
            return false;

        if (data.speedIncreaseAmount < 0f)
            return false;

        if (data.maxSpeedMultiplier < 1f)
            return false;

        return true;
    }
}
