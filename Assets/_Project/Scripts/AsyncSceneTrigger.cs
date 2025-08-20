using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class AsyncSceneTrigger : MonoBehaviour
{
    [Header("Scenes")]
    [Tooltip("Имя сцены, которую нужно подгрузить аддитивно при входе в триггер.")]
    public string sceneToLoad;

    [Tooltip("Имя сцены, которую нужно выгрузить после задержки (оставьте пустым - не выгружать).")]
    public string sceneToUnload;

    [Header("Trigger")]
    [Tooltip("Тег объекта, который может активировать триггер. Оставьте пустым, чтобы разрешить любой объект.")]
    public string requiredTag = "Player";

    [Header("Timing")]
    [Tooltip("Задержка в секундах после загрузки сцены перед выгрузкой другой сцены.")]
    public float delaySeconds = 1f;

    [Tooltip("Если true — действие выполнится только один раз.")]
    public bool triggerOnce = true;

    [Header("Events (optional)")]
    public UnityEvent onSceneLoaded;
    public UnityEvent onSceneUnloaded;

    bool hasTriggered = false;
    Coroutine runningCoroutine = null;

    void OnValidate()
    {
        if (delaySeconds < 0f) delaySeconds = 0f;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsValidTrigger(other.gameObject)) return;
        TryStart();
    }

    bool IsValidTrigger(GameObject go)
    {
        if (string.IsNullOrEmpty(requiredTag)) return true;
        return go.CompareTag(requiredTag);
    }

    void TryStart()
    {
        if (triggerOnce && hasTriggered) return;
        if (runningCoroutine != null) return; // уже выполняется

        runningCoroutine = StartCoroutine(LoadPauseUnloadCoroutine());
        hasTriggered = true;
    }

    IEnumerator LoadPauseUnloadCoroutine()
    {
        // Загрузка сцены (если указана)
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            AsyncOperation loadOp = null;
            try
            {
                loadOp = SceneManager.LoadSceneAsync(sceneToLoad, LoadSceneMode.Additive);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"AsyncSceneTrigger: исключение при попытке загрузить сцену '{sceneToLoad}': {ex.Message}");
            }

            if (loadOp == null)
            {
                Debug.LogWarning($"AsyncSceneTrigger: не удалось начать загрузку сцены '{sceneToLoad}'. Проверьте имя сцены и Build Settings.");
            }
            else
            {
                while (!loadOp.isDone)
                    yield return null;

                onSceneLoaded?.Invoke();
            }
        }

        // Пауза
        if (delaySeconds > 0f)
            yield return new WaitForSeconds(delaySeconds);

        // Выгрузка сцены (если указана)
        if (!string.IsNullOrEmpty(sceneToUnload))
        {
            AsyncOperation unloadOp = null;
            try
            {
                unloadOp = SceneManager.UnloadSceneAsync(sceneToUnload);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"AsyncSceneTrigger: исключение при попытке выгрузить сцену '{sceneToUnload}': {ex.Message}");
            }

            if (unloadOp == null)
            {
                Debug.LogWarning($"AsyncSceneTrigger: не удалось начать выгрузку сцены '{sceneToUnload}'. Проверьте имя сцены.");
            }
            else
            {
                while (!unloadOp.isDone)
                    yield return null;

                onSceneUnloaded?.Invoke();
            }
        }

        runningCoroutine = null;
    }

    // Сбросить флаг, чтобы триггер можно было активировать снова (если triggerOnce = true и вы хотите повторно запускать).
    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}
