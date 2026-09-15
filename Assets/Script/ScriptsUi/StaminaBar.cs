using UnityEngine;
using UnityEngine.UI;

public class StaminaBar : MonoBehaviour
{
    [SerializeField]
    private Slider staminaSlider;

    // 외부에서 값 받아서 표시만
    public void SetValue(float current, float max)
    {
        if (max <= 0)
            return;
        staminaSlider.value = current / max;
    }
}
