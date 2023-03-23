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
    private bool isScanningDevices = false;
    private bool isScanningServices = false;
    private bool isScanningCharacteristics = false;
    private bool isSubscribed = false;
    public Text deviceScanButtonText;
    public GameObject deviceScanResultProto;
    public Button serviceScanButton;
    public Button gameButton;
    public Text subcribeText;
    public HeartRateCharacteristic heartRateCharacteristic;


    Transform scanResultRoot;
    Dictionary<string, Dictionary<string, string>> devices = new Dictionary<string, Dictionary<string, string>>();

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
                    // Connect if the service is the heart rate service
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
                    // Connect to the first one available. Usually works, but maybe not always?
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
                subcribeText.text = Convert.ToInt32(BitConverter.ToString(res.buf, 1, 1), 16) + "bpm";
            }
            gameButton.interactable = true;
        }
        {
            // log potential errors
            BleApi.ErrorMessage res = new BleApi.ErrorMessage();
            BleApi.GetError(out res);
            UnityEngine.Debug.LogError(res.msg);
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
        serviceScanButton.interactable = true;
        heartRateCharacteristic.hrName = data.name;
    }

    public void StartServiceScan()
    {
        if (!isScanningServices)
        {
            // start new scan
            BleApi.ScanServices(heartRateCharacteristic.hrName);
            isScanningServices = true;
        }
    }

    public void SelectService(string serviceuuid)
    {
        heartRateCharacteristic.serviceId = serviceuuid;
    }

    public void StartCharacteristicScan()
    {
        if (!isScanningCharacteristics)
        {
            // start new scan
            BleApi.ScanCharacteristics(heartRateCharacteristic.hrName, heartRateCharacteristic.serviceId);
            isScanningCharacteristics = true;
        }
    }

    public void SelectCharacteristic(string uuid)
    {
        heartRateCharacteristic.characteristicId = uuid;
    }

    public void Subscribe()
    {
        // no error code available in non-blocking mode
        BleApi.SubscribeCharacteristic(heartRateCharacteristic.hrName, heartRateCharacteristic.serviceId, heartRateCharacteristic.characteristicId, false);
        isSubscribed = true;
    }

    public void GoToLevel()
    {
        SceneManager.LoadScene("Level");
    }
}
