using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    private float fromValue = 0;
    private float to = 1;
    private void Update()
    {
        var value = Mathf.Lerp(fromValue, to, Time.deltaTime * 0.1f);
        Debug.LogError(value);
        
    }
}
