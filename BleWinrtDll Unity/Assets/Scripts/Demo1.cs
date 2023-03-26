
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class Demo1 : MonoBehaviour
{
    public GameObject deviceScanResultProto;
    Transform scanResultRoot;
    public HeartRateCharacteristic heartRateCharacteristic;
    public Text subcribeText;

    // Start is called before the first frame update
    BLE ble;
    BLE.BLEScan scan;
    public bool isScanning = false, isConnected = false;
    string deviceId = null;
    IDictionary<string, string> discoveredDevices = new Dictionary<string, string>();
    int devicesCount = 0;
    List<string> devices = new List<string>();
    List<string> donecheck = new List<string>();
    int index = 0;
    bool added = false;
    string devid;
    string[] characteristic;
    bool connecting = false;

    // BLE Threads 
    Thread scanningThread, connectionThread, readingThread;
    int remoteAngle, lastRemoteAngle;
    void Start()
    {
        scanResultRoot = deviceScanResultProto.transform.parent;
        deviceScanResultProto.transform.SetParent(null);
        ble = new BLE();
        readingThread = new Thread(ReadBleData);
        StartScanHandler();
    }


    private void ReadBleData(object obj)
    {
        byte[] packageReceived = BLE.ReadBytes();
        // Convert little Endian.
        // In this example we're interested about an angle
        // value on the first field of our package.
        if (packageReceived != null && packageReceived.Length > 1)
        {
            remoteAngle = packageReceived[1];
        }
        //Thread.Sleep(100);
    }

    // Update is called once per frame
    void Update()
    {
        if (isScanning)
        {
            devicesCount = discoveredDevices.Count;
            if (deviceId != null && deviceId != "-1" && added)
            {
                GameObject g = Instantiate(deviceScanResultProto, scanResultRoot);
                g.name = deviceId;
                g.transform.GetChild(0).GetComponent<Text>().text = discoveredDevices[deviceId];
                g.transform.GetChild(1).GetComponent<Text>().text = deviceId;
                //deviceId = null;
                added = false;
            }
        }
        else
        {
            if (deviceId != null && deviceId != "-1")
            {
                if (ble.isConnected)
                {
                    UpdateGuiText("writeData");
                }
                if (!ble.isConnected)
                {
                    if (scanningThread == null && connectionThread == null && !connecting)
                    {
                        connecting = true;
                        StartConHandler();
                    }
                }
                else
                {
                    connecting = false;
                }
            }


        }
    }
    void UpdateGuiText(string action)
    {
        switch (action)
        {
            case "scan":
                
                foreach (KeyValuePair<string, string> entry in discoveredDevices)
                {
                    Debug.Log("DeviceID: " + entry.Key + "\nDeviceName: " + entry.Value + "\n\n");
                    Debug.Log("Added device: " + entry.Key);
                }
                break;
            case "connected":
               
                Debug.Log("Connected to target device:\n" + discoveredDevices[deviceId]);
                break;
            case "writeData":
                if (!readingThread.IsAlive)
                {
                    readingThread = new Thread(ReadBleData);
                    readingThread.Start();
                }
                if (remoteAngle != lastRemoteAngle)
                {
                    subcribeText.text = remoteAngle + "bpm";
                    lastRemoteAngle = remoteAngle;
                }
                break;
        }
    }


    public void StartScanHandler()
    {
        devicesCount = 0;
        isScanning = true;
        discoveredDevices.Clear();
        scanningThread = new Thread(ScanBleDevices);
        scanningThread.Start();
        
        Debug.Log("Scanning...");
    }
    public void ResetHandler()
    {
        
        // Reset previous discovered devices
        discoveredDevices.Clear();
        
        deviceId = null;
        CleanUp();
    }

    void ScanBleDevices()
    {
        scan = BLE.ScanDevices();
        Debug.Log("BLE.ScanDevices() started.");
        scan.Found = (_deviceId, deviceName) =>
        {
            // Debug.Log("found device with name: " + deviceName);
            if (!discoveredDevices.ContainsKey(_deviceId))
            {
                devices.Add(_deviceId);
                devid = _deviceId;
                discoveredDevices.Add(_deviceId, deviceName);
                ConnectBleDevice();
            }
        };

        scan.Finished = () =>
        {
            isScanning = false;
            Debug.Log("scan finished");
            if (deviceId == null)
                deviceId = "-1";
        };
        while (deviceId == null)
            Thread.Sleep(500);
        scan.Cancel();
        scanningThread = null;
        isScanning = false;
        if (deviceId == "-1")
        {
            Debug.Log("no device found!");
            return;
        }
    }

    public void StartConHandler()
    {
        connectionThread = new Thread(ConnectDevice);
       
        connectionThread.Start();
        connectionThread.Join();
    }


    public void ConnectDevice() {
        Debug.Log($" in connecting");
        if (heartRateCharacteristic.hrName != null)
        {
            Debug.Log($" device {heartRateCharacteristic.hrName}");
            Debug.Log($" service {heartRateCharacteristic.serviceId}");

            try
            {
                foreach (string characteristicUuid in characteristic)
                {
                    if (string.IsNullOrEmpty(characteristicUuid)) continue;
                    heartRateCharacteristic.characteristicId = characteristicUuid;
                }
                ble.Connect(heartRateCharacteristic.hrName, heartRateCharacteristic.serviceId, heartRateCharacteristic.characteristicId);
            }
            catch (Exception e)
            {
                Debug.Log("Could not establish connection to device with ID " + heartRateCharacteristic.hrName + $" service {heartRateCharacteristic.serviceId}" + e);
            }
        }
        if (ble.isConnected)
            Debug.Log("Connected to: " + discoveredDevices[deviceId]);

    }
    void ConnectBleDevice()
    {
        // print("connectThreatenters Ble");
        Tuple<string, string[]> ids = ble.CheckServices(devid);

        if (ids.Item1 != "not found")
        {
            Debug.Log("Devicefound with 180d: " + discoveredDevices[devid]);
            deviceId = devid;
            heartRateCharacteristic.hrName = devid;
            heartRateCharacteristic.serviceId = ids.Item1;
            characteristic = ids.Item2;
            added = true;
        }
        donecheck.Add(heartRateCharacteristic.hrName);
    }

    private void OnDestroy()
    {
        CleanUp();
    }

    private void OnApplicationQuit()
    {
        CleanUp();
    }

    // Prevent threading issues and free BLE stack.
    // Can cause Unity to freeze and lead
    // to errors when omitted.
    private void CleanUp()
    {
        try
        {
            scan.Cancel();
            ble.Close();
            scanningThread.Abort();
            connectionThread.Abort();
        }
        catch (NullReferenceException e)
        {
            Debug.Log("Thread or object never initialized.\n" + e);
        }
    }

    public void GoToLevel()
    {
        SceneManager.LoadScene("UIHeartrate");
    }
}
 
