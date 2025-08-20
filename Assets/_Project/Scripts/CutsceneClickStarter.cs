using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.InputSystem;

public class CutsceneClickStarter : MonoBehaviour
{
    public PlayableDirector currentDirector;

    [Tooltip("PlayableDirector следующей катсцены, который нужно запустить по клику")] 
    public PlayableDirector nextDirector;

    [Tooltip("Input Action Reference для Click (например Player/Click). Если не указан — используется fallback через Mouse/Legacy Input)")]
    public InputActionReference clickActionReference;

    [Tooltip("Если указан — вызовем SetInputBlocked(true) у этого компонента при старте прослушки и SetInputBlocked(false) при остановке")]
    public MonoBehaviour playerControllerReference;

    [Tooltip("UI подсказка Нажмите ЛКМ — будет показана во время ожидания клика)")]
    public GameObject clickPromptUI;

    [Tooltip("После запуска следующей катсцены перестать слушать клик и скрыть подсказку")]
    public bool stopListeningOnPlay = true;

    bool isListening = false;

    void OnEnable()
    {
        if (clickPromptUI != null)
            clickPromptUI.SetActive(false);
    }

    void OnDisable()
    {
        // Очистка подписок на случай выключения объекта
        if (clickActionReference != null && clickActionReference.action != null)
        {
            clickActionReference.action.performed -= OnClickPerformed;
            try { clickActionReference.action.Disable(); } catch { }
        }
    }

    /// <summary>
    /// Включает прослушивание клика. Вызвать из Signal в конце первой катсцены.
    /// </summary>
    public void StartListening()
    {
        if (isListening) return;

        // Попробуем заблокировать ввод у playerControllerReference через метод SetInputBlocked(bool)
        if (playerControllerReference != null)
        {
            var mi = playerControllerReference.GetType().GetMethod("SetInputBlocked");
            if (mi != null)
            {
                try { mi.Invoke(playerControllerReference, new object[] { true }); }
                catch (Exception e) { Debug.LogWarning($"CutsceneClickStarter: SetInputBlocked(true) вызвал исключение: {e.Message}"); }
            }
        }

        // Подписываемся на InputAction (если задано)
        if (clickActionReference != null && clickActionReference.action != null)
        {
            clickActionReference.action.performed += OnClickPerformed;
            try { clickActionReference.action.Enable(); } catch { }
        }

        isListening = true;
        if (clickPromptUI != null) clickPromptUI.SetActive(true);
    }

    /// <summary>
    /// Остановить прослушивание. Вызвать в начале следующей катсцены (или при необходимости).
    /// </summary>
    public void StopListening()
    {
        if (!isListening) return;

        if (clickActionReference != null && clickActionReference.action != null)
        {
            clickActionReference.action.performed -= OnClickPerformed;
            try { clickActionReference.action.Disable(); } catch { }
        }

        if (playerControllerReference != null)
        {
            var mi = playerControllerReference.GetType().GetMethod("SetInputBlocked");
            if (mi != null)
            {
                try { mi.Invoke(playerControllerReference, new object[] { false }); }
                catch (Exception e) { Debug.LogWarning($"CutsceneClickStarter: SetInputBlocked(false) вызвал исключение: {e.Message}"); }
            }
        }

        isListening = false;
        if (clickPromptUI != null) clickPromptUI.SetActive(false);
    }

    void Update()
    {
        if (!isListening) return;

        // Fallback если clickActionReference не указан или не работал — проверяем напрямую мышь / legacy input
        if (clickActionReference == null || clickActionReference.action == null)
        {
            #if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                HandleClick();
            }
            #else
            if (Input.GetMouseButtonDown(0))
            {
                HandleClick();
            }
            #endif
        }
    }

    private void OnClickPerformed(InputAction.CallbackContext ctx)
    {
        // Защитимся от многократных вызовов
        if (!isListening) return;
        HandleClick();
    }

    private void HandleClick()
    {
        if (clickPromptUI != null) clickPromptUI.SetActive(false);

        if (nextDirector != null)
        {
            currentDirector.extrapolationMode = DirectorWrapMode.None;
            nextDirector.Play();
        }
        else
        {
            Debug.LogWarning("CutsceneClickStarter: nextDirector не назначен.");
        }

        if (stopListeningOnPlay)
        {
            StopListening();
        }
    }
}
