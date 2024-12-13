using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControlCube : MonoBehaviour
{
    Rigidbody rb;
//    float power = 10f;
    // Start is called before the first frame update
    void Start()
    {
        rb = gameObject.GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.LeftArrow) == true)
        {
            transform.position += Vector3.left * 0.125f;
        }

        if (Input.GetKeyUp(KeyCode.RightArrow) == true)
        {
            transform.position += Vector3.right * 0.125f;
        }

        if (Input.GetKeyUp(KeyCode.UpArrow) == true)
        {
            transform.position += Vector3.forward * 0.125f;
        }

        if (Input.GetKeyUp(KeyCode.DownArrow) == true)
        {
            transform.position += Vector3.back * 0.125f;
        }
        if (Input.GetKey(KeyCode.Space) == true)
        {
            rb.AddForce(Vector3.up * 1f);
        }


    }
}
