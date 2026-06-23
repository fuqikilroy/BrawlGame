using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float zoomSpeed = 50;

    private Vector3 offset;
    private Vector3 startPoint;
    public Transform playerTransform;


    // Start is called before the first frame update
    void Start()
    {
        startPoint = new Vector3(0, 0, 0);
        offset = transform.position - startPoint;//获取偏移
        //offset = transform.position - playerTransform.position;

    }

    // Update is called once per frame
    void Update()
    {
        //playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        playerTransform = GameObject.Find("Player").transform;
        PlayerData palye;
        transform.position = playerTransform.position + offset;//计算偏移后的位置，实际上就是跟随

        float scroll = Input.GetAxis("Mouse ScrollWheel");

        Camera.main.fieldOfView += scroll * zoomSpeed;

        Camera.main.fieldOfView = Mathf.Clamp(Camera.main.fieldOfView, 17, 70);

        Debug.Log(playerTransform.name);

    }
}
