
using UnityEngine;

public class AntFoodCrumb : AntCarriableObject
{
    [SerializeField] private int FoodAmount = 1;
    
    public override void OnPickup()
    {
        
    }

    public override void OnDeposit(AntNest nest)
    {
        nest.foodCount += FoodAmount;
        Destroy(gameObject);
    }
}
