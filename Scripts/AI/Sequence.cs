using System.Collections.Generic;

public class Sequence : Composite
{
    protected LinkedListNode<Behavior> currentChild;
    protected override void OnInitialize()
    {
        currentChild = children.First;
    }
    protected override EStatus OnUpdate()
    {
        while(true)
        {
            var s = currentChild.Value.Tick();
            /*
            濡傛灉瀛愯妭鐐硅繍琛岋紝杩樻病鏈夋垚鍔燂紝灏辩洿鎺ヨ繑鍥炶缁撴灉銆?
            鏄€岃繍琛屼腑銆嶉偅灏辫〃鏄庢湰鑺傜偣涔熸槸杩愯涓紝鏈夎褰曞綋鍓嶈妭鐐癸紝涓嬫杩樹細缁х画鎵ц锛?
            鏄€屽け璐ャ€嶅氨琛ㄦ槑鏈妭鐐逛篃杩愯澶辫触浜嗭紝涓嬫浼氬啀缁忓巻OnInitialize锛屼粠澶村紑濮嬨€?
            */
            if( s != EStatus.Success)
                return s;
            currentChild = currentChild.Next;
            if(currentChild == null)
                return EStatus.Success;
        }
    }
}

