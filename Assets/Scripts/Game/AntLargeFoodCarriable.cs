
using UnityEngine;

public class AntLargeFoodCarriable : AntLargeCarriableObject
{
    [SerializeField] private int FoodGiven;
    
    protected override void DepositToNest(AntNest nest)
    {
        nest.foodCount += FoodGiven;
    }
}
