using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShadowScript : MonoBehaviour
{
    public Vector3 NV; // normal vector
    public float timeToMax = 2;
    public float lifetime = 0;

    // Start is called before the first frame update
    void Start()
    {
        NV = Vector3.one - new Vector3(Mathf.Abs(NV.x), Mathf.Abs(NV.y), Mathf.Abs(NV.z));
        //if (NV == Vector3.down || NV == Vector3.forward || NV == Vector3.right)
        //    NV -= Vector3.one * 2;

        transform.localScale = Vector3.Lerp(NV, Vector3.one, lifetime);
    }

    // Update is called once per frame
    void Update()
    {
        if (lifetime < timeToMax)
        {
            lifetime += Time.deltaTime / timeToMax;

            transform.localScale = Vector3.Lerp(NV, Vector3.one, lifetime);
        }
    }
}
