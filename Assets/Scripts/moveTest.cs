using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class moveTest : MonoBehaviour
{
    int y = 0;
    // Start is called before the first frame update
    public GameObject cube;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger)) {
            y += 1;
            
        } else if (OVRInput.Get(OVRInput.Button.SecondaryIndexTrigger)) {
            y -= 1;
        }

        cube.transform.position = new Vector3(0, y * Time.deltaTime, 0);
        Debug.Log("up?:" + transform.position);
    }
}
