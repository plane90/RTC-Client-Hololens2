using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    private void Update()
    {
        transform.Rotate(transform.up * 10 * Time.deltaTime);
    }
}
