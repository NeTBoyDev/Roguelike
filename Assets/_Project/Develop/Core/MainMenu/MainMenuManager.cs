using UnityEngine;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private TweenMover Camera;

    [SerializeField] private Button PlayButton = null;
    [SerializeField] private Button SettingsButton = null;
    [SerializeField] private Button ExitButton = null;

    [SerializeField] private Image StartPanel = null;
    [SerializeField] private Image SettingsPanel = null;

    private void OnEnable()
    {   
        PlayButton.onClick.RemoveAllListeners();
        SettingsButton.onClick.RemoveAllListeners();
        ExitButton.onClick.RemoveAllListeners();

        PlayButton.onClick.AddListener(() =>
        {
            Camera.StartMove();
            StartPanel.gameObject.SetActive(false);
        });
        SettingsButton.onClick.AddListener(() => SettingsPanel.gameObject.SetActive(!SettingsPanel.gameObject.activeInHierarchy));
        ExitButton.onClick.AddListener(() => Application.Quit());
    }
}
