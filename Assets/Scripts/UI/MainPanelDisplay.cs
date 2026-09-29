using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 관제 모니터(WorldCanvas) UI. WorldCanvas 루트에 붙인다.
/// 설비 상태 / 진행도 / 남은 시간 / 내구도 표시와 강조색(초록) 일괄 변경을 담당한다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
[AddComponentMenu("UI/Main Panel View")]
public class MainPanelDisplay : MonoBehaviour
{
    [Serializable]
    public class FacilitySlotUI
    {
        [Tooltip("설비 이름")]
        public string name;
        
        [Tooltip("FacilitySlot (테두리 Image)")]
        public Image frame;

        [Tooltip("FacilitySlot/Panel/Text (TMP)")]
        public TMP_Text statusText;
    }

    [Header("Facility")]
    [Tooltip("FacilityPanel 아래 설비 슬롯들")]
    [SerializeField] private FacilitySlotUI[] facilitySlots;

    [Header("Progress")]
    [Tooltip("ProgressPanel (테두리 Image)")]
    [SerializeField] private Image progressFrame;

    [Tooltip("ProgressPanel/Panel/Text (TMP)")]
    [SerializeField] private TMP_Text progressText;

    [Tooltip("ProgressPanel/Panel/ProgressBar")]
    [SerializeField] private Image progressBar;

    [Header("Timer")]
    [Tooltip("Timer (테두리 Image)")]
    [SerializeField] private Image timerFrame;

    [Tooltip("Timer/Panel (1)/Text (TMP)")]
    [SerializeField] private TMP_Text timerText;

    [Header("Durability")]
    [Tooltip("HPPanel (테두리 Image)")]
    [SerializeField] private Image durabilityFrame;

    [Tooltip("DurabilityPanel/Panel (1)/Text (TMP)")]
    [SerializeField] private TMP_Text durabilityText;

    [Tooltip("DurabilityPanel/Panel (1)/ProgressBar")]
    [SerializeField] private Image durabilityBar;

    [Header("Color")]
    [Tooltip("안전 강조색")]
    [SerializeField] private Color safeColor = new Color(0.3f, 1f, 0f, 1f);

    [Tooltip("불안정 강조색")] 
    [SerializeField] 
    private Color unsafeColor;
    
    [Tooltip("인스펙터에서 강조색을 바꾸면 에디터에서 바로 반영")]
    [SerializeField] private bool previewInEditor = true;

    private FacilityManager _facilityManager;
    private GameManager _gameManager;

    private void Awake()
    {
        SetupFillBar(progressBar);
        SetupFillBar(durabilityBar);
        ApplysafeColor();
    }

    public void Initialize(GameManager gameManager)
    {
        _gameManager = gameManager;
        _facilityManager = gameManager.FacilityManager;

        _facilityManager.OnFacilityStatusChanged += OnFacilityStatusChanged;
        _facilityManager.OnFacilityStatusChanged.Invoke(false);

    }

    private void OnValidate()
    {
        if (previewInEditor)
            ApplysafeColor();
    }

    // ---------- Color ----------

    public void SetsafeColor(Color color)
    {
        safeColor = color;
        ApplysafeColor();
    }

    public void ApplysafeColor()
    {
        if (facilitySlots != null)
        {
            foreach (FacilitySlotUI slot in facilitySlots)
            {
                if (slot == null) continue;
                SetColor(slot.frame, safeColor);
                SetColor(slot.statusText, safeColor);
            }
        }

        SetColor(progressFrame, safeColor);
        SetColor(progressText, safeColor);
        SetColor(progressBar, safeColor);

        SetColor(timerFrame, safeColor);
        SetColor(timerText, safeColor);

        SetColor(durabilityFrame, safeColor);
        SetColor(durabilityText, safeColor);
        SetColor(durabilityBar, safeColor);
    }

    private static void SetColor(Graphic graphic, Color color)
    {
        if (graphic != null)
            graphic.color = color;
    }

    // ---------- Bar ----------

    // fillAmount는 Filled 타입에서만 동작하므로 가로(왼쪽→오른쪽) 채우기로 맞춘다.
    private static void SetupFillBar(Image bar)
    {
        if (bar == null) return;
        bar.type = Image.Type.Filled;
        bar.fillMethod = Image.FillMethod.Horizontal;
        bar.fillOrigin = (int)Image.OriginHorizontal.Left;
    }

    private static void SetFill(Image bar, float normalized)
    {
        if (bar != null)
            bar.fillAmount = Mathf.Clamp01(normalized);
    }
    

    // ---------- Progress ----------

    /// <param name="normalized">0 ~ 1</param>
    public void SetProgress(float normalized)
    {
        SetFill(progressBar, normalized);
    }

    // ---------- Timer ----------

    public void SetRemainingTime()
    {
        if (_gameManager.SystemTimer.IsActive)
        {
            timerText.text = $"남은 시간\n{_gameManager.SystemTimer.CurrentTime:F0}s";
        }
        else
        {
            timerText.text = $"--";    
        }
        
    }

    // ---------- Durability ----------

    /// <param name="normalized">0 ~ 1</param>
    public void SetDurability(float normalized)
    {
        SetFill(durabilityBar, normalized);
    }

    public void OnFacilityTrouble(int index)
    {
        facilitySlots[index].statusText.text = $"{facilitySlots[index].name}\n기능 고장";
        facilitySlots[index].statusText.color = unsafeColor;
        facilitySlots[index].frame.color = unsafeColor;
    }

    public void OnFacilityNormalize(int index)
    {
        facilitySlots[index].statusText.text = $"{facilitySlots[index].name}\n정상 작동";
        facilitySlots[index].statusText.color = safeColor;
        facilitySlots[index].frame.color = safeColor;
    }

    private void SetFacilitySlot(int index , bool isFault)
    {
        if (isFault)
        {
            OnFacilityTrouble(index);
        }
        else
        {
            OnFacilityNormalize(index);
        }
    }
    

    private void OnFacilityStatusChanged(bool isCompleted)
    {
        //인덱스 기반으로 상태 슬롯 갱신하기.
        for (int i = 0; i < _facilityManager.Facilities.Length; i++)
        {
            SetFacilitySlot(i,_facilityManager.Facilities[i].IsFault());
        }
    }
}
