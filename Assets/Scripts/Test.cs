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
        if (Logger.Instance == null)
        {
            loggerInstanceFail.SetActive(true);
        }
        else
        {
            loggerInstanceSuccess.SetActive(true);
        }
        //byte[] imageData = new byte[1200000];
        //Logger.Frame(imageData);
    }

    private void FixedUpdate()
    {
        if(Logger.sock != null)
        {
            connectedSuccess.SetActive(true);
            connectedFail.SetActive(false);
        }
        else
        {
            connectedSuccess.SetActive(false);
            connectedFail.SetActive(true);
        }
    }
}
