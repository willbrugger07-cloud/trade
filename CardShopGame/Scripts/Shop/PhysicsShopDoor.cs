using System.Collections;
using UnityEngine;

/// <summary>
/// Physics hinge door. Spring pulls it closed; collision triggers squeak.
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(HingeJoint))]
public class PhysicsShopDoor : MonoBehaviour
{
    public AudioSource squeakAudio;
    private bool _squeaked;

    private void Start()
    {
        var hinge = GetComponent<HingeJoint>();

        var limits = hinge.limits;
        limits.min       = -90f;
        limits.max       =  90f;
        hinge.useLimits  = true;
        hinge.limits     = limits;

        hinge.useSpring  = true;
        var spring       = hinge.spring;
        spring.spring    = 15f;
        spring.damper    = 2f;
        hinge.spring     = spring;
    }

    private void OnCollisionEnter(Collision col)
    {
        if (_squeaked) return;
        if (!col.gameObject.CompareTag("Player") && !col.gameObject.CompareTag("Customer")) return;

        _squeaked = true;
        squeakAudio?.Play();
        StartCoroutine(ResetSqueak());
    }

    private IEnumerator ResetSqueak()
    {
        yield return new WaitForSeconds(2f);
        _squeaked = false;
    }
}
