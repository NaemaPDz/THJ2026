using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HotbarSlotUI : MonoBehaviour
{
	[SerializeField] private Image slotIcons;
	[SerializeField] private GameObject slotHighlights;
	[SerializeField] private TextMeshProUGUI itemCountText;

	private WeaponInstance subscribedWeapon;

	public void SetSlotInfo(ItemInstance item)
	{
		// เลิกฟังอาวุธชิ้นเดิมก่อน
		UnsubscribeAmmoEvent();

		slotIcons.sprite = item != null ? item.Data.icon : null;
		slotIcons.enabled = item != null && item.Data.icon != null;

		if (item is WeaponInstance weapon)
		{
			if (weapon.WeaponData.unlimitedAmmo)
			{
				itemCountText.gameObject.SetActive(false);
			}
			else
			{
				itemCountText.gameObject.SetActive(true);

				// แสดงค่าปัจจุบันทันที
				UpdateAmmoText(weapon.CurrentAmmo);

				// ฟัง Event จากอาวุธชิ้นนี้
				subscribedWeapon = weapon;
				subscribedWeapon.OnAmmoChanged += UpdateAmmoText;
			}
		}
		else
		{
			itemCountText.gameObject.SetActive(false);
		}
	}

	public void SetSelected(bool isSelected)
	{
		slotHighlights.SetActive(isSelected);
	}

	private void UpdateAmmoText(int currentAmmo)
	{
		itemCountText.text = currentAmmo.ToString();
	}

	private void UnsubscribeAmmoEvent()
	{
		if (subscribedWeapon == null)
			return;

		subscribedWeapon.OnAmmoChanged -= UpdateAmmoText;
		subscribedWeapon = null;
	}

	private void OnDestroy()
	{
		UnsubscribeAmmoEvent();
	}
}
