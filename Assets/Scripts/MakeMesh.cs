using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
public class MakeMesh : MonoBehaviour
{

    public Material material;
    #region private members     
    private TcpListener tcpListenerMesh;
    /// <summary> 
    /// Background thread for TcpServer workload.   
    /// </summary>  
    private Thread tcpListenerThreadMesh;
    /// <summary>   
    /// Create handle to connected tcp client.  
    /// </summary>  
    private TcpClient connectedTcpClientMesh;

    /// <summary>   
    /// TCPListener to listen for incoming TCP connection  
    /// requests.   
    /// </summary>
    /// <summary> 
    /// Background thread for TcpServer workload.   
    /// </summary>  
    private Thread tcpMeshListenerThread;

    #endregion

    MeshFilter mf;
    Mesh[,] meshes;
    List<List<List<Vector3>>> meshVertexArrays;
    int[] trisArray;
    Vector3[] normalsArray;
    List<Vector3>[,] meshVertexTiles;
    List<Vector2>[,] meshUVTiles;

    bool updateVerticesReady = false;
   
    public int mode = 0;

    static int tilewidth = 256;
    static int tileheight = 256;

    static int mapPixelWidth;
    static int mapPixelHeight;
    static int mapTileWidth;
    static int mapTileHeight;
    private byte[] byteArray;
    public bool textureReady;
    private Thread tcpListenerTextureThread;
    private TcpListener tcpListenerTexture;

    public TcpClient connectedTcpClientTexture { get; private set; }
    public Texture2D texture;


    // Use this for initialization
    void Start()
    {
        tcpListenerTextureThread = new Thread(new ThreadStart(ListenForTextureRequests));
        tcpListenerTextureThread.IsBackground = true;
        tcpListenerTextureThread.Start();

        tcpMeshListenerThread = new Thread(new ThreadStart(ListenForMesh));
        tcpMeshListenerThread.IsBackground = true;
        tcpMeshListenerThread.Start();



        textureReady = false;

        trisArray = new int[2 * tilewidth * tileheight*3];

        for (int j = 0; j < tileheight - 1; j++)
        {
            for (int i = 0; i < tilewidth - 1; i++)
             {
                int index = j * tilewidth + i;
                trisArray[index * 2 * 3 + 0] = index;
                trisArray[index * 2 * 3 + 1] = index+1;
                trisArray[index * 2 * 3 + 2] = index+tilewidth;
                trisArray[index * 2 * 3 + 3] = index+tilewidth;
                trisArray[index * 2 * 3 + 4] = index+1;
                trisArray[index * 2 * 3 + 5] = index+tilewidth+1;
            }
        }

        meshVertexArrays = new List<List<List<Vector3>>>();
        meshChildren = new List<GameObject>();
        updateMeshesReady = false;

        transform.position = new Vector3(Camera.main.transform.position.x, 0, Camera.main.transform.position.z);
    }
    public List<GameObject> indicators;
    public List<GameObject> meshChildren;
    private static List<Vector2>[,] uvArray;
    private byte[] meshbytes;
    private bool updateMeshesReady;

    Thread bytesToMeshes;
    // Update is called once per frame
    void Update()
    {
        //
        if (updateVerticesReady)// && updateTrisReady && updateNormsReady)
        {
            foreach (GameObject indicator in indicators)
            {
                //Debug.Log("ready");
                indicator.SetActive(true);
                indicator.GetComponent<Renderer>().material.color = Color.red;
                bytesToMeshes = new Thread(new ThreadStart(ConvertBytesToMeshes));
                bytesToMeshes.IsBackground = true;
                bytesToMeshes.Start();
            }

            //meshVertexTiles = ConvertBytesToMeshes(meshbytes);
        }
        if (updateMeshesReady)
        {
            foreach (GameObject indicator in indicators)
            {
                indicator.SetActive(false);
            }
            foreach (GameObject meshChild in meshChildren){
                Destroy(meshChild);
            }
            
            for (int j = 0; j < mapTileHeight; j++)
            {
                for (int i = 0; i < mapTileWidth ; i++)
                {
                    Mesh newMesh = new Mesh();
                    newMesh.SetVertices(meshVertexTiles[j, i]);
                    newMesh.triangles = trisArray;
                    newMesh.RecalculateNormals();
                    newMesh.SetUVs(0, uvArray[j,i]);
                    var newMeshChild = new GameObject("Mesh_"+j+"_"+i);
                    newMeshChild.transform.parent = this.transform;
                    newMeshChild.transform.localRotation = Quaternion.identity;
                    float scale = Mathf.Max(mapPixelWidth, mapPixelHeight);
                    float xOff = -mapPixelWidth / scale/2;
                    float yOff = -mapPixelHeight / scale / 2;
                    newMeshChild.transform.localPosition = new Vector3(xOff, yOff, 0);
                    newMeshChild.transform.localScale = new Vector3(1,1,1);
                    newMeshChild.AddComponent<MeshFilter>();
                    newMeshChild.AddComponent<MeshRenderer>();
                    newMeshChild.GetComponent<MeshFilter>().mesh = newMesh;

                    meshChildren.Add(newMeshChild);
                    MeshRenderer mr = newMeshChild.GetComponent<MeshRenderer>();
                    mr.material = material;
                }
            }
            updateMeshesReady = false;
        }
        if (textureReady)
        {
            Debug.Log("texture Ready!");
            texture = new Texture2D(mapPixelWidth, mapPixelHeight);
            texture.LoadImage(byteArray);
            material.mainTexture = texture;
            textureReady = false;
        }

    }

