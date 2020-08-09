using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class StickyRigidBody : MonoBehaviour
{
    public LayerMask stickyMask;
    public Vector3 impulseForce = Vector3.zero;

    [HideInInspector]
    public Rigidbody rigidBody;

    // State Data
    [HideInInspector]
    public bool isSticking = true;

    // Movement Data
    [HideInInspector]
    public Vector3
        velocity,
        lastPosition;
    public StickyPhysics.Triangle curTri;

    // Start is called before the first frame update
    void Start()
    {
        StickyPhysics.AfterUnityPhysics.init();
        StickyPhysics.AfterUnityPhysics.add(this);
        rigidBody = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        StickyPhysics.Utility.fixedUpdateStep(this);
    }

    public void AfterPhysicsUpdate()
    {
        StickyPhysics.Utility.afterPhysicsStep(this);
    }

    // Causes stickyRB to unstick from surface if attached
    public void UnStick()
    {
        if (isSticking)
        {
            isSticking = false;
            rigidBody.isKinematic = false;
            velocity = Vector3.zero;
        }
    }

    public void addImpulseForce(Vector3 v)
    {
        impulseForce += v;
    }
}
