using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Net.Sockets;
using System.Net;
using System.IO;
using UnityEngine.Networking;

public class OpenSesameListener : MonoBehaviour
{
    TcpListener tcpListener;
    Dictionary<string, Texture2D> receivedImages = new Dictionary<string, Texture2D>();
    private readonly Queue<Action> _executeOnMainThread = new Queue<Action>();
    private StreamWriter streamWriter;

    void Update()
    {
        while (_executeOnMainThread.Count > 0)
        {
            _executeOnMainThread.Dequeue().Invoke();
        }
    }
    
    public void Log(string message)
    {
        Debug.Log(message);
        streamWriter.WriteLine(message);
        streamWriter.Flush();
    }

    void Start()
    {
        tcpListener = new TcpListener(IPAddress.Any, 8052);
        streamWriter = new StreamWriter("unity-log.txt");
        tcpListener.Start();
        BeginAcceptTcpClient();
        Debug.Log("Server is listening on port 8052");
    }

    void BeginAcceptTcpClient()
    {
        tcpListener.BeginAcceptTcpClient(new AsyncCallback(OnTcpClientConnected), null);
    }

    void OnTcpClientConnected(IAsyncResult ar)
    {
        Debug.Log("Connected to client");
        TcpClient client = tcpListener.EndAcceptTcpClient(ar);
        BeginAcceptTcpClient(); // Continue listening for new connections

        StreamReader reader = new StreamReader(client.GetStream());
        string jsonData = reader.ReadToEnd();
        ProcessJsonData(jsonData);
    }

    void ProcessJsonData(string jsonData)
    {
        Debug.Log("Receiving JSON: " + jsonData);
        var data = JsonUtility.FromJson<JsonData>(jsonData);

        if (data.image_data != null && !string.IsNullOrEmpty(data.image_data.data))
        {
            Debug.Log("JSON contains image data");
            _executeOnMainThread.Enqueue(() =>
            {
                Debug.Log("Loading image data");    
                // Move texture operations back to the main thread
                byte[] imageBytes = Convert.FromBase64String(data.image_data.data);
                Texture2D smallTexture = new Texture2D(0, 0);
                smallTexture.LoadImage(imageBytes);
                Texture2D largeTexture = new Texture2D(6000, 6000);
                int startX = (largeTexture.width - smallTexture.width) / 2;
                int startY = (largeTexture.height - smallTexture.height) / 2;
                Color[] pixels = smallTexture.GetPixels();
                largeTexture.SetPixels(startX, startY, smallTexture.width, smallTexture.height, pixels);
                largeTexture.Apply();
                receivedImages[data.image_data.id] = largeTexture;
                Debug.Log("Image data loaded");    
            });
        }
        else if (!string.IsNullOrEmpty(data.flip_skybox))
        {
            Debug.Log("JSON contains flip command");
            FlipSkybox(data.flip_skybox);
        }
        else if (!string.IsNullOrEmpty(data.log))
        {
            Debug.Log("JSON contains log command");
            Log(data.log);
        }
        else
        {
            Debug.Log("Unknown JSON command");
        }
        Debug.Log("JSON was processed");
    }

    void FlipSkybox(string id)
    {
        _executeOnMainThread.Enqueue(() =>
        {
            Debug.Log("id: " + id);
            if (receivedImages.ContainsKey(id))
            {
                Material oldSkyboxMaterial = RenderSettings.skybox;
                Material newSkyboxMaterial = new Material(oldSkyboxMaterial);
                newSkyboxMaterial.mainTexture = receivedImages[id];
                // Set the new material as the skybox
                RenderSettings.skybox = newSkyboxMaterial;
                DynamicGI.UpdateEnvironment();
                Debug.Log("Skybox flipped to image with ID: " + id);
                // Free the old texture
                if (oldSkyboxMaterial != null && oldSkyboxMaterial.mainTexture != null)
                {
                    Destroy(oldSkyboxMaterial.mainTexture);
                }
                Destroy(oldSkyboxMaterial); // Destroy the old material itself
            }
            else
            {
                Debug.Log("Image ID not found");
            }
        });
    }

    void OnApplicationQuit()
    {
        Debug.Log("Closing");
        tcpListener.Stop();
        streamWriter.Close();
    }

    [Serializable]
    private class JsonData
    {
        public ImageData image_data;
        public string flip_skybox;
        public string log;
    }

    [Serializable]
    private class ImageData
    {
        public string id;
        public string data; // Base64-encoded image data
    }
}

