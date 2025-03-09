using System;
using System.Collections;
using System.Collections.Generic;
using _Project.Develop.Core.Enum;
using DG.Tweening;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SelectableCharacter : MonoBehaviour
{
    public StatPreset Stats;
    [TextArea(2,5)]
    public string Description;

    private Camera camera;
    private Vector3 cameraLastPos = Vector3.zero;
    private Quaternion cameraLastRotate = Quaternion.identity;

    public TMP_Text description;
    public GameObject DescriptionPanel;
    public Button _select;
    public Button _cancel;

    private void Awake()
    {
        camera = Camera.main;
        
        _cancel.onClick.AddListener(() =>
        {
            camera.transform.DOMove(new Vector3(1518f,783f,-817), 1);
            camera.transform.DORotate(new Vector3(0,90,0), 1);
            DescriptionPanel.SetActive(false);
        });
        
    }

    private void OnMouseDown()
    {
        
        DescriptionPanel.SetActive(true);
        description.text = Description;

        camera.transform.DOMove(transform.position + transform.forward*3 + transform.up*2, 1);
        camera.transform.DORotate(Quaternion.LookRotation(-transform.forward - transform.right/2, camera.transform.up).eulerAngles, 1);
        _select.onClick.RemoveAllListeners();
        _select.onClick.AddListener(() =>
        {
            GameData._preset = Stats;
            SceneManager.LoadSceneAsync(1);
        });
    }
}
