
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Demo1 : MonoBehaviour
{
    public GameObject deviceScanResultProto;
    Transform scanResultRoot;

    // Start is called before the first frame update
    BLE ble;
    BLE.BLEScan scan;
    public bool isScanning = false, isConnected = false;
    string deviceId = null;
    IDictionary<string, string> discoveredDevices = new Dictionary<string, string>();
    int devicesCount = 0;
    List<string> devices = new List<string>();
    int index = 0;

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
        remoteAngle = packageReceived[1];
        Debug.Log("HR: " + remoteAngle);
        //Thread.Sleep(100);
    }

    // Update is called once per frame
    void Update()
    {
        if (isScanning)
        {
            devicesCount = discoveredDevices.Count;
        }
        if (devices.Count != 0 && connectionThread == null) {
            Debug.Log(devices);
            index = 0;
            StartConHandler();
        }

        if (deviceId != null && deviceId != "-1")
        {
            GameObject g = Instantiate(deviceScanResultProto, scanResultRoot);
            g.name = deviceId;
            g.transform.GetChild(0).GetComponent<Text>().text = discoveredDevices[deviceId];
            g.transform.GetChild(1).GetComponent<Text>().text = deviceId;
            deviceId = null;
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
            Debug.Log("found device with name: " + deviceName);

            
            if (!discoveredDevices.ContainsKey(_deviceId))
            {
                
                //devices.Add(_deviceId);
                //StartConHandler();
                devices.Add(_deviceId);
            
                discoveredDevices.Add(_deviceId, deviceName);
                
                
            }
            
            

            //if (deviceId == null && deviceName == targetDeviceName)
             //   deviceId = _deviceId;

        };

        scan.Finished = () =>
        {
            isScanning = false;
            Debug.Log("scan finished");
            if (deviceId == null)
                deviceId = "-1";
        };
        while (deviceId == null || connectionThread != null)
            Thread.Sleep(1000);
        scan.Cancel();
        scanningThread = null;
        isScanning = false;
        isScanning = false;

        if (deviceId == "-1")
        {
            Debug.Log("no device found!");
            return;
        }
    }

    public void StartConHandler()
    {
        connectionThread = new Thread(ConnectBleDevice);
        connectionThread.Start();
        
    }
    void ConnectBleDevice()
    {
        print("connectThreatenters Ble");
        Debug.Log("cheacking device: " + discoveredDevices[devices[index]]);
        if (ble.CheackServices(devices[index]))
        {
            Debug.Log("Devicefound with 180d: " + discoveredDevices[devices[index]]);
            deviceId= devices[index];
        }
        else
            Debug.Log("Device " + discoveredDevices[devices[index]] + " doesn't have 180d");
        devices.RemoveAt(index);
        index++;
        connectionThread = null;

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
}
 
