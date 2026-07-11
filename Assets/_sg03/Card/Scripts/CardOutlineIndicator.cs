using UnityEngine;
using SG03;
using SaiGame.Services;

[RequireComponent(typeof(Renderer))]
public class CardOutlineIndicator : SaiBehaviour
{
    public Card3DCtrl _cardCtrl;
    [SerializeField] private Renderer _renderer;
    
    [SerializeField] private bool _isHover = false;

    protected override void LoadComponents()
    {
        base.LoadComponents();
        if (this._cardCtrl == null) this._cardCtrl = this.GetComponentInParent<Card3DCtrl>();
        if (this._renderer == null) this._renderer = this.GetComponent<Renderer>();
    }

    private void Awake()
    {
        // Disable renderer by default, keeping the material in the Inspector
        if (_renderer != null)
        {
            _renderer.enabled = false;
        }
    }

    private void Start()
    {
        // Update initial state if the card is already being hovered
        if (_cardCtrl != null && _cardCtrl.IsHover)
        {
            SetHover(true);
        }
    }

    private void OnEnable()
    {
        Card3DCtrl.HoverEntered += OnHoverEntered;
        Card3DCtrl.HoverExited += OnHoverExited;
    }

    private void OnDisable()
    {
        Card3DCtrl.HoverEntered -= OnHoverEntered;
        Card3DCtrl.HoverExited -= OnHoverExited;
    }

    private void OnHoverEntered(Card3DCtrl card)
    {
        if (card == _cardCtrl)
        {
            SetHover(true);
        }
    }

    private void OnHoverExited(Card3DCtrl card)
    {
        if (card == _cardCtrl)
        {
            SetHover(false);
        }
    }

    public void SetHover(bool isHover)
    {
        if (_isHover == isHover) return;
        _isHover = isHover;
        
        if (_renderer != null)
        {
            _renderer.enabled = _isHover;
        }
    }

    private void OnValidate()
    {
        // Allow toggling the _isHover checkbox in the inspector during Play Mode to test the outline
        if (Application.isPlaying && _renderer != null)
        {
            _renderer.enabled = _isHover;
        }
    }
}
