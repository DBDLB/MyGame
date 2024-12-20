using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; } // 单例实例
    
    public float moveSpeed = 5f;      // 控制移动速度
    public float gravity = -9.81f;    // 控制重力
    // public float jumpHeight = 2f;     // 跳跃高度
    public float groundOffset = 0.5f; // 设置物体与地面之间的距离

    private CharacterController controller;
    private Vector3 velocity;         // 用于处理垂直方向的运动（如重力）
    private bool isGrounded;          // 是否在地面上
    private Vector3 lastMovementDirection = Vector3.zero; 

    void Awake()
    {
        // 确保只有一个PlayerController实例存在
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 保持在场景切换时不会被销毁
        }
        else
        {
            Destroy(gameObject); // 防止多个实例
        }
    }
    
    void Start()
    {
        // 获取CharacterController组件
        controller = GetComponent<CharacterController>();
    }
    
    protected void AimAtMouse()
    {
        // if(playerControlled)
        {
            // 创建一条从摄像机到鼠标指针的射线
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // 如果射线击中了场景中的对象
            if (Physics.Raycast(ray, out hit))
            {
                // 计算发射器朝向鼠标所在位置的方向
                Vector3 targetPoint = hit.point;

                // 调整目标点的y值为发射器的y值，这样发射器只在水平面上旋转
                targetPoint.y = transform.position.y;

                // 让发射器看向目标点
                transform.LookAt(targetPoint);
            }
        }
    }
    
    float down = 9.81f;
    void Update()
    {
        AimAtMouse();
        // 检测角色是否在地面上，groundCheck是一个轻微偏移的检测球体
        // isGrounded = controller.isGrounded;

        // if (isGrounded && velocity.y < 0)
        // {
        //     velocity.y = -2f; // 使得角色紧贴地面
        // }

        // 获取输入
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        // 计算移动方向
        Vector3 move = Vector3.Normalize( new Vector3(horizontalInput, 0f, verticalInput));
        
        if (move != Vector3.zero)
        {
            lastMovementDirection = move;  // 记录当前的移动方向

            // 使物体朝向移动的方向
            // transform.rotation = Quaternion.LookRotation(lastMovementDirection);
        }
        
        

        // 使用CharacterController移动角色
        controller.Move(move * moveSpeed * Time.deltaTime);

        // 跳跃处理
        // if (Input.GetButtonDown("Jump") && isGrounded)
        // {
        //     velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        // }
        

        // 垂直移动 (重力和跳跃)
        controller.Move(velocity * Time.deltaTime);
        
        

        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit))
        {
            float distanceToGround = hit.distance;
            if (distanceToGround < groundOffset)
            {
                Vector3 position = transform.position;
                position.y += groundOffset - distanceToGround;
                transform.position = position;
            }
            else
            {
                // 添加重力效果
                velocity.y += gravity * Time.deltaTime;
            }
        }
        else
        {
            // 添加重力效果
            velocity.y = 0;
        }
    }
}