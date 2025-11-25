using UnityEngine;

public class AcidDamage : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public float damagePerSecond = 10f;
    public float damageTickRate = 0.5f;

    private bool playerInHazard = false;
    private Controller playerController;
    private float damageTimer = 0f;

    void OnTriggerEnter(Collider other)
    {
        Controller controller = other.GetComponent<Controller>();
        if (controller != null)
        {
            playerInHazard = true;
            playerController = controller;
            damageTimer = 0f;
            Debug.Log("Player entered acid pool!");
        }
    }

    void OnTriggerExit(Collider other)
    {
        Controller controller = other.GetComponent<Controller>();
        if (controller != null)
        {
            playerInHazard = false;
            playerController = null;
            Debug.Log("Player exited acid pool!");
        }
    }

    void Update()
    {
        if (!playerInHazard || playerController == null)
            return;

        damageTimer += Time.deltaTime;

        if (damageTimer >= damageTickRate)
        {
            float damage = damagePerSecond * damageTickRate;
            playerController.TakeDamage(damage);
            damageTimer = 0f;
        }
    }
}
