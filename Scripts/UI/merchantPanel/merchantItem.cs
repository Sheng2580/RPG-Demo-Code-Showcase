using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.EventSystems;

public class merchantItem : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler, IPointerClickHandler
{
    public Color SelectColor;
    public Text priceText;

    private Material _material;
    private Image _itemImage;
    private Image _maskImage;
    private Text _text;
    private Image _iconImage;
    private Tweener _imageTweener;
    private Tweener _sTweener;
    private Color _originalColor;
    private Vector3 _originalScale;
    private int _iconLoadVersion;
    public commodityClass Commodity { get; private set; }
    public Action<merchantItem> OnClicked;

    private void Awake()
    {
        _itemImage = GetComponent<Image>();
        _originalColor = _itemImage != null ? _itemImage.color : Color.white;
        _originalScale = transform.localScale;
        _maskImage = transform.GetChild(0).gameObject.GetComponent<Image>();
        _iconImage=transform.GetChild(0).transform.GetChild(0).gameObject.GetComponent<Image>();
        _text = transform.GetChild(0).transform.GetChild(1).gameObject.GetComponent<Text>();
        _material = _itemImage.material;

    }

    private void OnEnable()
    {
        _imageTweener = _itemImage.DOColor(SelectColor, 0.2f).SetAutoKill(false);
        _sTweener = transform.DOScale(Vector3.one * 1.2f, 0.2f).SetAutoKill(false);
        _sTweener.Pause();
        _imageTweener.Pause();
    }


    private void OnDisable()
    {
        _imageTweener?.Kill();
        _sTweener?.Kill();
        OnClicked = null;
        _iconLoadVersion++;
    }

    private void Start()
    {
    }


    public void InitMerchantItem(commodityClass  commodity)
    {
        Commodity = commodity;
        if (_text != null)
        {
            _text.text = commodity.CommodityName;
        }

        if (priceText != null)
        {
            priceText.text = commodity.CommodityPrice.ToString();
        }

        if (string.IsNullOrEmpty(commodity.CommodityImageName) || ABManager.Instance == null)
        {
            return;
        }

        int loadVersion = ++_iconLoadVersion;
        ABManager.Instance.LoadResAsync("icon", commodity.CommodityImageName, typeof (Sprite), (obj) =>
        {
            if (this == null || loadVersion != _iconLoadVersion || _iconImage == null || !_iconImage)
            {
                return;
            }

            Sprite sprite = obj as Sprite;
            if (sprite != null)
            {
                _iconImage.sprite = sprite;
            }
        });
    }

    public void SelectThisItem()
    {
        if (_imageTweener != null)
        {
            _imageTweener.Goto(0.2f, true);
            _imageTweener.Pause();
        }
        if (_sTweener != null)
        {
            _sTweener.Goto(0.2f, true);
            _sTweener.Pause();
        }
        if (_itemImage != null) _itemImage.enabled = false;
        if (_maskImage != null) _maskImage.enabled = true;
    }

    public void CancelThisItem()
    {
        if (_itemImage != null)
        {
            _itemImage.enabled = true;
            _itemImage.color = _originalColor;
        }
        if (_maskImage != null)
        {
            _maskImage.enabled = false;
        }

        transform.localScale = _originalScale;

        try
        {
            _imageTweener?.Rewind();
            _imageTweener?.Pause();
        }
        catch { /* ignore if tween state invalid */ }

        try
        {
            _sTweener?.Rewind();
            _sTweener?.Pause();
        }
        catch { /* ignore if tween state invalid */ }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_maskImage != null && _maskImage.enabled) return;
        _sTweener?.PlayForward();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_maskImage != null && _maskImage.enabled) return;
        _sTweener?.PlayBackwards();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClicked?.Invoke(this);
    }
}


