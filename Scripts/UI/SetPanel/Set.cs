using UnityEngine;
using UnityEngine.UI;

public class Set : MonoBehaviour
{
    private Button _saveTheGameButton;

    private Slider _backgroundMusicSlider;
    private Text _backgroundMusicValue;

    private Slider _soundSlider;
    private Text _soundValue;

    private void Awake()
    {
        Transform saveTrans = transform.Find("SaveTheGameButton");
        if (saveTrans != null)
        {
            _saveTheGameButton = saveTrans.GetComponent<Button>();
            _saveTheGameButton.onClick.RemoveListener(SaveTheGame);
            _saveTheGameButton.onClick.AddListener(SaveTheGame);
        }

        _backgroundMusicSlider = transform.Find("BackgroundMusic").transform.Find("Slider").GetComponent<Slider>();
        _backgroundMusicValue = transform.Find("BackgroundMusic").transform.Find("Text").GetComponent<Text>();

        _soundSlider = transform.Find("Sound").transform.Find("Slider").GetComponent<Slider>();
        _soundValue = transform.Find("Sound").transform.Find("Text").GetComponent<Text>();
    }

    private void SaveTheGame()
    {
        string saveName = GameManager.Instance.SavePlayerData();
        Debug.Log($"[SetPanel] Save player data: {saveName}");
    }

}


