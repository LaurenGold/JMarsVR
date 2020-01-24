using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MakeMesh2 : MonoBehaviour
{
    MeshFilter mf;

    // Start is called before the first frame update
    void Start()
    {
        mf = GetComponent<MeshFilter>();
        int width = 200;
        int height = 200;

        int[] trisArray = new int[2 * width * height * 3];
        Vector3[] vertexArray = new Vector3[width * height];



        for (int j = 0; j < height; j++)
        {
            for (int i = 0; i < width; i++)
            {
                int index = j * width + i;

                vertexArray[index] = new Vector3(i*1.0f/width, j*1.0f/height, 0);

                if (j<height-1 && i < width - 1) { 
                    trisArray[index * 2 * 3 + 0] = index;
                    trisArray[index * 2 * 3 + 1] = index + 1;
                    trisArray[index * 2 * 3 + 2] = index + 1+width;
                    trisArray[index * 2 * 3 + 3] = index;
                    trisArray[index * 2 * 3 + 4] = index + 1+width;
                    trisArray[index * 2 * 3 + 5] = index + width;
                }
            }
        }
        Mesh m = new Mesh();
        m.vertices = vertexArray;
        m.triangles = trisArray;
        m.RecalculateNormals();
        mf.mesh = m;


    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
