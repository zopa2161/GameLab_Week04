using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 1인칭 플레이어. Player 루트에 붙인다.
/// WASD 이동(중력 포함, 점프 없음)과 좌클릭 상호작용을 담당한다.
/// 좌클릭하면 조준선(카메라 정면)으로 Raycast해서 맞은 IInteractable을 바로 조작한다.
/// 맞은 대상이 IDraggable이면 누르고 있는 동안 잡아서 마우스 이동량을 넘기고, 그동안 이동을 멈추고 시점 감도를 낮춘다.
/// 회전과 커서는 FirstPersonCamera가 한다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    // 접지 중에도 살짝 아래로 눌러야 CharacterController.isGrounded가 안정적으로 유지된다
    private const float GroundedVerticalVelocity = -2f;

    [Header("References")]
    [Tooltip("비어 있으면 같은 오브젝트에서 찾는다")]
    [SerializeField] private CharacterController characterController;

    [Tooltip("비어 있으면 자식에서 찾는다")]
    [SerializeField] private FirstPersonCamera firstPersonCamera;

    [Header("Movement")]
    [Tooltip("m/초")]
    [SerializeField] private float moveSpeed = 3f;

    [SerializeField] private float gravity = -9.81f;

    [Header("Interaction")]
    [Tooltip("조작 가능 거리 (m)")]
    [SerializeField] private float interactDistance = 2f;

    [Tooltip("상호작용 Raycast 대상 레이어 (Button, Slider)")]
    [SerializeField] private LayerMask interactLayerMask;

    [Header("Drag")]
    [Tooltip("드래그 중 시점 감도 배율. 0이면 시점이 완전히 멈춘다. 가장 무거운 슬라이더(weight 0.3)를 끌 때 손잡이가 멈춰 보이지 않도록 낮게 둔다")]
    [SerializeField, Range(0f, 1f)] private float dragLookScale = 0.1f;

    private Vector2 _moveInput;
    private float _verticalVelocity;
    private int _lockCount;

    private IDraggable _dragTarget;
    private Vector2 _dragDelta;

    public bool IsInputLocked => _lockCount > 0;
    public bool IsDragging => _dragTarget != null;

    // 컴포넌트를 처음 붙일 때 기본 레이어 마스크를 Button, Slider로 채운다
    private void Reset()
    {
        interactLayerMask = LayerMask.GetMask("Button", "Slider");
    }

    private void Awake()
    {
        if (characterController == null) characterController = GetComponent<CharacterController>();
        if (firstPersonCamera == null) firstPersonCamera = GetComponentInChildren<FirstPersonCamera>();
    }

    // 잠금 카운트가 남지 않도록 비활성화될 때 잡고 있던 것을 놓는다
    private void OnDisable()
    {
        StopDrag();
    }

    // 창 밖에서 버튼을 떼면 canceled가 오지 않을 수 있으므로 포커스를 잃으면 놓는다
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) StopDrag();
    }

    // ---------- Input (PlayerInput Unity Events) ----------

    public void OnMove(InputAction.CallbackContext context)
    {
        // canceled 때는 zero가 들어와 멈춘다
        _moveInput = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        // 시점 회전은 FirstPersonCamera가 받는다. 여기서는 드래그 중인 이동량만 모은다.
        // 한 프레임에 여러 번 올 수 있어 대입하지 않고 누적한다.
        if (!context.performed || !IsDragging) return;
        _dragDelta += context.ReadValue<Vector2>();
    }

    public void OnClick(InputAction.CallbackContext context)
    {
        // 뗌 : 드래그 중에는 스스로 잠금을 걸어 두므로 잠금 검사보다 먼저 처리한다
        if (context.canceled)
        {
            StopDrag();
            return;
        }

        if (!context.performed) return;

        // 커서가 풀린 상태의 클릭은 커서 재잠금만 하고 상호작용하지 않는다
        if (!firstPersonCamera.IsCursorLocked)
        {
            firstPersonCamera.SetCursorLocked(true);
            return;
        }

        if (IsInputLocked || IsDragging) return;

        TryInteract();
    }

    // ---------- Movement ----------

    private void Update()
    {
        UpdateDrag();
        Move();
    }

    private void Move()
    {
        // WASD 컴포지트는 이미 정규화되어 있다. 게임패드 스틱 대비로 길이만 1로 제한한다
        // 드래그 중에는 손잡이와의 거리·각도가 바뀌지 않도록 걷지 않는다 (시점은 낮은 감도로 돈다)
        Vector2 input = IsInputLocked || IsDragging ? Vector2.zero : Vector2.ClampMagnitude(_moveInput, 1f);
        Vector3 direction = transform.forward * input.y + transform.right * input.x;

        if (characterController.isGrounded && _verticalVelocity < 0f)
            _verticalVelocity = GroundedVerticalVelocity;
        else
            _verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = direction * moveSpeed + Vector3.up * _verticalVelocity;
        characterController.Move(velocity * Time.deltaTime);
    }

    // ---------- Interaction ----------

    private void TryInteract()
    {
        Ray ray = firstPersonCamera.GetAimRay();

        if (!Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactLayerMask, QueryTriggerInteraction.Ignore))
            return;

        // 버튼은 루트와 자식(Clickable, FoucsIndicator), 슬라이더는 자식(Handle)에 콜라이더가 있으므로 부모 방향으로 찾는다
        IDraggable draggable = hit.collider.GetComponentInParent<IDraggable>();
        if (draggable != null)
        {
            StartDrag(draggable);
            return;
        }

        IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
        if (interactable != null) interactable.Interact();
    }

    // ---------- Drag ----------

    private void StartDrag(IDraggable target)
    {
        _dragTarget = target;
        _dragDelta = Vector2.zero;
        firstPersonCamera.SetLookScale(dragLookScale);
        target.BeginDrag(firstPersonCamera.Camera);
    }

    /// <summary>
    /// 잡고 있던 것을 놓는다. 뗌, Esc, 포커스 상실, 비활성화 모두 여기로 모인다.
    /// 드래그 중이 아니면 아무것도 하지 않으므로 여러 번 불려도 시점 감도가 한 번만 복구된다.
    /// </summary>
    private void StopDrag()
    {
        if (_dragTarget == null) return;

        IDraggable target = _dragTarget;
        _dragTarget = null;
        _dragDelta = Vector2.zero;

        target.EndDrag();
        firstPersonCamera.SetLookScale(1f);
    }

    private void UpdateDrag()
    {
        if (!IsDragging)
        {
            _dragDelta = Vector2.zero;
            return;
        }

        // 드래그 중 Esc로 커서가 풀렸으면 놓는다 (Escape 입력은 FirstPersonCamera가 받는다)
        // 장치가 스스로 드래그를 끝냈으면(수리 후 초기화 등) 이쪽도 정리한다
        if (!firstPersonCamera.IsCursorLocked || !_dragTarget.IsDragging)
        {
            StopDrag();
            return;
        }

        // 마우스 delta는 이미 프레임 이동량이므로 deltaTime을 곱하지 않는다
        _dragTarget.Drag(_dragDelta);
        _dragDelta = Vector2.zero;
    }

    // ---------- Lock ----------

    /// <summary>
    /// 이동·시점·상호작용 잠금. 요청 수를 세므로 잠근 쪽이 각자 한 번씩 풀어야 한다.
    /// (매뉴얼 확대 보기용. 슬라이더 드래그는 잠그지 않고 시점 감도만 낮춘다)
    /// </summary>
    public void SetInputLocked(bool locked)
    {
        if (locked) _lockCount++;
        else if (_lockCount > 0) _lockCount--;

        firstPersonCamera.SetLookLocked(IsInputLocked);
    }
}
