using UnityEngine;

public class PlayerStealth : MonoBehaviour
{
    public enum MovementNoise { Silent = 0, Crouch = 1, Walk = 2, Run = 3 }

    [Header("Stealth state (readonly from other systems)")]
    [SerializeField] private int darknessLevel = 0; // 0..2
    [SerializeField] private MovementNoise currentNoise = MovementNoise.Silent;
    [SerializeField] private bool isCrouching = false;

    public int DarknessLevel => darknessLevel;
    public MovementNoise CurrentNoise => currentNoise;
    public bool IsCrouching => isCrouching;

    // API
    public void SetDarknessLevel(int level)
    {
        darknessLevel = Mathf.Clamp(level, 0, 2);
    }

    public void SetCrouch(bool crouch)
    {
        isCrouching = crouch;
        currentNoise = crouch ? MovementNoise.Crouch : MovementNoise.Silent;
    }

    public void SetMovementNoise(MovementNoise noise)
    {
        currentNoise = noise;
        // if crouching flag contradicts noise, keep isCrouching true when noise==Crouch
        isCrouching = (noise == MovementNoise.Crouch) ? true : isCrouching;
    }

    // optional debug inspector
#if UNITY_EDITOR
    private void OnValidate()
    {
        darknessLevel = Mathf.Clamp(darknessLevel, 0, 2);
    }
#endif
}
