using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class Demo : MonoBehaviour
{
    public bool isScanningDevices = false;
    public bool isScanningServices = false;
    public bool isScanningCharacteristics = false;
    public bool isSubscribed = false;
    public Text deviceScanButtonText;
    public GameObject deviceScanResultProto;
    public Button serviceScanButton;
    public Button gameButton;
    public Text subcribeText;
    public Text errorText;
    public HeartRateCharacteristic heartRateCharacteristic;


    Transform scanResultRoot;
    public string selectedDeviceId;
    public string selectedServiceId;
    public string selectedCharacteristicId;
    Dictionary<string, Dictionary<string, string>> devices = new Dictionary<string, Dictionary<string, string>>();
    string lastError;

    // Start is called before the first frame update
    void Start()
    {
        gameButton.interactable = false;
        scanResultRoot = deviceScanResultProto.transform.parent;
        deviceScanResultProto.transform.SetParent(null);
    }

    // Update is called once per frame
    void Update()
    {
        BleApi.ScanStatus status;
        if (isScanningDevices)
        {
            BleApi.DeviceUpdate res = new BleApi.DeviceUpdate();
            do
            {
                status = BleApi.PollDevice(ref res, false);
                if (status == BleApi.ScanStatus.AVAILABLE)
                {
                    if (!devices.ContainsKey(res.id))
                        devices[res.id] = new Dictionary<string, string>() {
                            { "name", "" },
                            { "isConnectable", "False" }
                        };
                    if (res.nameUpdated)
                        devices[res.id]["name"] = res.name;
                    if (res.isConnectableUpdated)
                        devices[res.id]["isConnectable"] = res.isConnectable.ToString();
                    // consider only devices which have a name and which are connectable
                    if (devices[res.id]["name"] != "" && devices[res.id]["isConnectable"] == "True")
                    {
                        // add new device to list
                        GameObject g = Instantiate(deviceScanResultProto, scanResultRoot);
                        g.name = res.id;
                        g.transform.GetChild(0).GetComponent<Text>().text = devices[res.id]["name"];
                        g.transform.GetChild(1).GetComponent<Text>().text = res.id;
                    }
                }
                else if (status == BleApi.ScanStatus.FINISHED)
                {
                    isScanningDevices = false;
                    deviceScanButtonText.text = "Scan devices";
                }
            } while (status == BleApi.ScanStatus.AVAILABLE);
        }
        if (isScanningServices)
        {
            subcribeText.text = "Loading..";
            BleApi.Service res = new BleApi.Service();
            do
            {
                status = BleApi.PollService(out res, false);
                if (status == BleApi.ScanStatus.AVAILABLE)
                {
                    if (res.uuid.Contains("180d"))
                    {
                        SelectService(res.uuid);
                        StartCharacteristicScan();
                    }
                }
                else if (status == BleApi.ScanStatus.FINISHED)
                {
                    isScanningServices = false;
                    serviceScanButton.interactable = true;
                }
            } while (status == BleApi.ScanStatus.AVAILABLE);
        }
        if (isScanningCharacteristics)
        {
            BleApi.Characteristic res = new BleApi.Characteristic();
            do
            {
                status = BleApi.PollCharacteristic(out res, false);
                if (status == BleApi.ScanStatus.AVAILABLE)
                {
                    SelectCharacteristic(res.uuid);
                    Subscribe();
                }
                else if (status == BleApi.ScanStatus.FINISHED)
                {
                    isScanningCharacteristics = false;
                }
            } while (status == BleApi.ScanStatus.AVAILABLE);
        }
        if (isSubscribed)
        {
            BleApi.BLEData res = new BleApi.BLEData();
            while (BleApi.PollData(out res, false))
            {
                subcribeText.text = "HR: " + Convert.ToInt32(BitConverter.ToString(res.buf, 1, 1), 16);
            }
            gameButton.interactable = true;
        }
        {
            // log potential errors
            BleApi.ErrorMessage res = new BleApi.ErrorMessage();
            BleApi.GetError(out res);
            if (lastError != res.msg)
            {
                UnityEngine.Debug.LogError(res.msg);
                errorText.text = res.msg;
                lastError = res.msg;
            }
        }
    }

    private void OnApplicationQuit()
    {
        BleApi.Quit();
    }

    public void StartStopDeviceScan()
    {
        if (!isScanningDevices)
        {
            // start new scan
            for (int i = scanResultRoot.childCount - 1; i >= 0; i--)
                Destroy(scanResultRoot.GetChild(i).gameObject);
            BleApi.StartDeviceScan();
            isScanningDevices = true;
            deviceScanButtonText.text = "Stop scan";
        }
        else
        {
            // stop scan
            isScanningDevices = false;
            BleApi.StopDeviceScan();
            deviceScanButtonText.text = "Start scan";
        }
    }

    public void SelectDevice(GameObject data)
    {
        for (int i = 0; i < scanResultRoot.transform.childCount; i++)
        {
            var child = scanResultRoot.transform.GetChild(i).gameObject;
            child.transform.GetChild(0).GetComponent<Text>().color = child == data ? Color.red :
                deviceScanResultProto.transform.GetChild(0).GetComponent<Text>().color;
        }
        selectedDeviceId = data.name;
        serviceScanButton.interactable = true;
        heartRateCharacteristic.hrName = data.name;
    }

    public void StartServiceScan()
    {
        if (!isScanningServices)
        {
            // start new scan
            BleApi.ScanServices(selectedDeviceId);
            isScanningServices = true;
        }
    }

    public void SelectService(string serviceuuid)
    {
        selectedServiceId = serviceuuid;
        heartRateCharacteristic.serviceId = serviceuuid;
    }

    public void StartCharacteristicScan()
    {
        if (!isScanningCharacteristics)
        {
            // start new scan
            BleApi.ScanCharacteristics(selectedDeviceId, selectedServiceId);
            isScanningCharacteristics = true;
        }
    }

    public void SelectCharacteristic(string uuid)
    {
        selectedCharacteristicId = uuid;
        heartRateCharacteristic.characteristicId = uuid;
    }

    public void Subscribe()
    {
        // no error code available in non-blocking mode
        BleApi.SubscribeCharacteristic(selectedDeviceId, selectedServiceId, selectedCharacteristicId, false);
        isSubscribed = true;
    }

    public void GoToLevel()
    {
        SceneManager.LoadScene("Level");
    }
}
