
using UnityEngine;

public class AntFoodCrumb : AntCarriableObject
{
    [SerializeField] private float FoodAmount = 1;
    [SerializeField] private int AntAmount = 1;
    
    public override void OnPickup()
    {
        
    }

    public override void OnDeposit(AntNest nest)
    {
        nest.foodCount += FoodAmount;
        nest.SpawnAnts(AntAmount);
        Debug.Log(nest.foodCount);
        Destroy(gameObject);
    }
}
