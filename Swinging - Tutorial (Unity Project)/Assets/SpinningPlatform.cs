using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpinningPlatform : MonoBehaviour
{
    private Transform transForm;

    public float moveSpeed;
    // Start is called before the first frame update
    void Start()
    {
        transForm = GetComponent<Transform>();
    }

    // Update is called once per frame
    void Update()
    {
        transForm.Rotate(0f, moveSpeed * Time.deltaTime, 0f);
    }
}
