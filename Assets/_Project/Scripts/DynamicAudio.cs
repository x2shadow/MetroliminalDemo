using UnityEngine;

public class DynamicAudio : MonoBehaviour
{
    public Transform player;
    public float switchDistance = 5f;
    public AudioSource audioSource;

    void Awake()
    {
        if (player == null) player = GameObject.Find("Player").transform;
    }

    void Update()
    {
        if (player != null && audioSource != null)
        {
            float distance = Vector3.Distance(transform.position, player.position);
            
            // Плавно переключаем между 3D и 2D звуком
            audioSource.spatialBlend = Mathf.Clamp01((distance - switchDistance) / switchDistance);
        }
    }
}