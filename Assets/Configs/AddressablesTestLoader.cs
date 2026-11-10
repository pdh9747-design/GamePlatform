using UnityEngine;
using TMPro;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressablesTestLoader : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI statusText;

    [Header("Addressables")]
    public string assetAddress = "Assets/AddressablesTest/TestCube.prefab";

    private GameObject loadedObject;
    private AsyncOperationHandle<GameObject> loadHandle;
    private bool isLoading = false;

    public void LoadTestCube()
    {
        if (isLoading) return;

        if (loadedObject != null)
        {
            SetStatus("이미 리소스가 로드되어 있습니다.");
            return;
        }

        isLoading = true;
        SetStatus("리소스 로딩 중...");

        loadHandle = Addressables.LoadAssetAsync<GameObject>(assetAddress);
        loadHandle.Completed += OnLoadCompleted;
    }

    void OnLoadCompleted(AsyncOperationHandle<GameObject> handle)
    {
        isLoading = false;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            loadedObject = Instantiate(handle.Result);
            loadedObject.transform.position = new Vector3(0f, 1f, 0f);

            SetStatus("리소스 로드 성공!");
            Debug.Log("Addressables 비동기 로드 성공!");
        }
        else
        {
            SetStatus("리소스 로드 실패. 다시 시도해 주세요.");
            Debug.LogError("Addressables 리소스 로드 실패!");

            Addressables.Release(handle);
        }
    }

    public void RetryLoad()
    {
        LoadTestCube();
    }

    public void ReleaseTestCube()
    {
        if (isLoading)
        {
            SetStatus("로드가 진행 중입니다.");
            return;
        }

        if (loadedObject != null)
        {
            Destroy(loadedObject);
            loadedObject = null;
        }

        if (loadHandle.IsValid())
        {
            Addressables.Release(loadHandle);
        }

        SetStatus("리소스가 해제되었습니다.");
        Debug.Log("Addressables 리소스 해제 완료!");
    }

    void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    void OnDestroy()
    {
        if (loadedObject != null)
            Destroy(loadedObject);

        if (loadHandle.IsValid())
            Addressables.Release(loadHandle);
    }
}