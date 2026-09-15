using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConsumableSlotUI : MonoBehaviour
{
    [SerializeField]
    private Image itemIcon;

    [SerializeField]
    private TMP_Text countText;

    public void SetItem(ItemData data, int count)
    {
        itemIcon.sprite = data != null ? data.icon : null;
        SetCount(count);
    }

    public void SetCount(int count)
    {
        itemIcon.gameObject.SetActive(count > 0);
        countText.gameObject.SetActive(count > 0);
        countText.text = count.ToString();
    }
}
