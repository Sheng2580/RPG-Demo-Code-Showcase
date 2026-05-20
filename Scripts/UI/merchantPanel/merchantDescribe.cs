using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class merchantDescribe : MonoBehaviour
{
   public Text commodityText;
   public Text commodityDescribeText;
   public Image commodityImage;
   public Text commodityDetailedInformationText;
   public Button merchantButton;

   private int iconLoadVersion;

   public void SetDescribe(commodityClass commodity, UnityAction onBuy = null)
   {
      if (commodity == null)
      {
         Debug.LogWarning("[merchantDescribe] commodity is null");
         return;
      }

      if (commodityText != null)
      {
         commodityText.text = commodity.CommodityName;
      }

      if (commodityDescribeText != null)
      {
         commodityDescribeText.text = commodity.CommodityType == 1 ? "Buff" : (commodity.CommodityType == 2 ? "閬撳叿" : "鏈煡");
      }

      if (commodityDetailedInformationText != null)
      {
         commodityDetailedInformationText.text = commodity.CommodityDetailedInformationText;
      }

      if (commodityImage != null && !string.IsNullOrEmpty(commodity.CommodityImageName))
      {
         int loadVersion = ++iconLoadVersion;
         ABManager.Instance.LoadResAsync("icon", commodity.CommodityImageName, typeof(Sprite), obj =>
         {
            if (this == null || loadVersion != iconLoadVersion || commodityImage == null || !commodityImage)
            {
               return;
            }

            if (obj != null)
            {
               commodityImage.sprite = obj as Sprite;
            }
            else
            {
               Debug.LogWarning($"[merchantDescribe] Load icon failed: {commodity.CommodityImageName}");
            }
         });
      }

      if (merchantButton != null)
      {
         merchantButton.onClick.RemoveAllListeners();
         if (onBuy != null)
         {
            merchantButton.onClick.AddListener(onBuy);
            MusicManager.Instance.PlaySoundForAB("selece1");
         }
      }
   }

   private void OnDisable()
   {
      iconLoadVersion++;
      if (merchantButton != null)
      {
         merchantButton.onClick.RemoveAllListeners();
      }
   }
}


