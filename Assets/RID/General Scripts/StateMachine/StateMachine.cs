using UnityEngine;

public interface IState<T>
{
    string Name { get; }
    void Enter(T owner);
    void Tick(T owner);
    void Exit(T owner);
}

public class StateMachine : MonoBehaviour
{
    private object owner;
    private object currentState;

    public string CurrentStateName
    {
        get
        {
            if (currentState is IState<Stickman> s1) return s1.Name;
            // add other types here if needed
            return "None";
        }
    }

    public void Init<T>(T ownerRef) where T : class
    {
        owner = ownerRef;
    }

    public void SetState<T>(IState<T> newState) where T : class
    {
        var typedOwner = owner as T;
        if (typedOwner == null) return;

        // Exit old state if compatible
        if (currentState is IState<T> oldState)
        {
            oldState.Exit(typedOwner);
        }

        // Set new state
        currentState = newState;
        newState.Enter(typedOwner);
    }

    private void Update()
    {
        if (owner == null || currentState == null) return;
        
        if (currentState is IState<Stickman> stickmanState && owner is Stickman stickmanOwner)
        {
            stickmanState.Tick(stickmanOwner);
        }
    }
}