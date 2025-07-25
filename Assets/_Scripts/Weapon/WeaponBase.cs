using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public abstract class WeaponBase : NetworkBehaviour
{
    public Weapons weaponID;

    // weapon stats
    protected abstract int baseDamage { get; set; }
    protected abstract float critMultiplier { get; set; }
    protected abstract float limbMultiplier { get; set; }
    public abstract WeaponSlot WeaponSlot { get; }
    public abstract float MaxVelocity { get; }

    protected float fireDelay;
    [SerializeField] protected float fireDelayMax = 0.11f;


    // behaviour while detached from a rebel (i.e. on the floor)
    [SerializeField] Rigidbody _rigidbody { get; set; }
    [SerializeField] Collider _collider { get; set; }

    [SerializeField] protected GameObject ShootSfx;


    [SerializeField] public Rebel AttachedRebel;
    public NetworkVariable<NetworkObjectReference> AttachedRebelNetObjRef { get; set; } = new NetworkVariable<NetworkObjectReference>();

    public NetworkVariable<bool> IsDetached { get; private set; } = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public void SetIsDetachedIfOwner(bool val)
    {
        if (IsOwner)
        {
            // handles case where prev value matches the new value
            // (e.g. when default value matches new value)
            if (IsDetached.Value == val) ToggleColliderAndRigidbodyRpc(val);

            IsDetached.Value = val;
        }
    }


    public override void OnNetworkSpawn()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();

        AttachedRebelNetObjRef.OnValueChanged += OnAttachedRebelNetObjIDChanged;
        IsDetached.OnValueChanged += OnIsDetachedChanged;
    }


    protected virtual void OnAttachedRebelNetObjIDChanged(NetworkObjectReference prev, NetworkObjectReference curr)
    {
        curr.TryGet(out NetworkObject t);
        AttachedRebel = t.GetComponent<Rebel>();

        AttachedRebel.Attach(gameObject, weaponID, WeaponSlot);
    }

    void Update() => OnUpdate();
    protected virtual void OnUpdate() { }

    void FixedUpdate() => OnFixedUpdate();
    protected virtual void OnFixedUpdate() { }

    private void OnIsDetachedChanged(bool prev,  bool curr)
    {
        // if detaching: reset attached rebel
        if (curr) AttachedRebel = null;

        ToggleColliderAndRigidbody(curr);
    }

    [Rpc(SendTo.Everyone)]
    private void ToggleColliderAndRigidbodyRpc(bool isDetached) => ToggleColliderAndRigidbody(isDetached);

    private void ToggleColliderAndRigidbody(bool isDetached)
    {
        if (_collider != null) _collider.enabled = isDetached;
        if (_rigidbody != null) _rigidbody.isKinematic = !isDetached;
    }


    public abstract void Shoot();

    private void OnCollisionEnter(Collision collision)
    {
        if (collision != null && collision.gameObject.layer == (int)Layers.SolidGround)
            _rigidbody.isKinematic = true;
    }
}
