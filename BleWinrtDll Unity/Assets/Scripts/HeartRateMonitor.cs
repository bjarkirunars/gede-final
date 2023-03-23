using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class HeartRateMonitor : MonoBehaviour
{
    public HeartRateCharacteristic heartRateCharacteristic;
    public Text subcribeText;

    void Start()
    {
        // Get the service ID and characteristic ID from the HeartRateCharacteristic object
        string selectedDeviceId = heartRateCharacteristic.hrName;
        string selectedServiceId = heartRateCharacteristic.serviceId;
        string selectedCharacteristicId = heartRateCharacteristic.characteristicId;

        BleApi.SubscribeCharacteristic(selectedDeviceId, selectedServiceId, selectedCharacteristicId, false);
        

        // Use the IDs to connect to the heart rate monitor
        // ...
    }

    void Update()
    {
        BleApi.BLEData res = new BleApi.BLEData();
        while (BleApi.PollData(out res, false))
        {
            subcribeText.text = "HR: " + Convert.ToInt32(BitConverter.ToString(res.buf, 1, 1), 16);
        }
    }
}
