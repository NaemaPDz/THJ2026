using TMPro;
using UnityEngine;

public class HeartRateUI : MonoBehaviour
{
	public HeartRateSystem heartRate;
	public TMP_Text bpmText;

	public Color normalColor = Color.white;
	public Color warningColor = Color.yellow;
	public Color dangerColor = Color.red;

	private void Update()
	{
		if (heartRate == null || bpmText == null)
			return;

		float bpm = heartRate.CurrentBPM;

		bpmText.text = $"{Mathf.RoundToInt(bpm)} BPM";

		if (bpm >= heartRate.maxBPM - 30)
		{
			bpmText.color = dangerColor;
		}
		else if (bpm >= heartRate.recoverBPM)
		{
			bpmText.color = warningColor;
		}
		else
		{
			bpmText.color = normalColor;
		}
	}
}
