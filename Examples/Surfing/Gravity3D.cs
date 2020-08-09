using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gravity3D : MonoBehaviour
{
    StickyRigidBody stickyRB;
    public float gravityScale = 1f;
    float rotationStep = 0.05f;
    public bool useStickyGravity = true;
    List<GravityField> currentFields = new List<GravityField>();

    // Start is called before the first frame update
    void Start()
    {
        stickyRB = GetComponent<StickyRigidBody>();
    }

    private void FixedUpdate()
    {
        if (!stickyRB._isSticking)
        {
            Vector3 gravity = resolveGravityForce();
            if (gravity.magnitude > 0)
            {
                rotateUpTowardsVec(-gravity);
            }
            stickyRB._rigidBody.AddForce(resolveGravityForce() * stickyRB._rigidBody.mass, ForceMode.Force);
        }
        else if(useStickyGravity)
        {
            stickyRB.AddImpulseForce(resolveGravityForce() * Time.fixedDeltaTime);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public Vector3 resolveGravityForce()
    {
        //return (Vector3.zero - transform.position).normalized * 9.8f * gravityScale;
        Vector3 accumulatedForce = Vector3.zero;
        currentFields.ForEach(delegate (GravityField gravityField)
        {
            Vector3 direction = (gravityField.transform.position - this.transform.position).normalized;
            accumulatedForce += direction * gravityField.magnitude;
        });

        return accumulatedForce * gravityScale;
    }

    // Rotates character incrementally so character's up vector aligns with -gravity
    void rotateUpTowardsVec(Vector3 targetUp)
    {
        targetUp = targetUp.normalized;
        float dot = Vector3.Dot(transform.up, targetUp);
        dot = Mathf.Clamp(dot, -1, 1);

        if (dot == 1)
        {
            // Vectors align, no need to rotate
            return;
        }

        // Find rotation axis
        Vector3 axis;
        if (dot == -1)
        {
            // Special case, vectors were perfectly opposite
            dot = Vector3.Dot(transform.right, targetUp);
            axis = Vector3.Cross(transform.right, targetUp);
        }
        else
        {
            // Normal case
            axis = Vector3.Cross(transform.up, targetUp).normalized;
        }

        // Execute rotation
        float deg = Mathf.Clamp(Mathf.Acos(dot), 0, rotationStep) * Mathf.Rad2Deg;
        transform.Rotate(axis, deg, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.layer == 25)
        {
            currentFields.Add(other.GetComponent<GravityField>());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == 25)
        {
            GravityField leavingField = other.GetComponent<GravityField>();
            if (currentFields.Contains(leavingField))
            {
                currentFields.Remove(leavingField);
            }
        }
    }
}
