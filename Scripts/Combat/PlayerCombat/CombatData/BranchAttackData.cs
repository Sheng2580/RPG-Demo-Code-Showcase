using UnityEngine;

[CreateAssetMenu(menuName = "Configs/BranchAttackData")]
public class BranchAttackData : ScriptableObject
{
    /* 鍚庢憞鏃堕棿锛岃秴杩囧悗鍏佽绉诲姩鎵撴柇鏀诲嚮銆?*/ public float cdTime;
    /* 鏀诲嚮鍚嶇О锛屽彧鐢ㄤ簬鍦ㄩ厤缃潰鏉夸腑璇嗗埆銆?*/ public string attackName;
    /* 杩炴嫑绐楀彛寮€鍚椂闂达紝瓒呰繃鍚庡彲浠ユ帴鍥炰笅涓€娈垫櫘閫氭敾鍑汇€?*/ public float nextAttackTime;
    /* 鏀诲嚮缁撴潫鏃堕棿锛屽姩鐢?normalizedTime 瓒呰繃鍚庢病鏈夎緭鍏ュ氨鍥?Idle銆?*/ public float endTime = 0.95f;
    /* 璇ュ垎鏀敾鍑绘鐨勫懡涓垽瀹氭椂闂寸偣銆?*/ public TriggerHit[] triggerHits;
    /* 瀵瑰簲 Animator 閲岀殑鍔ㄧ敾鐘舵€佸悕銆?*/ public string attackAnimationName;
    /* 鍒嗘敮鏀诲嚮浣嶇Щ璺濈锛屾湁閿佸畾鐩爣鏃舵湞鐩爣绐佽繘锛屽惁鍒欐湞瑙掕壊鍓嶆柟浣嶇Щ銆?*/ public float displacement;

     public float displacementTime;
    public float repelDistance;

    public float enemyHitStunDuration = 0.8f;
}


