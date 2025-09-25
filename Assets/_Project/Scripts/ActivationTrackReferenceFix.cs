using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class ActivationTrackReferenceFix : MonoBehaviour
{
    [SerializeField] private PlayableDirector playableDirector;
    [SerializeField] private string targetObjectName = "Text (Credits)"; // Имя объекта для привязки

    void Awake()
    {
        if (playableDirector == null || playableDirector.playableAsset == null)
        {
            Debug.LogWarning("PlayableDirector or its asset is not assigned!");
            return;
        }

        // Находим целевой объект
        GameObject targetObject = GameObject.Find(targetObjectName);
        if (targetObject == null)
        {
            Debug.LogWarning($"Target object '{targetObjectName}' not found!");
            return;
        }

        // Находим все Activation треки
        int activationTrackCount = 0;
        TrackAsset lastActivationTrack = null;

        foreach (var output in playableDirector.playableAsset.outputs)
        {
            if (output.outputTargetType == typeof(GameObject) && 
                output.sourceObject != null && 
                output.sourceObject is ActivationTrack)
            {
                activationTrackCount++;
                lastActivationTrack = output.sourceObject as TrackAsset;
            }
        }

        // Если нашли хотя бы 2 Activation трека, привязываем к последнему
        if (activationTrackCount >= 2 && lastActivationTrack != null)
        {
            // Устанавливаем привязку для последнего Activation трека
            playableDirector.SetGenericBinding(lastActivationTrack, targetObject);
            targetObject.GetComponent<TextMeshProUGUI>().enabled = true;
            targetObject.SetActive(false);
            //Debug.Log($"Successfully bound '{targetObjectName}' to the last Activation Track");
        }
        else if (activationTrackCount == 1)
        {
            // Если только один трек, используем его
            foreach (var output in playableDirector.playableAsset.outputs)
            {
                if (output.outputTargetType == typeof(GameObject) && 
                    output.sourceObject != null && 
                    output.sourceObject is ActivationTrack)
                {
                    playableDirector.SetGenericBinding(output.sourceObject, targetObject);
                    targetObject.GetComponent<TextMeshProUGUI>().enabled = true;
                    targetObject.SetActive(false);
                    //Debug.Log($"Successfully bound '{targetObjectName}' to the only Activation Track");
                    break;
                }
            }
        }
        else
        {
            Debug.LogWarning($"Not enough Activation Tracks found. Found: {activationTrackCount}, need at least 1");
        }
    }
}