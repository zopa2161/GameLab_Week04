using UnityEngine;

/// <summary>
/// 레버 설비. 고장 나면 레버를 당길 때마다 repairChance 확률로 회복된다.
/// 맞출 목표 상태가 없으므로 "목표 달성"은 당길 때마다 하는 확률 판정이다.
/// </summary>
public class FacilityC : Facility
{
    [SerializeField] private Lever lever;

    [Header("레버 세팅")]
    [Tooltip("한 번 당길 때 회복 확률 (0 ~ 1)")]
    [SerializeField, Range(0f, 1f)] private float repairChance = 0.25f;

    public override void Initialize()
    {
        isFault = false;
        lever.OnPulled += OnLeverPulled;
    }

    // 목표 상태가 없다
    protected override void GenerateGoal()
    {
    }

    // 고장 중 당길 때만 불리므로, 부를 때마다 한 번 판정한다.
    // Random.value는 1.0도 나올 수 있으므로 확률 1은 따로 처리해 항상 성공하게 한다
    protected override bool IsGoalReached()
    {
        return repairChance >= 1f || Random.value < repairChance;
    }

    // 초기화할 장치 상태가 없다. 레버는 스프링이라 스스로 올라오고,
    // 강제로 되돌리면 내려가 있던 손잡이가 순간이동한다
    protected override void ResetDevices()
    {
    }

    private void OnLeverPulled()
    {
        OnDeviceChanged();
    }
}
