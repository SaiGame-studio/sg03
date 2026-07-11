using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

[RequireComponent(typeof(Renderer))]
public class CardOutlineIndicator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Renderer _renderer;
    private Material[] _originalMaterials;
    private Material[] _outlinedMaterials;
    
    private bool _hasOutlineMaterial = false;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        
        // Lấy tất cả material hiện có trên Renderer
        Material[] currentMaterials = _renderer.materials;
        List<Material> baseMaterials = new List<Material>();
        Material outlineMat = null;

        // Tìm material sử dụng shader CardURPOutline
        for (int i = 0; i < currentMaterials.Length; i++)
        {
            if (currentMaterials[i].shader.name == "Custom/CardURPOutline")
            {
                outlineMat = currentMaterials[i];
            }
            else
            {
                baseMaterials.Add(currentMaterials[i]);
            }
        }

        if (outlineMat != null)
        {
            _hasOutlineMaterial = true;
            _originalMaterials = baseMaterials.ToArray();
            
            _outlinedMaterials = new Material[_originalMaterials.Length + 1];
            for (int i = 0; i < _originalMaterials.Length; i++)
            {
                _outlinedMaterials[i] = _originalMaterials[i];
            }
            _outlinedMaterials[_outlinedMaterials.Length - 1] = outlineMat;

            // Mặc định tắt outline khi mới chạy
            _renderer.materials = _originalMaterials;
        }
        else
        {
            Debug.LogWarning("CardOutlineIndicator: Không tìm thấy Material nào sử dụng Shader 'Custom/CardURPOutline' trên Cube. Hãy chắc chắn bạn đã kéo Material đó vào Renderer.");
            _originalMaterials = currentMaterials;
            _outlinedMaterials = currentMaterials;
        }
    }

    // Handles UI/EventSystem pointer enter (Requires PhysicsRaycaster on Camera)
    public void OnPointerEnter(PointerEventData eventData)
    {
        SetOutlineEnabled(true);
    }

    // Handles UI/EventSystem pointer exit
    public void OnPointerExit(PointerEventData eventData)
    {
        SetOutlineEnabled(false);
    }

    // Handles physics raycast mouse enter (Requires Collider)
    private void OnMouseEnter()
    {
        SetOutlineEnabled(true);
    }

    // Handles physics raycast mouse exit (Requires Collider)
    private void OnMouseExit()
    {
        SetOutlineEnabled(false);
    }

    public void SetOutlineEnabled(bool isEnabled)
    {
        if (!_hasOutlineMaterial) return;

        if (isEnabled)
        {
            _renderer.materials = _outlinedMaterials;
        }
        else
        {
            _renderer.materials = _originalMaterials;
        }
    }
}
