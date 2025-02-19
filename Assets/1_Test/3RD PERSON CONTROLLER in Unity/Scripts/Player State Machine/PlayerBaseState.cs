
public abstract class PlayerBaseState
{
    protected PlayerStateMachine StateMachine;

    public PlayerBaseState(PlayerStateMachine stateMachine)
    {
        this.StateMachine = stateMachine;
    }

    public abstract void EnterState();
    public abstract void ExitState();
    public abstract void UpdateState();
    public abstract void FixedUpdateState();
}