    private void ListenForMesh()
    {
        try
        {
            tcpListenerMesh = new TcpListener(IPAddress.Any, 6789);
            tcpListenerMesh.Start();
            Debug.Log("Server is listening for mesh");

            while (true)
            {
                using (connectedTcpClientMesh = tcpListenerMesh.AcceptTcpClient())
                {
                    Debug.Log("6789");
                    
                    // Get a stream object for reading                  
                    using (NetworkStream stream = connectedTcpClientMesh.GetStream())
                    {
                        Debug.Log("6789 streaming");
                        meshbytes = ReadFully(stream, 32768);
                        Debug.Log("6789:" + meshbytes.Length);

                        updateVerticesReady = true;

                    }
                }
            }

        }
        catch (SocketException socketException)
        {
            Debug.Log("SocketException " + socketException.ToString());
        }
    }

    public void ConvertBytesToMeshes()
    {
        if (updateVerticesReady) {
            updateVerticesReady = false;
        byte[] buffer = meshbytes;
        Debug.Log("Read in to buffer");
        //if (BitConverter.IsLittleEndian)
        //{
        //    Array.Reverse(buffer, 0, 4);
        //    Array.Reverse(buffer, 4, 4);
        //    }
        mapPixelWidth = Mathf.RoundToInt(BitConverter.ToSingle(buffer, 0));
        mapPixelHeight = Mathf.RoundToInt(BitConverter.ToSingle(buffer, 4));

        Debug.Log(mapPixelWidth + " x " + mapPixelHeight);
        int read = (mapPixelWidth * mapPixelHeight + 2) * 4*3;// number of bytes in the buffer

            //if (BitConverter.IsLittleEndian)
            //{
            //    for (int i = 8; i < read; i += 4)
            //    {
            //        Array.Reverse(buffer, i, 4);
            //    }
            //    Debug.Log("Endianness converted " + BitConverter.IsLittleEndian);
            //}
            //meshbytes = ParallelEndianSwap.EndianSwap(meshbytes);
 //           Debug.Log("Endianness converted " + BitConverter.IsLittleEndian);

            int scale = Math.Max(mapPixelWidth, mapPixelHeight);

        mapTileWidth = mapPixelWidth/255 + 1;
        mapTileHeight = mapPixelHeight/255 + 1;

        List<Vector3>[,] tileArray = MakeTileArray(mapTileHeight, mapTileWidth, scale);
        Debug.Log("Tile Array made");
        int count=0;
        int textureWidth = Mathf.RoundToInt(Mathf.Pow(2, Mathf.CeilToInt(Mathf.Log(mapPixelWidth,2))));
        int textureHeight = Mathf.RoundToInt(Mathf.Pow(2, Mathf.CeilToInt(Mathf.Log(mapPixelHeight,2))));
        Debug.Log("Texture WH:" + textureWidth+","+textureHeight);
        float textureHeightOffset = (textureHeight - mapPixelHeight)/textureHeight;
        float textureWidthOffset = (textureWidth - mapPixelWidth) / textureWidth;
        for (int i = 8; i < read; i += 4 + 4 + 4)
        {
            count++;
            float x = mapPixelWidth - 1 - BitConverter.ToSingle(buffer, i);
            float y = BitConverter.ToSingle(buffer, i+ 4) ;
            float z = BitConverter.ToSingle(buffer, i+ 8) ;
            Vector3 v = (new Vector3(x, y, z))/scale;

            Vector2 uv = new Vector2(1-(x-mapPixelWidth)/textureWidth,1-(mapPixelHeight-y)/textureHeight);

            int X = Mathf.RoundToInt(x);
            int Y = Mathf.RoundToInt(y);
            int tileX = X / 255;
            int tileY = Y / 255;
            int tileXIndex = X % 255;
            int tileYIndex = Y % 255;

            int index = tileXIndex + tileYIndex * 256;
            // Debug.Log("Tile Array access" + tileY + "," + tileX + "," + index + ":" + x + ": " + y + ": " + z);

            tileArray[tileY, tileX][index] = v;
            uvArray[tileY, tileX][index] = uv;

            if (tileXIndex == 0 && tileX != 0)
            {
                index = 255 + tileYIndex * 256;
                tileArray[tileY, tileX - 1][index] = v;
                uvArray[tileY, tileX - 1][index] = uv;
            }
            if (tileYIndex == 0 && tileY != 0)
            {
                index = tileXIndex + 255 * 256;
                tileArray[tileY - 1, tileX][index] = v;
                uvArray[tileY - 1, tileX][index] = uv;
            }
            if (tileXIndex == 0 && tileX != 0 && tileYIndex == 0 && tileY != 0)
            {
                index = 255 + 255 * 256;
                tileArray[tileY - 1, tileX - 1][index] = v;
                uvArray[tileY - 1, tileX-1][index] = uv;
            }
        }
        //Force extraneous vertices back
        for (int y = 0; y < mapPixelHeight; y++)
        {
            int tileX = mapPixelWidth / 255;
            int tileY = y / 255;
            Vector3 v = tileArray[tileY, tileX][(mapPixelWidth % 255 - 1) + (y % 255) * 256];
            for (int xplus = mapPixelWidth; xplus < (mapTileWidth) * 255; xplus++)
            {
                int tileXIndex = xplus % 255;
                int tileYIndex = y % 255;

                int index = tileXIndex + tileYIndex * 256;
                tileArray[tileY, tileX][index] = v;
                if (tileYIndex == 0 && tileY != 0)
                {
                    index = tileXIndex + 255 * 256;
                    tileArray[tileY - 1, tileX][index] = v;
                }
            }
        }
        for (int x = 0; x < mapPixelWidth; x++)
        {
            int tileX = x / 255;
            int tileY = mapPixelHeight / 255;
            Vector3 v = tileArray[tileY, tileX][x % 255 + mapPixelHeight%255 * 256];
            for (int yplus = mapPixelHeight; yplus < (mapTileHeight) * 255; yplus++)
            {
                int tileXIndex = x % 255;
                int tileYIndex = yplus % 255;

                int index = tileXIndex + tileYIndex * 256;
                tileArray[tileY, tileX][index] = v;
                    if (tileXIndex == 0 && tileX != 0)
                    {
                        index = 255 + tileYIndex * 256;
                        tileArray[tileY, tileX - 1][index] = v;
                    }
                }
        }
        Debug.Log("Tile Array Filled " + count);
        meshVertexTiles= tileArray;
        updateMeshesReady = true;
        }
    }

