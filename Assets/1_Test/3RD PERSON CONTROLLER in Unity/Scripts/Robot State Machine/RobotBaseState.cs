public abstract class RobotBaseState
{
    protected RobotStateMachine StateMachine;

    public RobotBaseState(RobotStateMachine stateMachine)
    {
        this.StateMachine = stateMachine;
    }

    public abstract void EnterState();
    public abstract void ExitState();
    public abstract void UpdateState();
    public abstract void FixedUpdateState();
}