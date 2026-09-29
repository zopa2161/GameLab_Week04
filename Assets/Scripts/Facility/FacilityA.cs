using UnityEngine;

public class FacilityA : Facility
{
    // 목표가 우연히 현재 버튼 상태와 같을 때 다시 뽑는 최대 횟수 (확률상 거의 일어나지 않는다)
    private const int MaxGoalRetries = 10;

    [SerializeField]
    private FacilityButton[] facilityButtons;
    [SerializeField]
    private ButtonGuideManager buttonGuideManager;

    private ButtonStatus[] goalButtonStatuses;

    public override void Initialize()
    {
        foreach (FacilityButton facilityButton in facilityButtons)
        {
            facilityButton.Initialize();
            facilityButton.OnButtonPressed += OnButtonClicked;
        }

        goalButtonStatuses = new ButtonStatus[facilityButtons.Length];
        for (int i = 0; i < facilityButtons.Length; i++)
        {
            goalButtonStatuses[i] = ButtonStatus.Deactivate;

        }

        buttonGuideManager.SetGuide(goalButtonStatuses);
        isFault = false;
    }

    protected override void GenerateGoal()
    {
        int tries = 0;
        do
        {
            for (int i = 0; i < goalButtonStatuses.Length; i++)
            {
                goalButtonStatuses[i] = (ButtonStatus)Random.Range(0, 3);
            }
        } while (IsGoalReached() && ++tries < MaxGoalRetries);

        buttonGuideManager.SetGuide(goalButtonStatuses);
    }

    protected override bool IsGoalReached()
    {
        for (int i = 0; i < facilityButtons.Length; i++)
        {
            if (facilityButtons[i].Status != goalButtonStatuses[i])
            {
                return false;
            }
        }

        return true;
    }

    // 버튼 전부 회색, 목표 · 가이드도 전부 회색
    protected override void ResetDevices()
    {
        foreach (FacilityButton facilityButton in facilityButtons)
        {
            facilityButton.Clear();
        }

        for (int i = 0; i < goalButtonStatuses.Length; i++)
        {
            goalButtonStatuses[i] = ButtonStatus.Deactivate;
        }
        buttonGuideManager.SetGuide(goalButtonStatuses);
    }

    private void OnButtonClicked()
    {
        OnDeviceChanged();
    }
}
