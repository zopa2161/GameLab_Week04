using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class FacilityManager : MonoBehaviour
{

    public Action<bool> OnFacilityStatusChanged;

    
    [SerializeField] private Facility[] _facilities;
    

    private FaultScheduler _faultScheduler;
    
    
    public FaultScheduler FaultScheduler => _faultScheduler;
    
    public Facility[] Facilities => _facilities;
    
    public int FaultCount
    {
        get
        {
            var count = 0;
            foreach(var f in _facilities)
            {
                if(f.IsFault() ) count++;
            }
            return count;
        }
    }
    
    public void Initialize(GameManager gameManager)
    {
        
        _faultScheduler = GetComponent<FaultScheduler>();

        foreach (var f in _facilities)
        {
            f.Initialize();
            f.OnFacilityInteracted += OnFacilityInteracted;
        }
        
        
        _faultScheduler.OnFaultMade += OnFaultMade;

    }
    

    // 설비가 수리를 완료했을 때만 온다 (isFault = false). 장치 초기화는 설비가 스스로 한다
    private void OnFacilityInteracted(bool isFault, int facilityID)
    {
        OnFacilityStatusChanged?.Invoke(!isFault);
    }
    
    private void OnFaultMade(int count)
    {
        //임시 코드
        var rand = Random.Range(0, _facilities.Length);
        _facilities[rand].MakeFault();
        
        OnFacilityStatusChanged?.Invoke(false);
    }
    
}
