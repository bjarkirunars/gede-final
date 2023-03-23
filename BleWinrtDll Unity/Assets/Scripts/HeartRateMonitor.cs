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

        // Subscribe to the heartrate monitor again
        BleApi.SubscribeCharacteristic(selectedDeviceId, selectedServiceId, selectedCharacteristicId, false);
        
    }

    void Update()
    {
        // Use the value from the heart rate monitor to display on screen
        BleApi.BLEData res = new BleApi.BLEData();
        while (BleApi.PollData(out res, false))
        {
            subcribeText.text = Convert.ToInt32(BitConverter.ToString(res.buf, 1, 1), 16) + "bpm";
        }
    }
}
