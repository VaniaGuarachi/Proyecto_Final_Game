using UnityEngine;

[DisallowMultipleComponent]
public class CameraFollow2D : MonoBehaviour
{
    [Header("Seguimiento")]
    public Transform target;
    public Vector3 offset = new Vector3(0f, 1.4f, -10f);
    public float smoothTime = 0.16f;

    [Header("Limites")]
    public bool useLimits;
    public Vector2 minPosition = new Vector2(-100f, -20f);
    public Vector2 maxPosition = new Vector2(100f, 40f);

    private Vector3 velocity;

    private void Awake()
    {
        if (target == null)
        {
            target = FindPlayer();
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            target = FindPlayer();
        }

        if (target == null)
        {
            return;
        }

        Vector3 desiredPosition = target.position + offset;

        if (useLimits)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minPosition.x, maxPosition.x);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, minPosition.y, maxPosition.y);
        }

        desiredPosition.z = offset.z;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
    }

    private Transform FindPlayer()
    {
        GameObject player = GameObject.Find("Player1");

        if (player != null)
        {
            return player.transform;
        }

        Player1 playerScript = FindFirstObjectByType<Player1>();
        return playerScript != null ? playerScript.transform : null;
    }
}

public static class CameraFollowBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void SetupCameraFollow()
    {
        Camera mainCamera = Camera.main;

        if (mainCamera != null && mainCamera.GetComponent<CameraFollow2D>() == null)
        {
            mainCamera.gameObject.AddComponent<CameraFollow2D>();
        }
    }
}
