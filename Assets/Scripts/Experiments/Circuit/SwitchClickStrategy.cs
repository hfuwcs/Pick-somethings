using System.Diagnostics;
using UnityEngine;

public class SwitchClickStrategy : IHoldStrategy
{
    private Switch targetSwitch;

    public SwitchClickStrategy(Switch target)
    {
        targetSwitch = target;
        targetSwitch.OnSwitchClicked += HandleSwitchClicked;
    }

    private void HandleSwitchClicked()
    {
        
    }
    public void Hold(Rigidbody heldBody, Transform grabberTransform, Transform centerOfMassTransform)
    {
        //
    }
}