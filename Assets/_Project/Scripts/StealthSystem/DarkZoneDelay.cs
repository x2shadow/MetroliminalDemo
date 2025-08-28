using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// DarkZoneDelay: устанавливает уровень затемнения игрока, учитывая, стоит ли он в приседе.
/// - darknessLevel == 0 : зона не тёмная (ничего не делает)
/// - darknessLevel >= 1 : если игрок стоит => level = 1, если присел => level = 2
/// 
/// Работает через PlayerStealth (предпочтительно). Если компонента нет — пробует найти PlayerController
/// и прочитать публичное свойство/поле "IsCrouching". Если и его нет — просто применяет стоя/присед неявно.
/// 
/// Дополнительно: DelayOn / DelayOff управляют временем включённой/выключенной зоны.
/// Зона изначально включена.
/// </summary>
[RequireComponent(typeof(Collider))]
public class DarkZoneDelay : MonoBehaviour
{
    [Tooltip("0 = not dark, 1 = dim (hidden modifier), 2 = full dark (undetectable visually)")]
    [Range(0,2)]
    public int darknessLevel = 2;

    [Header("Delay cycle (seconds)")]
    [Tooltip("Время (в сек) в течение которого зона будет ВКЛЮЧЕНА на каждом цикле. Зона изначально включена.")]
    public float DelayOn = 5f;

    [Tooltip("Время (в сек) в течение которого зона будет ВЫКЛЮЧЕНА на каждом цикле.")]
    public float DelayOff = 3f;

    // Флаг работы зоны: если false — зона временно не действует (ведёт себя как darknessLevel == 0)
    private bool zoneActive = true;

    // Трекер коллайдеров, которые сейчас внутри триггера (помогает очистить/восстановить при переключении)
    private HashSet<Collider> collidersInside = new HashSet<Collider>();

    private Coroutine cycleCoroutine;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnEnable()
    {
        // стартуем цикл при включении компонента
        StartCycle();
    }

    private void OnDisable()
    {
        // остановим цикл при выключении компонента
        StopCycle();
        // если компонент или объект отключается — нужно очистить затемнение у всех, чтобы не осталось "висевших" эффектов
        ClearDarknessForAll();
    }

    private void OnDestroy()
    {
        StopCycle();
    }

    private void StartCycle()
    {
        // если darknessLevel == 0 — нет смысла запускать цикл
        if (darknessLevel <= 0) return;

        // если уже запущено — ничего не делаем
        if (cycleCoroutine != null) return;

        // стартуем с включённого состояния
        zoneActive = true;
        ApplyActiveToAll();

        cycleCoroutine = StartCoroutine(CycleRoutine());
    }

    private void StopCycle()
    {
        if (cycleCoroutine != null)
        {
            StopCoroutine(cycleCoroutine);
            cycleCoroutine = null;
        }
    }

