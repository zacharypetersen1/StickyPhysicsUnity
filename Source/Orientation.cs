using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StickyPhysics
{
    public static class Orientation
    {
        public static void smoothOrient(StickyRigidBody stickyRB, BorderCheck.Result result)
        {
            /*// Get new forward vector and adjust transform
            Vector3 newForward = Orientation.getNewSmoothedVector(result.triangle, result.position, stickyRB.transform.forward);
            stickyRB.velocity = newForward * stickyRB.velocity.magnitude;
            stickyRB.transform.position = stickyRB.curTri.projectOntoPlane(result.position);
            stickyRB.transform.LookAt(stickyRB.transform.position + newForward, result.triangle.getFaceNormal());

            // Update triangle
            stickyRB.curTri = result.triangle;*/

            // Find angle between velocity and forward
            float angle = UTL_Math.angleBetweenVectors(stickyRB.transform.forward, stickyRB._velocity, stickyRB.transform.up) * Mathf.Rad2Deg;

            // Get new Forward
            Vector3 newForward = Orientation.getNewSmoothedVector(result.triangle, result.position, stickyRB.transform.forward).normalized;

            // Adjust transform
            stickyRB.transform.position = stickyRB._currentTriangle.ProjectLocationOntoTriangle(result.position);
            stickyRB.transform.LookAt(stickyRB.transform.position + newForward, result.triangle.GetFaceNormal());

            // Calculate new velocity
            Vector3 newVelocity = Quaternion.AngleAxis(angle, result.triangle.GetFaceNormal()) * stickyRB.transform.forward;
            newVelocity = newVelocity.normalized * stickyRB._velocity.magnitude;
            stickyRB._velocity = Vector3.ProjectOnPlane(newVelocity, result.triangle.GetFaceNormal());

            // Update triangle
            stickyRB._currentTriangle = result.triangle;
        }

        public static Vector3 getNewSmoothedVector(Triangle newTri, Vector3 position, Vector3 oldVector)
        {
            //Debug.DrawLine(position, position + oldForward, Color.red);
            Vector3 interpolatedUp = newTri.GetInterpolatedNormal(position);
            //Debug.DrawLine(position, position + interpolatedUp, Color.yellow);
            Vector3 interpolatedForward = Vector3.ProjectOnPlane(oldVector.normalized, interpolatedUp).normalized;
            //Debug.DrawLine(position, position + interpolatedForward, Color.blue);
            Vector3 newUp = newTri.GetFaceNormal();
            Vector3 newDirection = interpolatedForward - ((Vector3.Dot(interpolatedForward, newUp) / Vector3.Dot(interpolatedUp, newUp)) * interpolatedUp);
            return Vector3.ProjectOnPlane(newDirection, newUp).normalized * oldVector.magnitude;
        }

        // Rotates a vector from the space of oldNormal to the space of newNormal
        public static Vector3 rotateVectorToNewPlane(Vector3 oldNormal, Vector3 newNormal, Vector3 v)
        {
            float dot = Vector3.Dot(oldNormal, newNormal);

            // Account for imprecision
            dot = Mathf.Clamp(dot, -1.0f, 1.0f);

            // Vectors point same way
            if (dot == 1)
            {
                return v;
            }

            //TODO catch case where dot == -1?

            // Calculate degrees for rotations and rotation axis
            float deg = Mathf.Acos(dot) * Mathf.Rad2Deg;
            Vector3 axis = Vector3.Cross(oldNormal, newNormal);

            // Preform rotation
            return Quaternion.AngleAxis(deg, axis) * v;
        }

        public static void rotateToNewUp(Transform t, Vector3 targetUp)
        {
            targetUp = targetUp.normalized;
            float dot = Vector3.Dot(t.up, targetUp);
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
                axis = Vector3.Cross(t.forward, targetUp);
            }
            else
            {
                // Normal case
                axis = Vector3.Cross(t.up, targetUp).normalized;
            }

            // Execute rotation
            float deg = Mathf.Acos(dot) * Mathf.Rad2Deg;
            t.Rotate(axis, deg, Space.World);
        }
    }
}
