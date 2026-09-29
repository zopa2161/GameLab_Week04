using System;
using UnityEngine;

/// <summary>
/// 설비 기반 클래스. 고장 여부는 isFault 플래그로만 판단한다.
/// 하위 클래스는 장치가 바뀔 때마다 OnDeviceChanged()를 부르고, 목표 생성 · 달성 검사 · 장치 초기화만 채운다.
/// - 정상일 때 장치 조작 : 장치는 반응하지만 설비에는 영향이 없다
/// - 고장 중 목표 달성 : 정상으로 돌아가고, 장치를 초기화한 뒤, 수리 알림을 한 번 보낸다
/// </summary>
public abstract class Facility  : MonoBehaviour
{
    /// <summary>수리 완료 때만 (isFault = false, facilityID)로 온다.</summary>
    public Action<bool, int> OnFacilityInteracted;

    [SerializeField]
    protected int facilityID;

    [Header("Observation")]
    [SerializeField]
    protected bool isFault;

    public int FacilityID => facilityID;

    public bool IsFault() => isFault;

    public abstract void Initialize();

    /// <summary>고장 발생. 새 목표를 만들고 고장 상태가 된다. 장치는 현재 상태를 유지한다. 이미 고장이면 무시.</summary>
    public void MakeFault()
    {
        if (isFault) return;
        GenerateGoal();
        isFault = true;
    }

    /// <summary>강제로 정상으로 돌리고 장치를 초기화한다. 재시작용.</summary>
    public void Clear()
    {
        isFault = false;
        ResetDevices();
    }

    /// <summary>장치가 바뀔 때마다 하위 클래스가 부른다.</summary>
    protected void OnDeviceChanged()
    {
        // 정상 : 장치는 이미 반응했다. 설비에는 영향 없음
        if (!isFault) return;

        // 고장 중, 아직 목표 미달성 : 알림 없음
        if (!IsGoalReached()) return;

        // 수리 완료 : 알림을 받는 쪽이 정리된 상태를 보도록 초기화를 먼저 한다
        isFault = false;
        ResetDevices();
        OnFacilityInteracted?.Invoke(false, facilityID);
    }

    /// <summary>새 목표를 만든다. 현재 장치 상태와 겹치지 않게 한다.</summary>
    protected abstract void GenerateGoal();

    /// <summary>현재 장치 상태가 목표를 달성했는지.</summary>
    protected abstract bool IsGoalReached();

    /// <summary>장치를 초기 상태로 되돌리고 목표를 해제한다.</summary>
    protected abstract void ResetDevices();
}
