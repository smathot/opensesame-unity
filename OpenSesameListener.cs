using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Linq;
using UnityEngine.Networking;

public class OpenSesameListener : MonoBehaviour
{
    private TcpListener tcpListener;
    private Dictionary<string, Texture2D> receivedImages = new Dictionary<string, Texture2D>();
    private readonly Queue<Action> _executeOnMainThread = new Queue<Action>();
    private StreamWriter logFileWriter;
    private int tcpPort = 8052;
    private int skyboxSize = 6000;    
    private Color skyboxColor = new Color(.5f, .5f, .5f, 1f);

    /** Continuously checks a queue for functions that should be executed in the main thread.
    This is necessary because some Unity functions only work on the main thread. */
    void Update()
    {
        while (_executeOnMainThread.Count > 0)
        {
            _executeOnMainThread.Dequeue().Invoke();
        }
    }
        
    
    /** Writes a message to the log file, assuming the log file is open. */
    public void Log(string message)
    {
        Debug.Log(message);
        if (logFileWriter != null)
        {
            logFileWriter.WriteLine(message);
            logFileWriter.Flush();
        }
    }
    
    /** Starts the listener when the scene starts. */
    void Start()
    {
        tcpListener = new TcpListener(IPAddress.Any, tcpPort);
        tcpListener.Start();
        BeginAcceptTcpClient();
        Debug.Log("Server is listening on port " + tcpPort);
    }

    /** Listens for an incoming connection and asynchronously processes it. */
    void BeginAcceptTcpClient()
    {
        tcpListener.BeginAcceptTcpClient(new AsyncCallback(OnTcpClientConnected), null);
    }

    /** Processes an incoming connection and listens for the next one.*/
    void OnTcpClientConnected(IAsyncResult ar)
    {
        TcpClient client = tcpListener.EndAcceptTcpClient(ar);
        BeginAcceptTcpClient();
        StreamReader reader = new StreamReader(client.GetStream());
        string jsonData = reader.ReadToEnd();
        ProcessJsonData(jsonData);
    }

    /** Processes one incoming command. */
    void ProcessJsonData(string jsonData)
    {
        var data = JsonUtility.FromJson<JsonData>(jsonData);
        if (data.command == "image")
        {
            LoadImage(data);   
        }
        else if (data.command == "skybox_size")
        {
            Debug.Log("Updating skybox size");
            skyboxSize = int.Parse(data.message);
        }
        else if (data.command == "skybox_color")
        {
            Debug.Log("Updating skybox color");
            skyboxColor = HexToColor(data.message);
        }
        else if (data.command == "flip_skybox")
        {
            Debug.Log("Flipping skybox");
            FlipSkybox(data.message);
        }
        else if (data.command == "init_log")
        {
            Debug.Log("Initializing log file: " + data.message);
            logFileWriter = new StreamWriter(data.message);
        }
        else if (data.command == "log")
        {
            Log(data.message);
        }
        else
        {
            Debug.Log("Unknown JSON command: " + data.command);
        }
    }
    
    /** Loads image data. This is queued so that it's executed on the main thread. */
    void LoadImage(JsonData data)
    {
        _executeOnMainThread.Enqueue(() =>
        {
            Debug.Log("Loading image data");    
            byte[] imageBytes = Convert.FromBase64String(data.data);
            Texture2D smallTexture = new Texture2D(0, 0);
            smallTexture.LoadImage(imageBytes);
            Color[] pixels = smallTexture.GetPixels();
            Texture2D skyboxTexture = new Texture2D(skyboxSize, skyboxSize);
            skyboxTexture.SetPixels(Enumerable.Repeat(skyboxColor, skyboxTexture.width * skyboxTexture.height).ToArray());
            int startX = (skyboxTexture.width - smallTexture.width) / 2;
            int startY = (skyboxTexture.height - smallTexture.height) / 2;
            skyboxTexture.SetPixels(startX, startY, smallTexture.width, smallTexture.height, pixels);
            skyboxTexture.Apply();
            receivedImages[data.message] = skyboxTexture;
            Debug.Log("Image data loaded");
        });   
    }
    
    /** Updates the skybox based on a previously loaded image. Executed on the main thread. */
    void FlipSkybox(string id)
    {
        _executeOnMainThread.Enqueue(() =>
        {
            if (receivedImages.ContainsKey(id))
            {
                Material oldSkyboxMaterial = RenderSettings.skybox;
                Material newSkyboxMaterial = new Material(oldSkyboxMaterial);
                newSkyboxMaterial.mainTexture = receivedImages[id];
                RenderSettings.skybox = newSkyboxMaterial;
                DynamicGI.UpdateEnvironment();
                Debug.Log("Skybox flipped to image with ID: " + id);
                if (oldSkyboxMaterial != null && oldSkyboxMaterial.mainTexture != null)
                {
                    Destroy(oldSkyboxMaterial.mainTexture);
                }
                Destroy(oldSkyboxMaterial);
            }
            else
            {
                Debug.Log("Image ID not found: " + id);
            }
        });
    }
    
    /** Convert hex color string to triplet floats */
    public static Color HexToColor(string hex)
    {
        hex = hex.Replace("#", "");
        byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
        byte a = hex.Length >= 8 ? byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber) : (byte)255;
        return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
    }

    void OnApplicationQuit()
    {
        Debug.Log("Closing");
        tcpListener.Stop();
        if (logFileWriter != null)
        {
            logFileWriter.Close();
        }
    }

    [Serializable]
    private class JsonData
    {        
        public string command;
        public string message;
        public string data;
    }
}
