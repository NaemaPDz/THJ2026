using System;

[Serializable]
public class ItemInstance
{
	public ItemData Data { get; }

	public ItemInstance(ItemData data)
	{
		if (data == null)
			throw new ArgumentNullException(nameof(data));

		Data = data;
	}
}