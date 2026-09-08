using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(Rigidbody))]
public class Spell : MonoBehaviour
{
    public SpellScriptableObject spellToCast;

    public SphereCollider sphereCollider;
    public Rigidbody rigidbody;

    private void Awake()
    {
        sphereCollider=GetComponent<SphereCollider>();
        sphereCollider.isTrigger=true;
        sphereCollider.radius = spellToCast.spellRadius;
        
        rigidbody=GetComponent<Rigidbody>();
        rigidbody.isKinematic=true;
        
        Destroy(this.gameObject, spellToCast.lifeTime);
    }

    private void Update()
    {
        if (spellToCast.speed > 0) 
            transform.Translate(Vector3.forward * (spellToCast.speed * Time.deltaTime));
    }

    private void OnTriggerEnter(Collider other)
    {
        // apply spell effects to whatever we hit.
        // apply hit particle effects
        // apply sound effects

        if (other.gameObject.CompareTag("Enemy")) ;
            // Damage to the hit enemy's health
        
        Destroy(this.gameObject);
    }
}
