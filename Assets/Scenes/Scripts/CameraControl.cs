using UnityEngine;

public class CameraControl : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private Vector3 defaultPos = new Vector3(250f, 30f, 90f);
    [SerializeField] private float speed = 40f;
    [SerializeField] private float zoomSensitivity = 600f;

    [Header("Bounds")]
    [SerializeField] private float minX = 0f;
    [SerializeField] private float maxX = 500f;
    [SerializeField] private float minZ = 0f;
    [SerializeField] private float maxZ = 300f;
    [SerializeField] private float minY = 5f;
    [SerializeField] private float maxY = 100f;

    private void Start()
    {
        transform.position = defaultPos;
        transform.rotation = Quaternion.Euler(45f, 0f, 0f);
    }

    private void Update()
    {
        Move();
        Zoom();
    }

    private void Move()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector3 direction = new Vector3(x, 0, z).normalized;

        if (direction.magnitude >= 0.1f)
        {
            transform.Translate(direction * speed * Time.deltaTime, Space.World);
        }

        // Map Constraints
        float px = Mathf.Clamp(transform.position.x, minX, maxX);
        float pz = Mathf.Clamp(transform.position.z, minZ, maxZ);

        transform.position = new Vector3(px, transform.position.y, pz);
    }

    private void Zoom()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scrollInput) > 0.01f)
        {
            Vector3 targetZoom = transform.position + (transform.forward * scrollInput * zoomSensitivity);
            targetZoom.y = Mathf.Clamp(targetZoom.y, minY, maxY);

            transform.position = Vector3.Lerp(transform.position, targetZoom, Time.deltaTime * 10f);
        }
    }
}