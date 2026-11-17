using UnityEngine;

namespace QuickDraw.Scripts
{
    // World Space UI. 버튼(START / MENU)은 총으로 쏴서 누른다.
    public class UI : MonoBehaviour, IShootable
    {
        public void Shooted()
        {
            Debug.Log("플레이어가 해당 UI를 쐈을 시 그에 대응하는 함수가 실행된다.");
            
        }
    }
}
