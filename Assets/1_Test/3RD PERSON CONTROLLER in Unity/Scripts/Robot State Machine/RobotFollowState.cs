using UnityEngine;

public class RobotFollowState : RobotBaseState
{


    public RobotFollowState(RobotStateMachine stateMachine) : base(stateMachine) { }
    
    
    
    
    public override void EnterState()
    {

    }
    
    public override void ExitState()
    {

    }

    public override void UpdateState()
    {
        

        CheckStateTransitions();
    }

    public override void FixedUpdateState()
    {

    }
    


    private void CheckStateTransitions()
    {

    }
}