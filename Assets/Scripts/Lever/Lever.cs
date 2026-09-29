using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 스프링 레버. Lever 프리팹 루트(Animator가 있는 곳)에 붙인다.
/// 클릭 한 번에 레버가 내려갔다가 스스로 올라온다. 끝까지 내려간 순간 OnPulled로 소속 설비에 알린다.
/// 움직이는 동안의 클릭은 무시한다.
/// </summary>
[DisallowMultipleComponent]
public class Lever : MonoBehaviour, IInteractable
{
    private static readonly int IsDownHash = Animator.StringToHash("IsDown");
    private static readonly int LeverUpStateHash = Animator.StringToHash("LeverUp");

    /// <summary>끝까지 내려간 순간 한 번. 소속 설비가 구독한다.</summary>
    public event Action OnPulled;

    [Header("Timing")]
    [Tooltip("내려가는 / 올라오는 시간 (초). Lever.controller의 전환 시간(0.25초)과 같게 둔다")]
    [SerializeField, Min(0f)] private float pullDuration = 0.25f;

    [Tooltip("아래에 머무는 시간 (초)")]
    [SerializeField, Min(0f)] private float holdDuration = 0.1f;

    [Header("Sound")]
    [Tooltip("당기는 소리 (clip에 지정). 비어 있으면 같은 오브젝트에서 찾는다")]
    [SerializeField] private AudioSource pullSource;

    private Animator _animator;
    private bool _isPulling;
    private Coroutine _pullRoutine;

    public bool IsPulling => _isPulling;

    // 설비 초기화 순서와 상관없도록 Start가 아니라 Awake에서 캐시한다
    private void Awake()
    {
        _animator = GetComponent<Animator>();
        if (pullSource == null) pullSource = GetComponent<AudioSource>();
    }

    // 컴포넌트만 꺼져도 코루틴은 계속 돌기 때문에 직접 멈춘다. 다시 켜졌을 때 영원히 잠기지 않게 한다
    private void OnDisable()
    {
        if (_pullRoutine != null) StopCoroutine(_pullRoutine);
        _pullRoutine = null;
        _isPulling = false;
    }

    public void Interact()
    {
        if (_isPulling) return;
        _pullRoutine = StartCoroutine(PullRoutine());
    }

    /// <summary>
    /// 당기던 것을 멈추고 올라간 상태로 즉시 되돌린다. 게임 재시작용.
    /// 수리(Facility.Clear) 때는 부르지 않는다. 내려가 있던 손잡이가 순간이동한다.
    /// </summary>
    public void ResetLever()
    {
        if (_pullRoutine != null) StopCoroutine(_pullRoutine);
        _pullRoutine = null;
        _isPulling = false;

        if (_animator == null) return;
        _animator.SetBool(IsDownHash, false);
        _animator.Play(LeverUpStateHash, 0, 0f);
    }

    private IEnumerator PullRoutine()
    {
        _isPulling = true;

        SetDown(true);
        PlayPullSound();
        yield return new WaitForSeconds(pullDuration);

        // 끝까지 내려간 순간 판정한다. 구독자가 없으면(단독 테스트) 애니메이션만 동작한다
        OnPulled?.Invoke();
        yield return new WaitForSeconds(holdDuration);

        SetDown(false);
        yield return new WaitForSeconds(pullDuration);

        _isPulling = false;
        _pullRoutine = null;
    }

    private void SetDown(bool isDown)
    {
        if (_animator != null) _animator.SetBool(IsDownHash, isDown);
    }

    private void PlayPullSound()
    {
        if (pullSource != null && pullSource.clip != null)
            pullSource.PlayOneShot(pullSource.clip);
    }
}
