using UnityEngine;

namespace StickyPhysics
{
    public class Triangle
    {
        public Transform _ownerTransform; // Transform component of gameobject that owns this triangle
        public int _triangleIndex;
        private Vector3[] _objectVertexPositions;     // In world position
        private Vector3[] _objectVertexNormals;
        private Vector3[] _worldVertexPositions;
        private Vector3[] _worldVertexNormals;
        private Vector3 _objectFaceNormal;
        private Vector3 _worldFaceNormal;

        // Construct using mesh and triangle index from raycast
        public Triangle()
        {

        }

        public static Triangle ConstructTriangleFromRaycastHit(RaycastHit hitResult)
        {
            Triangle triangle = new Triangle();
            triangle._ownerTransform = hitResult.transform;
            triangle._triangleIndex = hitResult.triangleIndex;

            MeshCollider meshCollider = hitResult.collider as MeshCollider;
            Mesh mesh = meshCollider.sharedMesh;
            triangle._objectVertexPositions = new Vector3[]{
                mesh.vertices[mesh.triangles[hitResult.triangleIndex*3]],
                mesh.vertices[mesh.triangles[hitResult.triangleIndex*3 + 1]],
                mesh.vertices[mesh.triangles[hitResult.triangleIndex*3 + 2]]
            };
            triangle._objectVertexNormals = new Vector3[]
            {
                mesh.normals[mesh.triangles[hitResult.triangleIndex*3]],
                mesh.normals[mesh.triangles[hitResult.triangleIndex*3 + 1]],
                mesh.normals[mesh.triangles[hitResult.triangleIndex*3 + 2]]
            };

            triangle._objectFaceNormal = Vector3.Cross(
                triangle._objectVertexPositions[1] - triangle._objectVertexPositions[0],
                triangle._objectVertexPositions[2] - triangle._objectVertexPositions[0]
            ).normalized;

            triangle._worldVertexPositions = new Vector3[3];
            triangle._worldVertexNormals = new Vector3[3];
            triangle.TransformObjectDataToWorld();

            return triangle;
        }

        private void TransformObjectDataToWorld()
        {
            for(int i = 0; i < 3; i++)
            {
                _worldVertexPositions[i] = _ownerTransform.TransformPoint(_objectVertexPositions[i]);
                _worldVertexNormals[i] = _ownerTransform.TransformDirection(_objectVertexNormals[i]);
            }
            _worldFaceNormal = _ownerTransform.TransformDirection(_objectFaceNormal);
        }

        private Vector3 GetWorldVertexPosition(int index)
        {
            if (_ownerTransform.hasChanged)
            {
                TransformObjectDataToWorld();
                _ownerTransform.hasChanged = false;
            }
            return _ownerTransform.TransformPoint(_objectVertexPositions[index]);
        }

        private Vector3 GetWorldVertexNormal(int index)
        {
            if (_ownerTransform.hasChanged)
            {
                TransformObjectDataToWorld();
                _ownerTransform.hasChanged = false;
            }
            return _ownerTransform.TransformDirection(_objectVertexNormals[index]);
        }

        public Vector3 GetWorldFaceNormal()
        {
            if (_ownerTransform.hasChanged)
            {
                TransformObjectDataToWorld();
                _ownerTransform.hasChanged = false;
            }
            return _ownerTransform.TransformDirection(_objectFaceNormal);
        }

        public Vector3 GetWorldInterpolatedNormal(Vector3 atThisLocation)
        {
            Vector3 baryCoords = GetBarycentricCoords(atThisLocation);
            return GetWorldVertexNormal(0) * baryCoords.x + GetWorldVertexNormal(1) * baryCoords.y + GetWorldVertexNormal(2) * baryCoords.z;
        }

        // Projects vector onto plane originating from origin
        public Vector3 ProjectDirectionOntoTriangle(Vector3 direction)
        {
            return Vector3.ProjectOnPlane(direction, GetWorldFaceNormal());
        }

        // Projects a vector onto the plane that this triangle lies on
        public Vector3 ProjectLocationOntoTriangle(Vector3 location)
        {
            // Transform so first vert is at origin, project, then transform back 
            return Vector3.ProjectOnPlane(location - GetWorldVertexPosition(0), GetWorldFaceNormal()) + GetWorldVertexPosition(0);
        }

        // Projects ray onto boundaries of tri. Returns false if no boundaries are hit.
        public bool ProjectRayOntoBoundaries(out Vector3 resultLocation, Ray ray)
        {
            resultLocation = Vector3.zero;
            bool foundIntersect = false;
            float distance = Mathf.Infinity;

            // Construct plane for each of tri's edges and try raycast
            for (int i = 0; i < 3; i++)
            {
                Plane plane = new Plane(GetWorldVertexPosition((i + 1) % 3), GetWorldVertexPosition(i), GetWorldVertexPosition(i) + GetWorldFaceNormal());
                float result;
                if (plane.Raycast(ray, out result))
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
                resultLocation = ray.origin + ray.direction * distance;

            return foundIntersect;
        }

        // Converts tri to something readable
        public override string ToString()
        {
            return string.Format("V0:{0} V1:{1} V2{2} N:{3}", GetWorldVertexPosition(0), GetWorldVertexPosition(1), GetWorldVertexPosition(2), GetWorldFaceNormal());
        }

        // Draw the triangle for debugging
        public void DebugDraw(Color color)
        {
            Debug.DrawLine(GetWorldVertexPosition(0), GetWorldVertexPosition(1), color);
            Debug.DrawLine(GetWorldVertexPosition(1), GetWorldVertexPosition(2), color);
            Debug.DrawLine(GetWorldVertexPosition(2), GetWorldVertexPosition(0), color);
        }

        private Vector3 GetBarycentricCoords(Vector3 position)
        {
            Vector3 v0 = GetWorldVertexPosition(1) - GetWorldVertexPosition(0),
                v1 = GetWorldVertexPosition(2) - GetWorldVertexPosition(0),
                v2 = position - GetWorldVertexPosition(0);
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
    }
}