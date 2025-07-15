using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

public class RagDollDeadTimer : MonoBehaviour
{
    private float timer = 6f;
    private bool hasColliders = true;
    private void Update()
    {
        timer -=Time.deltaTime;

        if (hasColliders && timer <= 3)
        {
            foreach (CharacterJoint characterJoint in GetComponentsInChildren<CharacterJoint>() )
            {
                Destroy(characterJoint);
            }
            foreach(Rigidbody rigidbody in GetComponentsInChildren<Rigidbody>() )
            {
                Destroy(rigidbody);
            }
            foreach (UnityEngine.Collider collider in GetComponentsInChildren<UnityEngine.Collider>())
            {
                Destroy(collider);
            }
            hasColliders = false;
        }
        if (timer <= 1f)
        {
            transform.position += Vector3.down * Time.deltaTime;
        }

        if(timer <= 0f)
        {
            Destroy(gameObject);
        }
    }
}
