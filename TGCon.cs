using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Text;
using System.IO;
using UnityEngine;
using System;

public class TGCon : MonoBehaviour
{
    ////classes for storing data by parsing JSON string
    [System.Serializable]
    public class rawEEG
    {
        public int raw = 0;
    }

    [System.Serializable]
    public class EEGPower
    {
        public int delta = 0;
        public int theta = 0;
        public int lowAlpha = 0;
        public int highAlpha = 0;
        public int lowBeta = 0;
        public int highBeta = 0;
        public int lowGamma = 0;
        public int highGamma = 0;
    }

    [System.Serializable]
    public class ESense
    {
        public int attention = 0;
        public int meditation = 0;
    }

    [System.Serializable]
    public class TGData
    {
        public ESense eSense = null;
        public EEGPower eegPower = null;
        public int poorSignalLevel = 0;
    }

    public TGData tgd;

    public int ThreadSleepTime = 50;//time to wait for the subThread to aquire next chunk of data from stream. Better to set this small enough (or set the buffer size large enough) because the stream data may grow larger than the buffer size. 
    private int bufferSize = 2048;///def for size of the buffer which holds the streamed string (from think gear connector)
    public bool showDataOnConsole = false;

    /////// recording data to text file:: simply dumping JSON string to file....thus the file may grow huge in its size...
    public bool rec_eSense = false;/// set true if you want to record eSense data to text file 
    public bool rec_raw = false;/// set true if you want to record raw EEG data to text file
    StreamWriter sw;// Stream writer for recording eSense data
    StreamWriter ew;// Stream writer for recording raw data
    private uint eSenseCount = 0;// counter for number of eSense data 
    private uint eegCount = 0;//counter for number of raw data

    private TcpClient client;
    private Stream stream;
    private byte[] buffer;
    private bool keepOnRunning = true;



    public TGCon()
    {
        //Constructor
    }

    void Start()
    {
        ThreadStart ts = new ThreadStart(Connect);
        Thread thread = new Thread(ts);
        thread.Start();

        if (rec_eSense)
        {
            sw = new StreamWriter("./Assets/eSenseData.txt", true);
            sw.Write(DateTime.Now + "\n");
        }
        if (rec_raw)
        {
            ew = new StreamWriter("./Assets/raw.txt", true);
            ew.Write(DateTime.Now + "\n");
        }
    }

    public void Connect()
    {
        client = new TcpClient("127.0.0.1", 13854);// -> ThinkGear Connector
        stream = client.GetStream();
        buffer = new byte[bufferSize];
        byte[] writeBuffer = Encoding.ASCII.GetBytes(@"{""enableRawOutput"": true, ""format"": ""Json""}");
        stream.Write(writeBuffer, 0, writeBuffer.Length);
        while (keepOnRunning)
        {
            /////main loop
            ParseData();
            Thread.Sleep(ThreadSleepTime);
        }

        Disconnect();
    }

    private void ParseData()
    {
        //gather data string from stream and parse JSON.
        if (stream.CanRead)
        {
            try
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string packet = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                ////////////////////////////////////////////////////////
                StringReader Rdr = new StringReader(packet);
                while (true)
                {
                    string ol = Rdr.ReadLine();
                    if (ol != null)
                    {
                        if (ol.Contains("rawEeg"))
                        {
                            rawEEG raw = JsonUtility.FromJson<rawEEG>(ol);
                            if (rec_raw && keepOnRunning)
                            {
                                ew.Write(eegCount + ", " + ol + "\n");
                            }
                            eegCount++;
                        }
                        if (ol.Contains("eSense"))
                        {
                            tgd = JsonUtility.FromJson<TGData>(ol);

                            if (rec_eSense && keepOnRunning)
                            {
                                sw.Write(eSenseCount + ", " + ol + "\n");
                            }

                            eSenseCount++;

                            if (showDataOnConsole)
                            {
                                Debug.Log("ATT:" + tgd.eSense.attention + " MED:" + tgd.eSense.meditation + " Del:" + tgd.eegPower.delta + " The:" + tgd.eegPower.theta + " lAl:" + tgd.eegPower.lowAlpha + " hAl:" + tgd.eegPower.highAlpha +
                                    " lBe:" + tgd.eegPower.lowBeta + " hBe:" + tgd.eegPower.highBeta + " lGa:" + tgd.eegPower.lowGamma + " hGa:" + tgd.eegPower.highGamma);
                            }
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            catch (IOException e)
            {
                Debug.Log("IOException: " + e);
            }
        }
    }

    private void Disconnect()
    {
        Task.Delay(ThreadSleepTime * 2);
        if (rec_eSense)
        {
            sw.Flush();
            sw.Close();
        }
        if (rec_raw)
        {
            ew.Flush();
            ew.Close();
        }
        stream.Close();
    }

    private void OnDestroy()
    {
        keepOnRunning = false;
    }
    private void OnDisable()
    {
        keepOnRunning = false;
    }
    private void OnApplicationQuit()
    {
        keepOnRunning = false;
    }
}
