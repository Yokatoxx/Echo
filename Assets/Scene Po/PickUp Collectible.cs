using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickUpCollectible : MonoBehaviour
{

    public GameObject player;
    public Transform holdPos;
    public float pickUpRange = 5f;

    private GameObject heldObj;
    private Rigidbody heldObjRb;
    private int LayerNumber;


    void Start()
    {
        LayerNumber = LayerMask.NameToLayer("holdLayer");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (heldObj == null)
            {

                RaycastHit hit;
                if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, pickUpRange))
                {

                    if (hit.transform.gameObject.tag == "Collectible")
                    {

                        PickUpObject(hit.transform.gameObject);
                    }
                }
            }
            
        }
        if (heldObj != null)
        {
            MoveObject();

        }
    }

    void PickUpObject(GameObject pickUpObj)
    {
        if (pickUpObj.GetComponent<Rigidbody>())
        {
            heldObj = pickUpObj;
            heldObjRb = pickUpObj.GetComponent<Rigidbody>();
            heldObjRb.isKinematic = true;
            heldObjRb.transform.parent = holdPos.transform;
            heldObj.layer = LayerNumber;
            Physics.IgnoreCollision(heldObj.GetComponent<Collider>(), player.GetComponent<Collider>(), true);
        }
    }

    void MoveObject()
    {

        heldObj.transform.position = holdPos.transform.position;
    }
}
