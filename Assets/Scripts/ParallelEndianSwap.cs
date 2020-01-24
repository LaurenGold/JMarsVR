using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class ParallelEndianSwap : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        NativeArray<byte> a = new NativeArray<byte>(16, Allocator.TempJob);

//        NativeArray<float> b = new NativeArray<float>(2, Allocator.TempJob);

        NativeArray<byte> result = new NativeArray<byte>(16, Allocator.TempJob);

        for (int i = 0; i < 16; i++)
        {
            a[i] = (byte)('a'+i);

        }
        MyParallelJob jobData = new MyParallelJob();
        jobData.a = a;
        jobData.result = result;

        // Schedule the job with one Execute per index in the results array and only 1 item per processing batch
        JobHandle handle = jobData.Schedule(result.Length, 1);

        // Wait for the job to complete
        handle.Complete();

        Debug.Log("done");

        foreach(char x in result)
        {
            Debug.Log(x);
        }

        // Free the memory allocated by the arrays
        a.Dispose();
        result.Dispose();

    }
    public static byte[] EndianSwap(byte[] inputBytes)
    {
        NativeArray<byte> a = new NativeArray<byte>(inputBytes.Length, Allocator.TempJob);
        NativeArray<byte> result = new NativeArray<byte>(inputBytes.Length, Allocator.TempJob);

        a.CopyFrom(inputBytes);

        MyParallelJob jobData = new MyParallelJob();
        jobData.a = a;
        jobData.result = result;

        // Schedule the job with one Execute per index in the results array and only 1 item per processing batch
        JobHandle handle = jobData.Schedule(result.Length, 1);

        // Wait for the job to complete
        handle.Complete();

        result.CopyTo(inputBytes);

        a.Dispose();
        result.Dispose();

        return inputBytes;
    }
    // Update is called once per frame
    void Update()
    {
    }
    public struct MyParallelJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<byte> a;
        public NativeArray<byte> result;

        public void Execute(int i)
        {
            result[i] = a[i+3-(i%4)*2];
        }
    }
}