    private IEnumerator CycleRoutine()
    {
        // Небольшая защита: если DelayOn/DelayOff отрицательны — считаем их нулём
        float on = Mathf.Max(0f, DelayOn);
        float off = Mathf.Max(0f, DelayOff);

        while (true)
        {
            // зона включена на DelayOn секунд
            if (on > 0f) yield return new WaitForSeconds(on);
            else yield return null; // если 0 — пропускаем, но даём возможность переключиться

            // переключаем в выключенное состояние
            zoneActive = false;
            ClearDarknessForAll();

            // ожидаем DelayOff
            if (off > 0f) yield return new WaitForSeconds(off);
            else yield return null;

            // снова включаем и применяем для всех внутри
            zoneActive = true;
            ApplyActiveToAll();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // трекинг для корректной обработки при переключениях цикла
        collidersInside.Add(other);
        UpdateDarknessForCollider(other);
    }

    private void OnTriggerStay(Collider other)
    {
        // коллайдеры, находящиеся внутри — подтверждаем трекинг (иногда OnTriggerEnter мог не сработать)
        collidersInside.Add(other);
        UpdateDarknessForCollider(other);
    }

    private void OnTriggerExit(Collider other)
    {
        // удаляем из трекера
        collidersInside.Remove(other);

        // Если игрок выходит из зоны — вернуть 0 (как раньше)
        var ps = other.GetComponentInParent<PlayerStealth>();
        if (ps != null)
        {
            ps.SetDarknessLevel(0);
            return;
        }

        // fallback: если нет PlayerStealth, попробуем PlayerController (если у вас есть публичное поле/свойство)
        var pc = other.GetComponentInParent<PlayerController>();
        if (pc != null)
        {
            // попытка найти PlayerStealth на том же объекте (на случай, если он был на root)
            var ps2 = pc.GetComponent<PlayerStealth>();
            if (ps2 != null)
            {
                ps2.SetDarknessLevel(0);
            }
            else
            {
                // если нет PlayerStealth — ничего больше не делаем
            }
        }
    }

    /// <summary>
    /// Применяет затемнение (если зона активна). Если зона неактивна — ничего не делает.
    /// </summary>
    private void UpdateDarknessForCollider(Collider other)
    {
        if (darknessLevel <= 0) return; // зона не тёмная — ничего не делаем

        if (!zoneActive) return; // зона временно выключена — не ставим затемнение

        // 1) Предпочтительный путь: используем PlayerStealth, если он есть
        var ps = other.GetComponentInParent<PlayerStealth>();
        if (ps != null)
        {
            bool isCrouch = ps.IsCrouching;
            ps.SetDarknessLevel(isCrouch ? 2 : 1);
            return;
        }

        // 2) fallback: попробуем PlayerController (если в нём есть публичный IsCrouching)
        var pc = other.GetComponentInParent<PlayerController>();
        if (pc != null)
        {
            // Попробуем обратиться к публичному свойству/полю IsCrouching через reflection (без ошибок, если нет)
            bool isCrouch = false;
            var type = pc.GetType();

            // first try property
            var prop = type.GetProperty("IsCrouching");
            if (prop != null && prop.PropertyType == typeof(bool))
            {
                object val = prop.GetValue(pc);
                if (val is bool b) isCrouch = b;
            }
            else
            {
                // try field
                var field = type.GetField("IsCrouching");
                if (field != null && field.FieldType == typeof(bool))
                {
                    object val = field.GetValue(pc);
                    if (val is bool b) isCrouch = b;
                }
            }

            // Если нашли публичное поле/свойство — применим значение
            if (prop != null || type.GetField("IsCrouching") != null)
            {
                // Если у PlayerController нет PlayerStealth — мы всё равно можем добавить уровень во внешнюю логику.
                var ps2 = pc.GetComponent<PlayerStealth>();
                if (ps2 != null)
                {
                    ps2.SetDarknessLevel(isCrouch ? 2 : 1);
                }
                else
                {
                    // Если PlayerStealth нет — ничего делать не будем (можно логировать)
                    // Debug.LogWarning("DarkZone: PlayerController имеет IsCrouching, но PlayerStealth не найден.");
                }
                return;
            }

            // Если ни prop ни field не найдены — пытаемся безопасно применить, если PlayerStealth всё-таки есть выше
        }

        // 3) Ничего из вышеперечисленного не найдено — ничего не делаем
    }

    /// <summary>
    /// Устанавливает darkness = 0 для всех игроков/объектов, которые сейчас внутри триггера.
    /// Вызывается при выключении зоны или при деактивации компонента.
    /// </summary>
    private void ClearDarknessForAll()
    {
        // Создаём копию, т.к. SetDarknessLevel может изменить состояние объектов и триггер-лист
        var copy = new List<Collider>(collidersInside);
        foreach (var c in copy)
        {
            if (c == null) continue;

            var ps = c.GetComponentInParent<PlayerStealth>();
            if (ps != null)
            {
                ps.SetDarknessLevel(ps.DarknessLevel - 1);
                continue;
            }

            var pc = c.GetComponentInParent<PlayerController>();
            if (pc != null)
            {
                var ps2 = pc.GetComponent<PlayerStealth>();
                if (ps2 != null)
                {
                    ps2.SetDarknessLevel(ps2.DarknessLevel - 1);
                }
                // иначе — нет PlayerStealth, ничего не делаем
            }
        }
    }

    /// <summary>
    /// Применяет тёмность (в зависимости от стояния/приседа) для всех коллайдеров внутри.
    /// Вызывается при включении зоны.
    /// </summary>
    private void ApplyActiveToAll()
    {
        var copy = new List<Collider>(collidersInside);
        foreach (var c in copy)
        {
            if (c == null) continue;
            UpdateDarknessForCollider(c);
        }
    }
}
