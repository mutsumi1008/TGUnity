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
    ////classes for storing data from parsed JSON string
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

    public int ThreadSleepTime = 50;//time to wait for the subThread to aquire next chunk of data from the stream. Better to set this small enough (or set the buffer size large enough) because the data chunk may grow larger than the buffer size. 
    private int bufferSize = 2048;///def for size of the buffer which holds the streamed string (from think gear connector)
    public bool showDataOnConsole = false;//If you want to see the data gathered on console (eSense data only)

    /////// recording data to text file:: simply dumping JSON string to file....thus the file may grow huge in its size...
    public bool rec_eSense = false;/// set true if you want to record eSense data to text file 
    public bool rec_raw = false;/// set true if you want to record raw EEG data to text file
    StreamWriter eSenseOut;// Stream writer for recording eSense data
    StreamWriter rawOut;// Stream writer for recording raw data
    private uint eSenseCount = 0;// counter for number of eSense data 
    private uint eegCount = 0;//counter for number of raw data

    private TcpClient client;
    private Stream stream;
    private byte[] buffer;
    private bool keepOnRunning = true;

    private Thread thread;
    private ThreadStart ts;

    public TGCon()
    {
        //Constructor
    }

    void Start()
    {
        ts = new ThreadStart(Connect);
        thread = new Thread(ts);
        thread.Start();

        if (rec_eSense)
        {
            eSenseOut = new StreamWriter("./Assets/eSenseData.txt", true);
            eSenseOut.Write(DateTime.Now + "\n");
        }
        if (rec_raw)
        {
            rawOut = new StreamWriter("./Assets/raw.txt", true);
            rawOut.Write(DateTime.Now + "\n");
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
                StringReader Rdr = new StringReader(packet);
                while (true)
                {
                    string dataLine = Rdr.ReadLine();
                    if (dataLine != null)
                    {
                        if (dataLine.Contains("rawEeg"))
                        {
                            rawEEG raw = JsonUtility.FromJson<rawEEG>(dataLine);
                            if (rec_raw && keepOnRunning)
                            {
                                rawOut.Write(eegCount + ", " + dataLine + "\n");
                            }
                            eegCount++;
                        }
                        if (dataLine.Contains("eSense"))
                        {
                            tgd = JsonUtility.FromJson<TGData>(dataLine);

                            if (rec_eSense && keepOnRunning)
                            {
                                eSenseOut.Write(eSenseCount + ", " + dataLine + "\n");
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
        //closing everything
        Task.Delay(ThreadSleepTime * 2);//just in case 
        if (rec_eSense)
        {
            eSenseOut.Flush();
            eSenseOut.Close();
        }
        if (rec_raw)
        {
            rawOut.Flush();
            rawOut.Close();
        }
        stream.Close();
        thread.Abort();//just in case 
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