    private void ListenForTextureRequests()
    {
        try
        {
            // Create listener on localhost port 8052.          
            tcpListenerTexture = new TcpListener(IPAddress.Any, 6801);
            tcpListenerTexture.Start();
            Debug.Log("Server is listening");
            Byte[] bytes = new Byte[1024];

            while (true)
            {
                Debug.Log("texturelistening");

                using (connectedTcpClientTexture = tcpListenerTexture.AcceptTcpClient())
                {
                    Debug.Log("6801");

                    // Get a stream object for reading                  
                    using (NetworkStream stream = connectedTcpClientTexture.GetStream())
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
    /// <summary>
    /// Reads data from a stream until the end is reached. The
    /// data is returned as a byte array. An IOException is
    /// thrown if any of the underlying IO calls fail.
    /// </summary>
    /// <param name="stream">The stream to read data from</param>
    /// <param name="initialLength">The initial buffer length</param>
    //
    public static byte[] ReadFully(NetworkStream stream, int initialLength)
    {
        // If we've been passed an unhelpful initial length, just
        // use 32K.
        if (initialLength < 8)
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
        return buffer;
    }

    private static List<Vector3>[,] MakeTileArray(int h, int w, float scale)
    {

        List<Vector3>[,] tileArray = new List<Vector3>[h,w];
        uvArray = new List<Vector2>[h, w];

        for (int j = 0; j < h; j++)
        {
            for (int i = 0; i < w; i++)
            {
                //int tileXIndex = X % 255;
                //int tileYIndex = Y % 255;
                tileArray[j, i] = new List<Vector3>(256*256);
                uvArray[j, i] = new List<Vector2>(256 * 256);
                for (int k = 0; k < 256*256; k++)
                {
                    float x = (i * 255 + k % 255);
                    float y = (j * 255 + k / 255);
                    x = Mathf.Min(x, mapPixelWidth);
                    y = Mathf.Min(y, mapPixelHeight);
                    tileArray[j,i].Add(new Vector3(x/scale,y/scale,0));
                    uvArray[j, i].Add(Vector2.zero);

                }
                //Debug.Log("tilearray " + j + "," + i);
            }
        }
        return tileArray;
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
    //static int[] ConvertByteArrayToInts(byte[] bytes)
    //{

    //    //if (bytes.Length % 4 != 0) throw new ArgumentException();

    //    int[] tris = new int[bytes.Length / 4];
    //    for (int i = 0; i < tris.Length; i ++)
    //    {
    //        Array.Reverse(bytes, i*4, 4);
    //        int x = BitConverter.ToInt32(bytes, i * 4);
    //        tris[i] = x;
    //        if (i < 10)
    //        {
    //            Debug.Log("Triangle int " + i + ":" + tris[i]);
    //        }
    //    }

    //    return tris;
    //}

    /// <summary>   
    /// Send message to client using socket connection.     
    /// </summary>  
    //private void SendMessage()
    //{
    //    if (connectedTcpClient == null)
    //    {
    //        return;
    //    }

    //    try
    //    {
    //        // Get a stream object for writing.             
    //        NetworkStream stream = connectedTcpClient.GetStream();
    //        if (stream.CanWrite)
    //        {
    //            string serverMessage = "This is a message from your server.";
    //            // Convert string message to byte array.                 
    //            byte[] serverMessageAsByteArray = Encoding.ASCII.GetBytes(serverMessage);
    //            // Write byte array to socketConnection stream.               
    //            stream.Write(serverMessageAsByteArray, 0, serverMessageAsByteArray.Length);
    //            Debug.Log("Server sent his message - should be received by client");
    //        }
    //    }
    //    catch (SocketException socketException)
    //    {
    //        Debug.Log("Socket exception: " + socketException);
    //    }
    //}


    //private void ListenForIncomingRequests2()
    //{
    //    try
    //    {
    //        // Create listener on localhost port 8052.          
    //        tcpListener2 = new TcpListener(IPAddress.Parse("127.0.0.1"), 6790);
    //        tcpListener2.Start();
    //        Debug.Log("Server is listening");
    //        Byte[] bytes = new Byte[1024];

    //        while (!updateTrisReady)
    //        {
    //            using (connectedTcpClient2 = tcpListener2.AcceptTcpClient())
    //            {

    //                // Get a stream object for reading                  
    //                using (NetworkStream stream = connectedTcpClient2.GetStream())
    //                {
    //                    byte[] byteArray = ReadFully(stream, 32768);

    //                    Debug.Log("tris: "+byteArray.Length);
    //                    //trisArray = 
    //                    ConvertByteArrayToInts(byteArray);
    //                    updateTrisReady = true;

    //                    //mf.sharedMesh.vertices = floatArray;
    //                    /*int length;
    //                    // Read incoming stream into byte arrary.                      
    //                    while ((length = stream.Read(bytes, 0, bytes.Length)) != 0)
    //                    {
    //                        var incomingData = new byte[length];
    //                        Array.Copy(bytes, 0, incomingData, 0, length);
    //                        //Array.Copy(bytes, floatArray, bytes.Length);
    //                        // Convert byte array to string message.                            
    //                        //string clientMessage = Encoding.ASCII.GetString(incomingData);
    //                        //Debug.Log("client message received as: " + length);
    //                    }*/
    //                }
    //            }
    //        }

    //    }
    //    catch (SocketException socketException)
    //    {
    //        Debug.Log("SocketException " + socketException.ToString());
    //    }
    //}

    //private void ListenForUV()
    //{
    //    try
    //    {
    //        // Create listener on localhost port 8052.          
    //        tcpListener3 = new TcpListener(IPAddress.Parse("127.0.0.1"), 6791);
    //        tcpListener3.Start();
    //        Debug.Log("Server is listening");
    //        Byte[] bytes = new Byte[1024];

    //        while (!updateVerticesReady)
    //        {
    //            using (connectedTcpClient3 = tcpListener3.AcceptTcpClient())
    //            {

    //                // Get a stream object for reading                  
    //                using (NetworkStream stream = connectedTcpClient3.GetStream())
    //                {
    //                    byte[] byteArray = ReadFully(stream, 32768);

    //                    Debug.Log("6791:" + byteArray.Length);
    //                    //normalsArray = ConvertByteArrayToVector3(byteArray);
    //                    updateTexturesReady = true;
    //                }
    //            }
    //        }

    //    }
    //    catch (SocketException socketException)
    //    {
    //        Debug.Log("SocketException " + socketException.ToString());
    //    }
    //}



}
