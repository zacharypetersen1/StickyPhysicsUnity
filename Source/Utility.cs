using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StickyPhysics
{
    // Container for custom character physics
    public static class Utility
    {

        // Resolves a step of sticky physics
        public static void fixedUpdateStep(StickyRigidBody stickyRB)
        {
            // Character is sticking
            if (stickyRB.isSticking)
            {
                Vector3 force = Vector3.ProjectOnPlane(stickyRB.impulseForce, stickyRB.curTri.getFaceNormal());
                if (stickyRB.velocity.magnitude > 0)
                {
                    Vector3 friction = -stickyRB.velocity * (stickyRB.velocity.magnitude * 0.0022f);
                    force += friction;
                }
                Vector3 acceleration = force;
                stickyRB.velocity = Vector3.ProjectOnPlane(stickyRB.velocity + acceleration, stickyRB.curTri.getFaceNormal());
                stickyRB.impulseForce = Vector3.zero;
                applyVelocity(stickyRB, 0, 0);
            }
        }

        public static void afterPhysicsStep(StickyRigidBody stickyRB)
        {
            // When character is in the air, check to see if grounded
            if (!stickyRB.isSticking)
            {
                Vector3 deltaPosition = stickyRB.transform.position - stickyRB.lastPosition;
                checkIfGrounded(stickyRB, deltaPosition);
            }
            stickyRB.lastPosition = stickyRB.transform.position;
        }

        static void checkIfGrounded(StickyRigidBody stickyRB, Vector3 deltaPosition)
        {
            Ray r = new Ray(stickyRB.transform.position - deltaPosition.normalized * 0.2f, deltaPosition);
            RaycastHit hit;
            if (Physics.Raycast(r, out hit, deltaPosition.magnitude + 0.4f, stickyRB.stickyMask))
            {
                Vector3 velocity = stickyRB.rigidBody.velocity;
                stickyRB.isSticking = true;
                stickyRB.GetComponent<Rigidbody>().isKinematic = true;
                stickyRB.transform.position = hit.point;
                stickyRB.curTri = new Triangle(hit.transform, hit.normal, hit.collider as MeshCollider, hit.triangleIndex);
                Vector3 forward = stickyRB.curTri.projectOntoPlaneFromOrigin(stickyRB.transform.forward);
                if(forward.magnitude < Mathf.Epsilon)
                {
                    forward = stickyRB.curTri.projectOntoPlaneFromOrigin(stickyRB.transform.up);
                }
                stickyRB.transform.LookAt(stickyRB.transform.position + forward, stickyRB.curTri.getFaceNormal());
                stickyRB.addImpulseForce(velocity);
            }
        }

        // Applies velocity based on surfing physics
        static void applyVelocity(StickyRigidBody stickyRB, float traveled, int call)
        {
            //stickyRB.stickyTri.debugRender(Color.blue);
            stickyRB.transform.position = stickyRB.curTri.projectOntoPlane(stickyRB.transform.position);
            Vector3 tickVelocity = stickyRB.velocity.normalized * ((stickyRB.velocity.magnitude * Time.fixedDeltaTime) - traveled);

            // Find exit point and exit vector
            Vector3 exitPoint;
            stickyRB.curTri.projectOntoBoundaries(new Ray(stickyRB.transform.position, tickVelocity), out exitPoint);
            exitPoint = stickyRB.curTri.projectOntoPlane(exitPoint);
            Vector3 exitVector = exitPoint - stickyRB.transform.position;

            // Check if player will cross over to a new triangle
            BorderCheck.Result result = BorderCheck.checkRay(
                stickyRB.curTri,
                stickyRB.transform.position + stickyRB.curTri.getFaceNormal() * 0.01f,
                tickVelocity.normalized,
                Mathf.Min(tickVelocity.magnitude, exitVector.magnitude),
                stickyRB.stickyMask);
            if (result.outcome == BorderCheck.Result.Outcomes.success)
            {
                // need to cross over to a new triangle
                Triangle oldTri = stickyRB.curTri;
                stickyRB.curTri = result.triangle;

                // Update orientation of stickyVelocity and stickyRB's object
                stickyRB.velocity = Orientation.rotateVectorToNewPlane(oldTri.getFaceNormal(), stickyRB.curTri.getFaceNormal(), stickyRB.velocity);
                stickyRB.velocity = stickyRB.curTri.projectOntoPlaneFromOrigin(stickyRB.velocity);
                Orientation.rotateToNewUp(stickyRB.transform, stickyRB.curTri.getFaceNormal());
                
                float thisTraveled = (stickyRB.transform.position - result.position).magnitude;
                stickyRB.transform.position = stickyRB.curTri.projectOntoPlane(result.position);
                applyVelocity(stickyRB, traveled + thisTraveled, call + 1);
            }
            // Check if player will exit the tri this frame
            else if (tickVelocity.magnitude < exitVector.magnitude)
            {
                // Will not exit
                stickyRB.transform.position = stickyRB.curTri.projectOntoPlane(stickyRB.transform.position + tickVelocity);
            }
            else
            {
                // Check if there is a neighboring tri
                Triangle oldTri = stickyRB.curTri;
                result = BorderCheck.checkBorder(stickyRB.curTri, exitPoint, tickVelocity, stickyRB.stickyMask);
                if (result.outcome == BorderCheck.Result.Outcomes.success)
                {
                    // Update orientation of stickyVelocity and stickyRB's object             
                    Orientation.smoothOrient(stickyRB, result);

                    // Make another call to apply remaining velocity on new tri
                    applyVelocity(stickyRB, traveled + exitVector.magnitude, call + 1);
                }
                else
                {
                    //oldTri.debugRender(Color.yellow);
                    // TODO Switch to non-surfing physics
                    Debug.Log("No tri detected");
                }
            }
        }
    }
}