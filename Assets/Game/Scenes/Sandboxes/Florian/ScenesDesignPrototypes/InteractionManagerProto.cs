using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class InteractionManagerProto : MonoBehaviour
{
    public static InteractionManagerProto Instance { get; private set; }

    // Ref aux Cameras (cinemachine)
    [Header("Caméras Cinemachine")]
    [SerializeField] private CinemachineBrain brain;
    [SerializeField] private CinemachineCamera startUpCamera;
    [SerializeField] private CinemachineCamera advisorChoiceCamera;

    // Ref aux "Panels" 
    [Header("UI Panels")]
    [SerializeField] private GameObject startUpUIPanel;
    [SerializeField] private GameObject advisorChoiceUIPanel;

    //enum to manage panels name
    public enum PanelType
    {
        startUpUI,
        AdvisorChoiceUI
    }


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        brain = Camera.main.GetComponent<CinemachineBrain>();
    }


    public void DoAfterBlend(Action onComplete)
    {
        StartCoroutine(WaitForBlendThen(onComplete));
    }

    private IEnumerator WaitForBlendThen(Action onComplete)
    {
        yield return null;

        while (brain.IsBlending)
        {
            yield return null;
        }

        onComplete?.Invoke();
    }

    public void StartMission()
    {
        startUpUIPanel.SetActive(false);
        advisorChoiceCamera.gameObject.SetActive(true);
        DoAfterBlend(() => advisorChoiceUIPanel.SetActive(true));
    }
}
