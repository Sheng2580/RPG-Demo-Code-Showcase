

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
public class BasePanel : MonoBehaviour
{
    protected bool isShow = false;
    protected Button closeBtn;

    public virtual void Awake()
    {
        Transform closeBtnTrans = transform.Find("CloseBtn");
        if (closeBtnTrans != null)
        {
            closeBtn = closeBtnTrans.GetComponent<Button>();
            if (closeBtn != null) 
                closeBtn.onClick.AddListener(Hide);
        }
    }

    public virtual void Show()
    {
        if (isShow) return;
        gameObject.SetActive(true);
        isShow = true;
    }

    public virtual void Hide()
    {
        if (!isShow) return;
        gameObject.SetActive(false);
        isShow = false;
    }

    public bool IsShow() => isShow;
}


