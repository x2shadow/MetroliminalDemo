using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;


public class CinemachineBrainReferenceFix : MonoBehaviour
{
    [SerializeField] private PlayableDirector playableDirector;
    [SerializeField] private string playerObjectName = "Player"; // Имя объекта игрока
    [SerializeField] private string signalTrackName = "Signal Track"; // Имя сигнального трека

    void Awake()
    {
        // Fix CinemachineBrain reference
        CinemachineBrain cinemachineBrain = Camera.main?.GetComponent<CinemachineBrain>();

        foreach (var output in playableDirector.playableAsset.outputs)
        {
            if (output.outputTargetType == typeof(CinemachineBrain))
            {
                // Устанавливаем привязку
                playableDirector.SetGenericBinding(output.sourceObject, cinemachineBrain);
                break; // Прерываем после нахождения первого подходящего выхода
            }
        }

        // Fix Player SignalReceiver reference
        GameObject playerObject = GameObject.Find(playerObjectName);
        SignalReceiver signalReceiver = playerObject.GetComponent<SignalReceiver>();

        foreach (var output in playableDirector.playableAsset.outputs)
        {
            if (output.outputTargetType == typeof(SignalReceiver) && output.streamName == signalTrackName)
            {
                // Устанавливаем привязку
                playableDirector.SetGenericBinding(output.sourceObject, signalReceiver);
                break; // Прерываем после нахождения первого подходящего выхода
            }
        }
    }
}
