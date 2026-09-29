using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class FacilityButton : MonoBehaviour, IInteractable
{
    public Action OnButtonPressed;
    
    [SerializeField] private Material greenColor;
    [SerializeField] private Material redColor;
    [SerializeField] private Material grayColor;
    
    [SerializeField] private float animationDuration = 0.25f;

    [Tooltip("클릭음을 낼 AudioSource (clip에 클릭음 지정). 비어 있으면 같은 오브젝트에서 찾는다")]
    [SerializeField] private AudioSource clickSource;

    private Animator _animator;
    private MeshRenderer mesh;
    
    private bool _interactable = true;
    
    private ButtonStatus _status;
    private IInteractable _interactableImplementation;

    public ButtonStatus Status => _status;
    
    public void Initialize()
    {
        _animator = GetComponent<Animator>();
        var clickable = transform.Find("Clickable");
        mesh = clickable.GetComponent<MeshRenderer>();
        if (clickSource == null) clickSource = GetComponent<AudioSource>();

        _status = ButtonStatus.Deactivate;
    }
    

    public void Interact()
    {
        if (_interactable)
        {
            _status = _status.Next();
            StartCoroutine(WaitForAnimation());
            _animator.SetTrigger("ButtonPressed");
            PlayClickSound();
            OnButtonPressed?.Invoke();
        }

    }

    // 연속 클릭 시 앞 소리가 끊기지 않도록 PlayOneShot으로 겹쳐 재생한다
    private void PlayClickSound()
    {
        if (clickSource != null && clickSource.clip != null)
            clickSource.PlayOneShot(clickSource.clip);
    }

    private void ChangeStatus()
    {
        
        ChangeColor();
    }

    private void ChangeColor()
    {
        switch (_status)
        {
            case ButtonStatus.Deactivate:
                mesh.material = grayColor;
                break;
            case ButtonStatus.Red:
                mesh.material = redColor;
                break;
            case ButtonStatus.Green:
                mesh.material = greenColor;
                break;
        }
    }

    public void Clear()
    {
        _status = ButtonStatus.Deactivate;
        ChangeColor();
    }

    IEnumerator WaitForAnimation()
    {
        _interactable = false;
        yield return new WaitForSeconds(animationDuration);
        ChangeColor();
        _interactable = true;
        
    }
}