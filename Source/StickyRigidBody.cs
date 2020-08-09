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
        StickyPhysics.Utility.fixedUpdateStep(this);
    }

    public void AfterPhysicsUpdate()
    {
        StickyPhysics.Utility.afterPhysicsStep(this);
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
