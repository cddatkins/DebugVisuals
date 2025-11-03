using System;
using System.Collections.Generic;
using UnityEngine;
using DebugVisuals;

public class Sample : MonoBehaviour
{
    private void Start()
    {
        DebugDrawGL.DrawWireBox(Vector3.zero, Vector3.one * 2, Color.red, 5f);
    }
}

