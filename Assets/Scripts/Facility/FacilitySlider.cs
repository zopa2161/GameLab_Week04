using System;
using UnityEngine;

/// <summary>
/// 설비 B의 슬라이더 장치. Slider 프리팹 루트에 붙인다.
/// 손잡이(Slider 레이어 콜라이더)를 잡고 드래그하면 레일(RailStart → RailEnd)을 따라 0 ~ 1 값으로 움직인다.
/// 마우스 이동량을 화면에 보이는 레일 방향으로 투영하므로, weight 1이면 손잡이가 레일 기준으로 마우스를 1:1로 따라온다.
/// 드래그 중에도 시점이 낮은 감도로 돌기 때문에 화면상 레일 축은 매번 다시 구한다.
/// 목표 범위에 들어가는 순간 '철컥' 소리만 내고, 시각 표시는 하지 않는다.
/// </summary>
[DisallowMultipleComponent]
public class FacilitySlider : MonoBehaviour, IDraggable
{
    /// <summary>값이 바뀌면 소속 설비에 알린다.</summary>
    public event Action OnValueChanged;

    [Header("References")]
    [Tooltip("움직일 손잡이. Slider 레이어 콜라이더를 가진다")]
    [SerializeField] private Transform handle;

    [Tooltip("값 0 위치")]
    [SerializeField] private Transform railStart;

    [Tooltip("값 1 위치")]
    [SerializeField] private Transform railEnd;

    [Header("Drag")]
    [Tooltip("무게감. 드래그 양 대비 움직이는 비율 (무거울수록 작다). 1.0 / 0.7 / 0.5 / 0.3")]
    [SerializeField, Min(0f)] private float weight = 1f;

    [Tooltip("시작 값")]
    [SerializeField, Range(0f, 1f)] private float initialValue;

    [Tooltip("화면에 보이는 레일 길이의 최솟값 (픽셀). 레일을 거의 끝에서 바라볼 때 값이 튀지 않게 한다")]
    [SerializeField, Min(1f)] private float minScreenAxisLength = 50f;

    [Header("Sound")]
    [Tooltip("움직이는 동안 반복 재생하는 끄는 소리")]
    [SerializeField] private AudioSource dragLoopSource;

    [Tooltip("목표 범위에 들어가는 순간 재생하는 '철컥' 소리 (clip에 지정)")]
    [SerializeField] private AudioSource snapSource;

    [Tooltip("이 시간(초) 동안 값이 변하지 않으면 끄는 소리를 멈춘다")]
    [SerializeField, Min(0f)] private float dragSoundHoldTime = 0.1f;

    private float _value;
    private Vector2 _screenAxis;
    private Camera _dragCamera;     // 드래그 중에만 값이 있다

    private float _target;
    private float _tolerance;
    private bool _hasTarget;
    private bool _isInRange;

    private float _lastMoveTime;

    public float Value => _value;

    /// <summary>설비가 목표를 지정해 둔 상태 (설비 고장 중).</summary>
    public bool HasTarget => _hasTarget;

    /// <summary>목표가 있고, 현재 값이 목표 ± 허용 범위/2 안에 있다.</summary>
    public bool IsInRange => _hasTarget && Mathf.Abs(_value - _target) <= _tolerance * 0.5f;

    private bool IsConfigured => handle != null && railStart != null && railEnd != null;

    private void Awake()
    {
        if (!IsConfigured)
            Debug.LogWarning($"[FacilitySlider] {name}의 Handle / RailStart / RailEnd 참조가 비어 있습니다.", this);

        if (dragLoopSource != null) dragLoopSource.loop = true;

        // 설비에 등록되기 전(단독 테스트)에도 손잡이가 시작 값 위치에 오도록 여기서 한 번 맞춘다
        ResetState();
    }

    private void Update()
    {
        if (dragLoopSource != null && dragLoopSource.isPlaying && Time.time - _lastMoveTime > dragSoundHoldTime)
            dragLoopSource.Stop();
    }

    // ---------- Facility API ----------

    /// <summary>시작 상태로 되돌린다. 소속 설비가 부른다.</summary>
    public void Initialize()
    {
        ResetState();
    }

