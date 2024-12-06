using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController instance;
    public static CameraController Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<CameraController>();
                if (instance == null)
                {
                    GameObject singleton = new GameObject(typeof(CameraController).Name);
                    instance = singleton.AddComponent<CameraController>();
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    public float dragSpeedBase = 2.0f; // 基础拖动速度
    public float minSize = 5.0f; // 正交相机最小 size
    public float maxSize = 20.0f; // 正交相机最大 size
    public BoxCollider groundCollider; // 参考的地面 Collider
    private Vector3 lastMousePosition;
    private Camera camera;

    void Start()
    {
        camera = GetComponent<Camera>(); // 获取相机组件
    }

    void Update()
    {
        // 检测鼠标右键是否按下
        if (Input.GetMouseButtonDown(1)) // 右键
        {
            lastMousePosition = Input.mousePosition; // 记录鼠标当前位置
            return;
        }

        // 检测鼠标右键是否保持按下状态
        if (Input.GetMouseButton(1)) // 右键
        {
            // Vector3 currentMousePosition = Input.mousePosition;
            // Vector3 delta = lastMousePosition - currentMousePosition;
            Vector3 delta = Input.mousePosition - lastMousePosition; // 计算鼠标移动量

            // 动态调整拖动速度
            float dragSpeed = dragSpeedBase * (camera.orthographicSize / maxSize) * (Screen.dpi / 96.0f);

            Vector3 move = new Vector3(-delta.x, 0, -delta.y) * dragSpeed * Time.deltaTime; // 计算移动向量
            Vector3 newPosition = transform.position + move;

            // 限制相机在地面边界内移动
            newPosition.x = Mathf.Clamp(newPosition.x, groundCollider.bounds.min.x + camera.orthographicSize * camera.aspect, groundCollider.bounds.max.x - camera.orthographicSize * camera.aspect);
            newPosition.z = Mathf.Clamp(newPosition.z, groundCollider.bounds.min.z + camera.orthographicSize, groundCollider.bounds.max.z - camera.orthographicSize);
            transform.position = newPosition;

            lastMousePosition = Input.mousePosition;
        }

        // 鼠标滚轮调整相机大小
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            camera.orthographicSize -= scroll * 5; // 调整缩放速度，乘以一个因子
            camera.orthographicSize = Mathf.Clamp(camera.orthographicSize, minSize, maxSize); // 限制 size 在 minSize 和 maxSize 之间

            // 调整相机位置以确保不超出边界
            RestrictCameraToBounds();
        }
    }

    // 限制相机在地面边界内
    private void RestrictCameraToBounds()
    {
        Vector3 newPosition = transform.position;

        // 计算相机的视野范围
        float width = camera.orthographicSize * camera.aspect;
        float height = camera.orthographicSize;

        newPosition.x = Mathf.Clamp(newPosition.x, groundCollider.bounds.min.x + width, groundCollider.bounds.max.x - width);
        newPosition.z = Mathf.Clamp(newPosition.z, groundCollider.bounds.min.z + height, groundCollider.bounds.max.z - height);

        transform.position = newPosition;
    }
}