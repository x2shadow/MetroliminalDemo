using UnityEngine;

/// <summary>
/// DarkZone: устанавливает уровень затемнения игрока, учитывая, стоит ли он в приседе.
/// - darknessLevel == 0 : зона не тёмная (ничего не делает)
/// - darknessLevel >= 1 : если игрок стоит => level = 1, если присел => level = 2
/// 
/// Работает через PlayerStealth (предпочтительно). Если компонента нет — пробует найти PlayerController
/// и прочитать публичное свойство/поле "IsCrouching". Если и его нет — просто применяет стоя/присед неявно.
/// </summary>
[RequireComponent(typeof(Collider))]
public class DarkZone : MonoBehaviour
{
    [Tooltip("0 = not dark, 1 = dim (hidden modifier), 2 = full dark (undetectable visually)")]
    [Range(0,2)]
    public int darknessLevel = 2;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        UpdateDarknessForCollider(other);
    }

    private void OnTriggerStay(Collider other)
    {
        UpdateDarknessForCollider(other);
    }

    private void OnTriggerExit(Collider other)
    {
        // Если игрок выходит из зоны — вернуть 0
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

    private void UpdateDarknessForCollider(Collider other)
    {
        if (darknessLevel <= 0) return; // зона не тёмная — ничего не делаем

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
}
