using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StickyPhysics
{
    public static class BorderCheck
    {
        public struct Result
        {
            public enum Outcomes {hitNothing, hitSelf, success }
            public Outcomes outcome;
            public Triangle triangle;
            public Vector3 position;
            public Result(Outcomes setOutcome)
            {
                outcome = setOutcome;
                triangle = null;
                position = Vector3.zero;
            }
        }

        public static Result checkBorder(Triangle curTri, Vector3 borderPos, Vector3 velocity, LayerMask mask)
        {
            float size1 = 0.05f;
            float spread = 6f;
            float size2 = 0.2f;

            Result r;
            r = checkTriangle(curTri, borderPos, velocity, size1, 0, mask);
            if(r.outcome == Result.Outcomes.success)
                return r;
            r = checkTriangle(curTri, borderPos, velocity, size1, spread, mask);
            if (r.outcome == Result.Outcomes.success)
                return r;
            r = checkTriangle(curTri, borderPos, velocity, size1, -spread, mask);
            if (r.outcome == Result.Outcomes.success)
                return r;
            r = checkTriangle(curTri, borderPos, velocity, size2, 0, mask);
            return r;
        }

        static Result checkTriangle(Triangle curTri, Vector3 borderPos, Vector3 direction, float size, float angle, LayerMask mask)
        {
            // Move StartPos back
            Vector3 startPos = borderPos - (direction.normalized * (size / 3));

            // Adjust direction by given angle
            if (angle != 0)
            {
                direction = Quaternion.AngleAxis(angle, curTri.GetFaceNormal()) * direction;
                direction = Vector3.ProjectOnPlane(direction, curTri.GetFaceNormal()).normalized;
            }

            // Cast away from start pos along normal and velocity
            Vector3 cast1 = (curTri.GetFaceNormal() + direction.normalized).normalized * size;
            Result r = checkRay(curTri, startPos + curTri.GetFaceNormal() * float.Epsilon, cast1, size, mask);
            switch (r.outcome)
            {
                case Result.Outcomes.success:
                case Result.Outcomes.hitSelf:
                    return r;
            }

            // Cast along negative of normal from where last cast ended
            r = checkRay(curTri, startPos + cast1, -curTri.GetFaceNormal(), size * 1.41421356237f, mask);
            switch (r.outcome)
            {
                case Result.Outcomes.success:
                case Result.Outcomes.hitSelf:
                    return r;
            }

            // Cast back toward border point from where last cast ended
            Vector3 cast2 = (direction.normalized - curTri.GetFaceNormal()).normalized * size;
            r = checkRay(curTri, startPos + cast2 - curTri.GetFaceNormal() * Mathf.Epsilon, -cast2, size, mask);
            return r;
        }

        public static Result checkRay(Triangle curTri, Vector3 origin, Vector3 direction, float length, LayerMask mask)
        {
            //Debug.DrawRay(origin, direction.normalized * length, Color.yellow);
            RaycastHit hit;
            if(Physics.Raycast(origin, direction.normalized, out hit, length, mask))
            {
                if (hit.transform == curTri._ownerTransform && hit.triangleIndex == curTri._triangleIndex)
                {
                    // TODO check for same transform
                    Debug.Log("Hit current triangle");
                    //DebugExtension.DebugPoint(hit.point);
                    return new Result(Result.Outcomes.hitSelf);
                }
                Result r = new Result(Result.Outcomes.success);
                r.triangle = new Triangle(hit.transform, hit.normal, hit.collider as MeshCollider, hit.triangleIndex);
                r.position = hit.point;
                return r;

            }
            return new Result(Result.Outcomes.hitNothing);
        }
    }
}