    /// <summary>목표 위치와 허용 범위(전체 폭, 0.1 = ±5%)를 지정한다.</summary>
    public void SetTarget(float target, float tolerance)
    {
        _target = Mathf.Clamp01(target);
        _tolerance = Mathf.Max(0f, tolerance);
        _hasTarget = true;

        // 지정 직후 이미 범위 안이어도 '철컥'이 나지 않도록 현재 상태로 맞춰 둔다
        _isInRange = IsInRange;
    }

    /// <summary>목표를 해제한다. 목표가 없으면 범위 진입 소리를 내지 않는다.</summary>
    public void ClearTarget()
    {
        _hasTarget = false;
        _isInRange = false;
    }

    // ---------- IDraggable ----------

    public void BeginDrag(Camera cam)
    {
        if (!IsConfigured || cam == null) return;

        _dragCamera = cam;

        // 레일 끝이 카메라 뒤에 있어 처음부터 축을 못 구하는 경우의 기본값
        _screenAxis = Vector2.up * minScreenAxisLength;
        RecalculateScreenAxis();
    }

    public void Drag(Vector2 mouseDelta)
    {
        if (_dragCamera == null) return;

        // 드래그 중에도 시점이 낮은 감도로 돌기 때문에, 화면에 보이는 레일 축을 매번 다시 구한다
        RecalculateScreenAxis();

        // 레일 방향 성분만 남긴다. 레일 전체 길이(픽셀)만큼 끌면 값이 1 변한다 (weight 1 기준)
        float deltaValue = Vector2.Dot(mouseDelta, _screenAxis) / _screenAxis.sqrMagnitude * weight;
        SetValue(_value + deltaValue);
    }

    public void EndDrag()
    {
        _dragCamera = null;
        if (dragLoopSource != null) dragLoopSource.Stop();
    }

    public bool IsDragging => _dragCamera != null;

    // ---------- Internal ----------

    public void ResetState()
    {
        _dragCamera = null;
        _value = initialValue;
        ClearTarget();
        UpdateHandlePosition();
        if (dragLoopSource != null) dragLoopSource.Stop();
    }

    // 값 0 → 1이 화면에서 차지하는 픽셀 벡터를 구한다
    private void RecalculateScreenAxis()
    {
        Vector3 start = _dragCamera.WorldToScreenPoint(railStart.position);
        Vector3 end = _dragCamera.WorldToScreenPoint(railEnd.position);

        // 레일 끝이 카메라 뒤로 가면 화면 좌표가 뒤집히므로 이전 축을 그대로 쓴다
        if (start.z <= 0f || end.z <= 0f) return;

        Vector2 axis = (Vector2)(end - start);
        float length = axis.magnitude;

        // 레일을 거의 끝에서 바라보면 축이 짧아져 값이 튀므로 방향만 쓰고 길이는 최솟값으로 늘린다
        if (length < minScreenAxisLength)
        {
            Vector2 direction = length > Mathf.Epsilon ? axis / length : Vector2.up;
            axis = direction * minScreenAxisLength;
        }

        _screenAxis = axis;
    }

    private void SetValue(float value)
    {
        value = Mathf.Clamp01(value);
        if (Mathf.Approximately(value, _value)) return;

        _value = value;
        UpdateHandlePosition();
        PlayDragSound();
        CheckRange();

        OnValueChanged?.Invoke();
    }

    private void UpdateHandlePosition()
    {
        if (!IsConfigured) return;
        handle.position = Vector3.Lerp(railStart.position, railEnd.position, _value);
    }

    private void PlayDragSound()
    {
        _lastMoveTime = Time.time;
        if (dragLoopSource != null && !dragLoopSource.isPlaying) dragLoopSource.Play();
    }

    // 범위 밖 → 안으로 바뀌는 순간에만 '철컥'. 안 → 밖은 소리 없음
    private void CheckRange()
    {
        bool inRange = IsInRange;
        if(inRange) Debug.Log("InRange");
        if (inRange && !_isInRange && snapSource != null && snapSource.clip != null)
            snapSource.PlayOneShot(snapSource.clip);

        _isInRange = inRange;
    }
}
