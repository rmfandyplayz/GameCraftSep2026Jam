using UnityEngine;
using UnityEngine.SceneManagement;

public class Burger : AntLargeCarriableObject
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected override void DepositToNest(AntNest nest)
    {
        SceneManager.LoadScene("Win");
    }
    
}
