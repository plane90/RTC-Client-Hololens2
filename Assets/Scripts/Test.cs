using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    public GameObject loggerInstanceFail;
    public GameObject loggerInstanceSuccess;
    public GameObject connectedFail;
    public GameObject connectedSuccess;
    public Logger logger;
    private System.Threading.Thread thread;

    private void Update()
    {
        transform.Rotate(transform.up * 10 * Time.deltaTime);
        //byte[] imageData = new byte[1200000];
        //System.Random random = new System.Random();
        //random.NextBytes(imageData);
        //Logger.Frame(imageData);
    }

    private void Start()
    {
        //if (Logger.Instance == null)
        //{
        //    loggerInstanceFail.SetActive(true);
        //}
        //else
        //{
        //    loggerInstanceSuccess.SetActive(true);
        //}
        //int cnt = 0;
        //Logger.Log($"hi {cnt++}");  // 0
        //byte[] imageData = new byte[10] { 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 };
        //Logger.Frame(imageData);
        //Logger.Log($"hi {cnt++}");  // 1
        //imageData = new byte[10] { 9, 8, 7, 6, 5, 6, 7, 8, 9, 10 };
        //Logger.Frame(imageData);
        //Logger.Log($"hi {cnt++}");  // 2
        //Logger.Log($"hi {cnt++}");  // 3
        //Logger.Frame(imageData);
        //Logger.Log($"hi {cnt++}");  // 4
        //Logger.Log($"hi {cnt++}");  // 5
    }

    //private void FixedUpdate()
    //{
    //    if(Logger.sock != null)
    //    {
    //        connectedSuccess.SetActive(true);
    //        connectedFail.SetActive(false);
    //    }
    //    else
    //    {
    //        connectedSuccess.SetActive(false);
    //        connectedFail.SetActive(true);
    //    }
    //}
}
