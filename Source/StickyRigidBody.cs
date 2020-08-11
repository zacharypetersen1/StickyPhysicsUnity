using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class StickyRigidBody : MonoBehaviour
{
    struct BorderResult
    {
        public enum Outcomes { hitNothing, hitSelf, success }
        public Outcomes outcome;
        public StickyPhysics.Triangle triangle;
        public Vector3 position;
        public BorderResult(Outcomes setOutcome)
        {
            outcome = setOutcome;
            triangle = null;
            position = Vector3.zero;
        }
    }

    public LayerMask _stickyMask;

    [HideInInspector]
    public Vector3 _impulseForce = Vector3.zero;
    [HideInInspector]
    public Rigidbody _rigidBody;
    [HideInInspector]
    public bool _isSticking = true;

    // Movement Data
    [HideInInspector]
    public Vector3 _velocity;
    [HideInInspector]
    public Vector3 _lastPosition;
    public StickyPhysics.Triangle _currentTriangle;

    void Start()
    {
        StickyPhysics.AfterUnityPhysics.init();
        StickyPhysics.AfterUnityPhysics.add(this);
        _rigidBody = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (_isSticking)
        {
            Vector3 forceOnTrianglePlane = _currentTriangle.ProjectDirectionOntoTriangle(_impulseForce);
            if (_velocity.magnitude > 0)
            {
                Vector3 friction = -_velocity * (_velocity.magnitude * 0.0022f);
                forceOnTrianglePlane += friction;
            }
            Vector3 acceleration = forceOnTrianglePlane;
            _velocity = _currentTriangle.ProjectDirectionOntoTriangle(_velocity + acceleration);
            _impulseForce = Vector3.zero;
            ApplyVelocity(0, 0);
        }
    }

    public void AfterPhysicsUpdate()
    {
        // When character is in the air, check to see if grounded
        if (!_isSticking)
        {
            Vector3 deltaPosition = transform.position - _lastPosition;
            RaycastHit hitResultData;
            if (ShouldStartSticking(out hitResultData, deltaPosition))
            {
                StartSticking(hitResultData);
            }
        }
        _lastPosition = transform.position;
    }

    // Causes stickyRB to unstick from surface if attached
    public void UnStick()
    {
        if (_isSticking)
        {
            _isSticking = false;
            _rigidBody.isKinematic = false;
            _velocity = Vector3.zero;
        }
    }

    public void AddImpulseForce(Vector3 force)
    {
        _impulseForce += force;
    }

    private bool ShouldStartSticking(out RaycastHit dataForStartSticking, Vector3 deltaPosition)
    {
        Ray ray = new Ray(transform.position - deltaPosition.normalized * 0.2f, deltaPosition);
        return Physics.Raycast(ray, out dataForStartSticking, deltaPosition.magnitude + 0.4f, _stickyMask);
    }

    private void StartSticking(RaycastHit dataForStartSticking)
    {
        Vector3 velocity = _rigidBody.velocity;
        _isSticking = true;
        _rigidBody.isKinematic = true;
        transform.position = dataForStartSticking.point;
        _currentTriangle = new StickyPhysics.Triangle(dataForStartSticking.transform, dataForStartSticking.normal, dataForStartSticking.collider as MeshCollider, dataForStartSticking.triangleIndex);
        Vector3 forward = _currentTriangle.ProjectDirectionOntoTriangle(transform.forward);
        if (forward.magnitude < Mathf.Epsilon)
        {
            forward = _currentTriangle.ProjectDirectionOntoTriangle(transform.up);
        }
        transform.LookAt(transform.position + forward, _currentTriangle.GetFaceNormal());
        AddImpulseForce(velocity);
    }

    // Applies velocity based on surfing physics
    private void ApplyVelocity(float traveled, int call)
    {
        transform.position = _currentTriangle.ProjectLocationOntoTriangle(transform.position);
        Vector3 tickVelocity = _velocity.normalized * ((_velocity.magnitude * Time.fixedDeltaTime) - traveled);

        // Find exit point and exit vector
        Vector3 exitPoint;
        _currentTriangle.ProjectRayOntoBoundaries(out exitPoint, new Ray(transform.position, tickVelocity));
        exitPoint = _currentTriangle.ProjectLocationOntoTriangle(exitPoint);
        Vector3 exitVector = exitPoint - transform.position;

        // Check if player will cross over to a new triangle
        BorderResult borderResult = CastRayToLookForBorder(
            transform.position + _currentTriangle.GetFaceNormal() * 0.01f,
            tickVelocity.normalized,
            Mathf.Min(tickVelocity.magnitude, exitVector.magnitude)
        );

        if (borderResult.outcome == BorderResult.Outcomes.success)
        {
            float thisTraveled = (transform.position - borderResult.position).magnitude;
            CrossBorderNotSmooth(borderResult);
            ApplyVelocity(traveled + thisTraveled, call + 1);
        }
        // Check if player will exit the tri this frame
        else if (tickVelocity.magnitude < exitVector.magnitude)
        {
            // Will not exit
            transform.position = _currentTriangle.ProjectLocationOntoTriangle(transform.position + tickVelocity);
        }
        else
        {
            // Check if there is a neighboring tri
            StickyPhysics.Triangle oldTri = _currentTriangle;
            borderResult = LookForBorderAtLocation(exitPoint);
            if (borderResult.outcome == BorderResult.Outcomes.success)
            {
                // Update orientation of stickyVelocity and stickyRB's object             
                CrossBorderSmoothly(borderResult);

                // Make another call to apply remaining velocity on new tri
                ApplyVelocity(traveled + exitVector.magnitude, call + 1);
            }
            else
            {
                // TODO Switch to non-surfing physics
                Debug.Log("No tri detected");
            }
        }
    }

    private BorderResult LookForBorderAtLocation(Vector3 location)
    {
        float size1 = 0.05f;
        float spread = 6f;
        float size2 = 0.2f;

        BorderResult result;

        result = CastTriangleToLookForBorder(location, _velocity, size1, 0);
        if (result.outcome == BorderResult.Outcomes.success)
            return result;

        result = CastTriangleToLookForBorder(location, _velocity, size1, spread);
        if (result.outcome == BorderResult.Outcomes.success)
            return result;

        result = CastTriangleToLookForBorder(location, _velocity, size1, -spread);
        if (result.outcome == BorderResult.Outcomes.success)
            return result;

        result = CastTriangleToLookForBorder(location, _velocity, size2, 0);
        return result;
    }

    private BorderResult CastTriangleToLookForBorder(Vector3 borderPos, Vector3 direction, float size, float angle)
    {
        // Move StartPos back
        Vector3 startPos = borderPos - (direction.normalized * (size / 3));

        // Adjust direction by given angle
        if (angle != 0)
        {
            direction = Quaternion.AngleAxis(angle, _currentTriangle.GetFaceNormal()) * direction;
            direction = Vector3.ProjectOnPlane(direction, _currentTriangle.GetFaceNormal()).normalized;
        }

        // Cast away from start pos along normal and velocity
        Vector3 cast1 = (_currentTriangle.GetFaceNormal() + direction.normalized).normalized * size;
        BorderResult borderResult = CastRayToLookForBorder(startPos + _currentTriangle.GetFaceNormal() * float.Epsilon, cast1, size);
        switch (borderResult.outcome)
        {
            case BorderResult.Outcomes.success:
            case BorderResult.Outcomes.hitSelf:
                return borderResult;
        }

        // Cast "down" along negative of normal
        borderResult = CastRayToLookForBorder(startPos + cast1, -_currentTriangle.GetFaceNormal(), size * 1.41421356237f);
        switch (borderResult.outcome)
        {
            case BorderResult.Outcomes.success:
            case BorderResult.Outcomes.hitSelf:
                return borderResult;
        }

        // Cast back toward starting point
        Vector3 cast2 = (direction.normalized - _currentTriangle.GetFaceNormal()).normalized * size;
        borderResult = CastRayToLookForBorder(startPos + cast2 - _currentTriangle.GetFaceNormal() * Mathf.Epsilon, -cast2, size);
        return borderResult;
    }

    private BorderResult CastRayToLookForBorder(Vector3 origin, Vector3 direction, float length)
    {
        RaycastHit raycastHit;
        if (Physics.Raycast(origin, direction.normalized, out raycastHit, length, _stickyMask))
        {
            if (raycastHit.transform == _currentTriangle._ownerTransform && raycastHit.triangleIndex == _currentTriangle._triangleIndex)
            {
                Debug.Log("Hit current triangle");
                return new BorderResult(BorderResult.Outcomes.hitSelf);
            }
            BorderResult borderResult = new BorderResult(BorderResult.Outcomes.success);
            borderResult.triangle = new StickyPhysics.Triangle(raycastHit.transform, raycastHit.normal, raycastHit.collider as MeshCollider, raycastHit.triangleIndex);
            borderResult.position = raycastHit.point;
            return borderResult;

        }
        return new BorderResult(BorderResult.Outcomes.hitNothing);
    }

    private void CrossBorderSmoothly(BorderResult result)
    {
        // Store angle between velocity and forward
        float angle = UTL_Math.angleBetweenVectors(transform.forward, _velocity, transform.up) * Mathf.Rad2Deg;

        // Get new Forward
        Vector3 newForward = PredictForwardAfterCrossBorder(result.triangle, result.position, transform.forward).normalized;

        // Adjust transform
        transform.position = _currentTriangle.ProjectLocationOntoTriangle(result.position);
        transform.LookAt(transform.position + newForward, result.triangle.GetFaceNormal());

        // Calculate new velocity
        Vector3 newVelocity = Quaternion.AngleAxis(angle, result.triangle.GetFaceNormal()) * transform.forward;
        newVelocity = newVelocity.normalized * _velocity.magnitude;
        _velocity = Vector3.ProjectOnPlane(newVelocity, result.triangle.GetFaceNormal());

        // Update triangle
        _currentTriangle = result.triangle;
    }

    private Vector3 PredictForwardAfterCrossBorder(StickyPhysics.Triangle newTri, Vector3 position, Vector3 oldVector)
    {
        Vector3 interpolatedUp = newTri.GetInterpolatedNormal(position);
        Vector3 interpolatedForward = Vector3.ProjectOnPlane(oldVector.normalized, interpolatedUp).normalized;
        Vector3 newUp = newTri.GetFaceNormal();
        Vector3 newDirection = interpolatedForward - ((Vector3.Dot(interpolatedForward, newUp) / Vector3.Dot(interpolatedUp, newUp)) * interpolatedUp);
        return Vector3.ProjectOnPlane(newDirection, newUp).normalized * oldVector.magnitude;
    }

    // Rotates a vector from the space of oldNormal to the space of newNormal
    private void CrossBorderNotSmooth(BorderResult borderResult)
    {
        float dot = Vector3.Dot(_currentTriangle.GetFaceNormal(), borderResult.triangle.GetFaceNormal());

        // Account for imprecision
        dot = Mathf.Clamp(dot, -1.0f, 1.0f);

        // If vectors don't point same way
        if (dot != 1)
        {
            // Calculate degrees for rotations and rotation axis
            float rotationDegrees = Mathf.Acos(dot) * Mathf.Rad2Deg;
            Vector3 axis = Vector3.Cross(_currentTriangle.GetFaceNormal(), borderResult.triangle.GetFaceNormal());

            // Preform rotation
            _velocity = Quaternion.AngleAxis(rotationDegrees, axis) * _velocity;
            _velocity = borderResult.triangle.ProjectDirectionOntoTriangle(_velocity);
        }

        //TODO catch case where dot == -1?

        _currentTriangle = borderResult.triangle;
        RotateToNewUp(transform, _currentTriangle.GetFaceNormal());
        transform.position = _currentTriangle.ProjectLocationOntoTriangle(borderResult.position);
    }

    private void RotateToNewUp(Transform t, Vector3 targetUp)
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
