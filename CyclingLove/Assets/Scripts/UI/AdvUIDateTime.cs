using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class AdvUIDateTime : MonoBehaviour
    {
        [SerializeField] private RectTransform left;
        [SerializeField] private RectTransform right;
        
        [SerializeField] private TextMeshProUGUI date;
        [SerializeField] private TextMeshProUGUI timing;

        [SerializeField] private Image weatherImage;

        [SerializeField] private Sprite weatherSunny;
        [SerializeField] private Sprite weatherRain;
        [SerializeField] private Sprite weatherCloudy;
        [SerializeField] private Sprite weatherPartlySunny;
        
        [SerializeField] private TextMeshProUGUI location;

        public void Show()
        {
        }

        public void Hide()
        {
        }
    }
}