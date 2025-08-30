using UnityEngine;

[DisallowMultipleComponent]
public class PatrolPoint : MonoBehaviour
{
    public enum PointType { Normal, Wait }

    [Tooltip("Тип точки: Normal — просто проход, Wait — остановиться на waitSeconds")]
    public PointType type = PointType.Normal;

    [Tooltip("Если тип == Wait — сколько секунд ждать")]
    public float waitSeconds = 2f;

    public float GetWaitSeconds()
    {
        if (type == PointType.Normal) return 0f;
        return Mathf.Max(0f, waitSeconds);
    }
}
