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
            if (stickyRB._isSticking)
            {
                Vector3 force = Vector3.ProjectOnPlane(stickyRB._impulseForce, stickyRB._currentTriangle.GetFaceNormal());
                if (stickyRB._velocity.magnitude > 0)
                {
                    Vector3 friction = -stickyRB._velocity * (stickyRB._velocity.magnitude * 0.0022f);
                    force += friction;
                }
                Vector3 acceleration = force;
                stickyRB._velocity = Vector3.ProjectOnPlane(stickyRB._velocity + acceleration, stickyRB._currentTriangle.GetFaceNormal());
                stickyRB._impulseForce = Vector3.zero;
                applyVelocity(stickyRB, 0, 0);
            }
        }

        public static void afterPhysicsStep(StickyRigidBody stickyRB)
        {
            // When character is in the air, check to see if grounded
            if (!stickyRB._isSticking)
            {
                Vector3 deltaPosition = stickyRB.transform.position - stickyRB._lastPosition;
                checkIfGrounded(stickyRB, deltaPosition);
            }
            stickyRB._lastPosition = stickyRB.transform.position;
        }

        static void checkIfGrounded(StickyRigidBody stickyRB, Vector3 deltaPosition)
        {
            Ray r = new Ray(stickyRB.transform.position - deltaPosition.normalized * 0.2f, deltaPosition);
            RaycastHit hit;
            if (Physics.Raycast(r, out hit, deltaPosition.magnitude + 0.4f, stickyRB._stickyMask))
            {
                Vector3 velocity = stickyRB._rigidBody.velocity;
                stickyRB._isSticking = true;
                stickyRB.GetComponent<Rigidbody>().isKinematic = true;
                stickyRB.transform.position = hit.point;
                stickyRB._currentTriangle = new Triangle(hit.transform, hit.normal, hit.collider as MeshCollider, hit.triangleIndex);
                Vector3 forward = stickyRB._currentTriangle.ProjectDirectionOntoTriangle(stickyRB.transform.forward);
                if(forward.magnitude < Mathf.Epsilon)
                {
                    forward = stickyRB._currentTriangle.ProjectDirectionOntoTriangle(stickyRB.transform.up);
                }
                stickyRB.transform.LookAt(stickyRB.transform.position + forward, stickyRB._currentTriangle.GetFaceNormal());
                stickyRB.AddImpulseForce(velocity);
            }
        }

        // Applies velocity based on surfing physics
        static void applyVelocity(StickyRigidBody stickyRB, float traveled, int call)
        {
            //stickyRB.stickyTri.debugRender(Color.blue);
            stickyRB.transform.position = stickyRB._currentTriangle.ProjectLocationOntoTriangle(stickyRB.transform.position);
            Vector3 tickVelocity = stickyRB._velocity.normalized * ((stickyRB._velocity.magnitude * Time.fixedDeltaTime) - traveled);

            // Find exit point and exit vector
            Vector3 exitPoint;
            stickyRB._currentTriangle.ProjectRayOntoBoundaries(out exitPoint, new Ray(stickyRB.transform.position, tickVelocity));
            exitPoint = stickyRB._currentTriangle.ProjectLocationOntoTriangle(exitPoint);
            Vector3 exitVector = exitPoint - stickyRB.transform.position;

            // Check if player will cross over to a new triangle
            BorderCheck.Result result = BorderCheck.checkRay(
                stickyRB._currentTriangle,
                stickyRB.transform.position + stickyRB._currentTriangle.GetFaceNormal() * 0.01f,
                tickVelocity.normalized,
                Mathf.Min(tickVelocity.magnitude, exitVector.magnitude),
                stickyRB._stickyMask);
            if (result.outcome == BorderCheck.Result.Outcomes.success)
            {
                // need to cross over to a new triangle
                Triangle oldTri = stickyRB._currentTriangle;
                stickyRB._currentTriangle = result.triangle;

                // Update orientation of stickyVelocity and stickyRB's object
                stickyRB._velocity = Orientation.rotateVectorToNewPlane(oldTri.GetFaceNormal(), stickyRB._currentTriangle.GetFaceNormal(), stickyRB._velocity);
                stickyRB._velocity = stickyRB._currentTriangle.ProjectDirectionOntoTriangle(stickyRB._velocity);
                Orientation.rotateToNewUp(stickyRB.transform, stickyRB._currentTriangle.GetFaceNormal());
                
                float thisTraveled = (stickyRB.transform.position - result.position).magnitude;
                stickyRB.transform.position = stickyRB._currentTriangle.ProjectLocationOntoTriangle(result.position);
                applyVelocity(stickyRB, traveled + thisTraveled, call + 1);
            }
            // Check if player will exit the tri this frame
            else if (tickVelocity.magnitude < exitVector.magnitude)
            {
                // Will not exit
                stickyRB.transform.position = stickyRB._currentTriangle.ProjectLocationOntoTriangle(stickyRB.transform.position + tickVelocity);
            }
            else
            {
                // Check if there is a neighboring tri
                Triangle oldTri = stickyRB._currentTriangle;
                result = BorderCheck.checkBorder(stickyRB._currentTriangle, exitPoint, tickVelocity, stickyRB._stickyMask);
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