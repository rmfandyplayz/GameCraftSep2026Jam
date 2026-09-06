using UnityEngine;
using UnityEngine.InputSystem.HID;
using UnityEngine.UI;

public class ReadPaper : AntInteractable
{
    [SerializeField] private Canvas lorePiece;
    private bool paperVisible = true;

    public override void AntBeginInteract(Ant ant)
    {
        lorePiece.gameObject.SetActive(true);
    }

    public override bool CanAntInteract(Ant ant)
    {
        return true;
    }

    public override Vector3 GetAntInteractPos(Ant ant)
    {
        return transform.position;
    }

    public override void CancelAntInteract(Ant ant)
    {
        
    }

    public override void AntEndInteract(Ant ant)
    {
        
    }

    public void CloseImage()
    {
        Debug.Log("clicked");
        lorePiece.gameObject.SetActive(false);
        Destroy((this.gameObject));
    }
    
}
