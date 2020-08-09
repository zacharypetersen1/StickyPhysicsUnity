using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StickyPhysics
{
    public class Triangle
    {
        public Transform _ownerTransform; // Transform component of gameobject that owns this triangle
        public int _triangleIndex;
        private Vector3[] _vertexPositions;     // In world position
        private Vector3[] _vertexNormals;
        private Vector3 _faceNormal;

        // Construct using mesh and triangle index from raycast
        public Triangle(Transform setTransform, Vector3 setNormal, MeshCollider mc, int setTriangleIndex)
        {
            _faceNormal = setNormal;
            _ownerTransform = setTransform;
            _triangleIndex = setTriangleIndex;

            Mesh mesh = mc.sharedMesh;
            _vertexPositions = new Vector3[]{
                _ownerTransform.TransformPoint(mesh.vertices[mesh.triangles[_triangleIndex*3]]),
                _ownerTransform.TransformPoint(mesh.vertices[mesh.triangles[_triangleIndex*3 + 1]]),
                _ownerTransform.TransformPoint(mesh.vertices[mesh.triangles[_triangleIndex*3 + 2]])
            };
            _vertexNormals = new Vector3[]
            {
                _ownerTransform.TransformDirection(mesh.normals[mesh.triangles[_triangleIndex*3]]),
                _ownerTransform.TransformDirection(mesh.normals[mesh.triangles[_triangleIndex*3 + 1]]),
                _ownerTransform.TransformDirection(mesh.normals[mesh.triangles[_triangleIndex*3 + 2]])
            };
        }

        public Vector3 getFaceNormal()
        {
            return _faceNormal;
        }

        public Vector3 getInterpolatedNormal(Vector3 position)
        {
            Vector3 baryCoords = getBarycentricCoords(position);
            return _vertexNormals[0] * baryCoords.x + _vertexNormals[1] * baryCoords.y + _vertexNormals[2] * baryCoords.z;
        }

        public Vector3 getBarycentricCoords(Vector3 pos)
        {
            Vector3 v0 = _vertexPositions[1] - _vertexPositions[0],
                v1 = _vertexPositions[2] - _vertexPositions[0],
                v2 = pos - _vertexPositions[0];
            float d00 = Vector3.Dot(v0, v0);
            float d01 = Vector3.Dot(v0, v1);
            float d11 = Vector3.Dot(v1, v1);
            float d20 = Vector3.Dot(v2, v0);
            float d21 = Vector3.Dot(v2, v1);
            float denom = d00 * d11 - d01 * d01;
            float v = (d11 * d20 - d01 * d21) / denom;
            float w = (d00 * d21 - d01 * d20) / denom;
            float u = 1.0f - v - w;
            return new Vector3(u, v, w);
        }

        // Projects a vector onto the plane that this triangle lies on
        public Vector3 projectOntoPlane(Vector3 v)
        {
            // Transform so first vert is at origin, project, then transform back 
            return Vector3.ProjectOnPlane(v - _vertexPositions[0], _faceNormal) + _vertexPositions[0];
        }

        // Projects vector onto plane originating from origin
        public Vector3 projectOntoPlaneFromOrigin(Vector3 v)
        {
            return Vector3.ProjectOnPlane(v, _faceNormal);
        }

        // Projects ray onto boundaries of tri. Returns false if no boundaries are hit.
        public bool projectOntoBoundaries(Ray r, out Vector3 point)
        {
            point = Vector3.zero;
            bool foundIntersect = false;
            float distance = Mathf.Infinity;

            // Construct plane for each of tri's edges and try raycast
            for (int i = 0; i < 3; i++)
            {
                Plane p = new Plane(_vertexPositions[(i + 1) % 3], _vertexPositions[i], _vertexPositions[i] + _faceNormal);
                float result;
                if (p.Raycast(r, out result))
                {
                    // If this hit is closest hit, set this as intersection point
                    if (result <= distance)
                    {
                        foundIntersect = true;
                        distance = result;
                    }
                }
            }

            // Assign point if projection succeded
            if (foundIntersect)
                point = r.origin + r.direction * distance;

            return foundIntersect;
        }

        // Converts tri to something readable
        public override string ToString()
        {
            return string.Format("V0:{0} V1:{1} V2{2} N:{3}", _vertexPositions[0], _vertexPositions[1], _vertexPositions[2], _faceNormal);
        }

        // Draw the triangle for debugging
        public void debugRender(Color c)
        {
            Debug.DrawLine(_vertexPositions[0], _vertexPositions[1], c);
            Debug.DrawLine(_vertexPositions[1], _vertexPositions[2], c);
            Debug.DrawLine(_vertexPositions[2], _vertexPositions[0], c);
        }
    }
}