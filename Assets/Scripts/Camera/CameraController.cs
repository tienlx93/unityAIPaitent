using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour {

	public float xMoveThreshold = 60.0f;
	public float yMoveThreshold = 60.0f;

	public float yMaxLimit = 45.0f;
	public float yMinLimit = -45.0f;

	public Quaternion rotation = Quaternion.identity;
	float yRotCounter = 0f;
	float xRotCounter = 0f;

    // Use this for initialization
    void Start () {
		//set camera rotation is default
	}
	
	// Update is called once per frame
	void Update () {
        // Smoothly tilts a transform towards a target rotation.
    }
}
