using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class AsyncSceneTrigger : MonoBehaviour
{
    [Header("Scenes")]
    [Tooltip("Имя сцены, которую нужно подгрузить аддитивно при входе в триггер.")]
    public string sceneToLoad;

    [Tooltip("Имя сцены, которую нужно выгрузить перед загрузкой новой сцены (оставьте пустым - не выгружать).")]
    public string sceneToUnload;

    [Header("Trigger")]
    [Tooltip("Тег объекта, который может активировать триггер. Оставьте пустым, чтобы разрешить любой объект.")]
    public string requiredTag = "Player";

    [Header("Timing")]
    [Tooltip("Задержка в секундах между выгрузкой старой сцены и загрузкой новой.")]
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

        runningCoroutine = StartCoroutine(UnloadThenLoadCoroutine());
        hasTriggered = true;
    }

    IEnumerator UnloadThenLoadCoroutine()
    {
        // 1) Выгружаем старую сцену (если указана и она загружена), кроме случая когда имена совпадают
        if (!string.IsNullOrEmpty(sceneToUnload))
        {
            if (!string.IsNullOrEmpty(sceneToLoad) && sceneToUnload == sceneToLoad)
            {
                Debug.Log($"AsyncSceneTrigger: сцена для выгрузки '{sceneToUnload}' совпадает с целевой сценой загрузки — пропускаю выгрузку.");
            }
            else
            {
                Scene existingUnload = SceneManager.GetSceneByName(sceneToUnload);
                if (!existingUnload.isLoaded)
                {
                    Debug.Log($"AsyncSceneTrigger: сцена '{sceneToUnload}' не загружена — пропускаю выгрузку.");
                }
                else
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
                        Debug.Log($"AsyncSceneTrigger: сцена '{sceneToUnload}' успешно выгружена.");
                    }
                }
            }
        }

        // 2) Пауза между выгрузкой и загрузкой (может быть нулевой)
        if (delaySeconds > 0f)
            yield return new WaitForSeconds(delaySeconds);

        // 3) Загружаем новую сцену (если указана и ещё не загружена)
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            Scene existingLoad = SceneManager.GetSceneByName(sceneToLoad);
            if (existingLoad.isLoaded)
            {
                Debug.Log($"AsyncSceneTrigger: сцена '{sceneToLoad}' уже загружена — пропускаю загрузку.");
            }
            else
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
                    Debug.Log($"AsyncSceneTrigger: сцена '{sceneToLoad}' успешно загружена (additive).");
                }
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
