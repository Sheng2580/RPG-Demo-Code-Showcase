using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;
using UnityEngine.UI;

public class InteractionItem : MonoBehaviour
{
    public Color color;
    private GameObject _key;
    private Text _text;
    private Image _image;

    private Tweener _keyTweener;
    private Tweener _imageTweener;
    public UnityAction Interaction; 

    private void Awake()
    {
        if (transform.childCount > 1)
        {
            var child1 = transform.GetChild(1);
            _image = child1.GetComponent<Image>() ?? child1.GetComponentInChildren<Image>(true);
        }
        if (transform.childCount > 0)
        {
            var child0 = transform.GetChild(0);
            _key = child0 != null ? child0.gameObject : null;
        }

        Transform textTf = null;
        if (transform.childCount > 1)
            textTf = transform.GetChild(1).transform.Find("InteractionText");
        if (textTf != null)
            _text = textTf.GetComponent<Text>();
        if (_text == null)
            _text = GetComponentInChildren<Text>(true);

        if (_image == null) _image = GetComponentInChildren<Image>(true);

    }

    private void OnEnable()
    {
        if (_key != null)
            _keyTweener = _key.transform.DOScale(Vector3.one * 0.7f, 0.5f).SetLoops(-1, LoopType.Yoyo);
        else
            Debug.LogWarning("[InteractionItem] _key is null in OnEnable");

        if (_image != null)
        {
            _imageTweener = _image.DOColor(color, 0.2f).SetLoops(2, LoopType.Yoyo);
            _imageTweener.Pause();
            _imageTweener.SetAutoKill(false);
        }
        else
        {
            Debug.LogWarning("[InteractionItem] _image is null in OnEnable");
        }

        if (_key != null)
            _key.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        _keyTweener?.Kill();
        _imageTweener?.Kill();
    }

    public void InitInteraction(string text , UnityAction action)
    {
        if (_text != null)
            _text.text = text;
        else
            Debug.LogWarning("[InteractionItem] InitInteraction: _text is null, cannot set text");

        Interaction = action;
    }


    public void CallInteraction()
    {
        if (_imageTweener != null)
            _imageTweener.Play();
        else
            Debug.LogWarning("[InteractionItem] CallInteraction: _imageTweener is null");
        MultiTimerManager.Instance.AddOneShotTimer(0.5f,()=>
        {
            Interaction?.Invoke();
            UIManager.Instance.ClosePanel<InteractionPanel>();
        });
    }

    public void UseAction()
    {
        if (_key != null)
            _key.gameObject.SetActive(true);
        else
            Debug.LogWarning("[InteractionItem] UseAction: _key is null");
    }

    public void StopAction()
    {
        if (_key != null)
            _key.gameObject.SetActive(false);
        else
            Debug.LogWarning("[InteractionItem] StopAction: _key is null");
    }


}


