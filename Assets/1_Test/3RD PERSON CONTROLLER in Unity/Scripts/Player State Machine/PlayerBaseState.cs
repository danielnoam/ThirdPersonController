
public abstract class PlayerBaseState
{
    protected PlayerStateMachine stateMachine;

    public PlayerBaseState(PlayerStateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
    }

    public abstract void EnterState();
    public abstract void ExitState();
    public abstract void UpdateState();
    public abstract void FixedUpdateState();
    public virtual void OnAnimatorMoveState() { }
}