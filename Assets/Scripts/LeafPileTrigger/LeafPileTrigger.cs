using UnityEngine;

public class LeafPileTrigger : MonoBehaviour
{
    [SerializeField] private ParticleSystem leafParticles;
    [SerializeField] private string playerTag = "Player";

    // optional — hide the mesh when hit
    [SerializeField] private bool hideOnTrigger = true;
    [SerializeField] private MeshRenderer leafMesh;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        leafParticles.Play();

        if (hideOnTrigger && leafMesh != null)
            leafMesh.enabled = false;
    }
}