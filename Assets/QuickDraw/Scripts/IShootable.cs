using UnityEngine;

namespace QuickDraw.Scripts
{
    // 총에 맞을 수 있는 물체 (표적, UI 버튼). hit = 총의 조준 레이캐스트가 맞은 지점
    public interface IShootable
    {
        void Shooted(RaycastHit hit);
    }
}
