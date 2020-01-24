using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class RcvTxt : MonoBehaviour
{
    private TcpListener tcpListener;
    /// <summary> 
    /// Background thread for TcpServer workload.   
    /// </summary>  
    private Thread tcpListenerThread;
    /// <summary>   
    /// Create handle to connected tcp client.  
    /// </summary>  
    private TcpClient connectedTcpClient;

    public Texture2D texture;
    bool textureReady = false;

    public byte[] byteArray { get; private set; }

    // Start is called before the first frame update
    void Start()
    {
        tcpListenerThread = new Thread(new ThreadStart(ListenForIncomingRequests));
        tcpListenerThread.IsBackground = true;
        tcpListenerThread.Start();

        //texture = new Texture2D(1369, 701);


    }

    // Update is called once per frame
    void Update()
    {
        if (textureReady)
        {
            texture.LoadImage(byteArray);
            Renderer rend = GetComponent<Renderer>();
            rend.material.mainTexture = texture;
        }
    }

    private void ListenForIncomingRequests()
    {
        try
        {
            // Create listener on localhost port 8052.          
            tcpListener = new TcpListener(IPAddress.Parse("127.0.0.1"), 6801);
            tcpListener.Start();
            Debug.Log("Server is listening");
            Byte[] bytes = new Byte[1024];

            while (true)
            {
                Debug.Log("texturelistening");

                using (connectedTcpClient = tcpListener.AcceptTcpClient())
                {
                    Debug.Log("6801");

                    // Get a stream object for reading                  
                    using (NetworkStream stream = connectedTcpClient.GetStream())
                    {
                        Debug.Log("6801 streaming");
                        byteArray = ReadFullyTrunc(stream, 32768);


                        Debug.Log("6801:" + byteArray.Length);
                        textureReady = true;
                        //ConvertByteArrayToVector3(byteArray);
                        //updateVerticesReady = true;

                    }
                }
            }

        }
        catch (SocketException socketException)
        {
            Debug.Log("SocketException " + socketException.ToString());
        }
    }
    public static byte[] ReadFullyTrunc(NetworkStream stream, int initialLength)
    {
        // If we've been passed an unhelpful initial length, just
        // use 32K.
        if (initialLength < 1)
        {
            initialLength = 32768;
        }

        byte[] buffer = new byte[initialLength];
        int read = 0;

        int chunk;
        while ((chunk = stream.Read(buffer, read, buffer.Length - read)) > 0)
        {
            read += chunk;

            // If we've reached the end of our buffer, check to see if there's
            // any more information
            if (read == buffer.Length)
            {
                int nextByte = stream.ReadByte();

                // End of stream? If so, we're done
                if (nextByte == -1)
                {
                    return buffer;
                }

                // Nope. Resize the buffer, put in the byte we've just
                // read, and continue
                byte[] newBuffer = new byte[buffer.Length * 2];
                Array.Copy(buffer, newBuffer, buffer.Length);
                newBuffer[read] = (byte)nextByte;
                buffer = newBuffer;
                read++;
            }
        }
        // Buffer is now too big. Shrink it.
        byte[] ret = new byte[read];
        Array.Copy(buffer, ret, read);
        return ret;
    }
}
