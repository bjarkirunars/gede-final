using UnityEngine;

[CreateAssetMenu(fileName = "NewHeartRateCharacteristic", menuName = "Heart Rate Characteristic")]
public class HeartRateCharacteristic : ScriptableObject
{
    public string hrName;
    public string serviceId;
    public string characteristicId;
}
