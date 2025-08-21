using UnityEngine;

/*
    При вызове EmitNoise() находит все EntityAI в радиусе и сообщает им о шуме
    Добавляется NoiseEmitter на объекты, которые могут шуметь;
    Вызов EmitNoise() (через событие — падение предмета, OnCollision или вручную через Inspector)
*/

public class NoiseEmitter : MonoBehaviour
{
    [Tooltip("Noise level: 0..3 (0=none, 3=run)")]
    public int noiseLevel = 3;
    public float radius = 6f;
    public bool emitOnStart = false;

    private void Start()
    {
        if (emitOnStart) EmitNoise();
    }

    [ContextMenu("Emit Noise")]
    public void EmitNoise()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, radius);
        foreach (var col in hits)
        {
            var ai = col.GetComponent<EntityAI>();
            if (ai != null)
            {
                ai.TriggerHeardNoise(noiseLevel, transform.position);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
