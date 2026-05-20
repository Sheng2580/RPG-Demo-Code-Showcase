using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActiveSelector: Selector
{
    protected override EStatus OnUpdate()
    {
        var prev = currentChild;
        base.OnInitialize();
        var res = base.OnUpdate();
        /*
        鍙涓嶆槸閬嶅巻缁撴潫鎴栧彲鎵ц鑺傜偣涓嶅彉锛岄兘搴旇涓柇涓婁竴娆℃墽琛岀殑鑺傜偣锛屾棤璁轰紭鍏堟槸楂樻槸浣庛€?
        鍥犱负濡傛灉褰撳墠浼樺厛绾ф瘮涔嬪墠鐨勯珮锛岀悊搴斾腑鏂箣鍓嶇殑锛?
        鑰屽鏋滄瘮涔嬪墠鐨勪綆锛岄偅灏辫瘉鏄庝箣鍓嶉珮浼樺厛绾х殑琛屼负鏃犳硶缁х画浜嗭紝
        鍚﹀垯鎬庝箞浼氱瓑鍒扮幇鍦ㄧ殑浣庝紭鍏堢骇鐨勮涓哄憿锛熸墍浠ヤ篃搴斾腑鏂畠銆?
        */
        if(prev != null && currentChild != prev)
            prev.Value.Abort();
        return res;
    }
}

