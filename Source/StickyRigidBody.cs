using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class StickyRigidBody : MonoBehaviour
{
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
            StickyPhysics.Utility.applyVelocity(this, 0, 0);
        }
    }

    public void AfterPhysicsUpdate()
    {
        // When character is in the air, check to see if grounded
        if (!_isSticking)
        {
            Vector3 deltaPosition = transform.position - _lastPosition;
            StickyPhysics.Utility.checkIfGrounded(this, deltaPosition);
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
}
