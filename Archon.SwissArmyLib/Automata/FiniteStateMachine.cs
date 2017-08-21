﻿using System;
using System.Collections.Generic;

namespace Archon.SwissArmyLib.Automata
{
    public class FiniteStateMachine<T>
    {
        /// <summary>
        /// A shared context which all states have access to.
        /// </summary>
        public T Context { get; private set; }

        public FsmState<T> CurrentState { get; private set; }
        public FsmState<T> PreviousState { get; private set; }

        private readonly Dictionary<Type, FsmState<T>> _states = new Dictionary<Type, FsmState<T>>();

        /// <summary>
        /// Creates a new Finite State Machine.
        /// 
        /// If you need control over how the states are created, you can register them manually using <see cref="RegisterState{TState}"/>.
        /// If not, then you can freely use <see cref="ChangeStateAuto{TState}"/> which will create the states using their default constructor.
        /// </summary>
        /// <param name="context"></param>
        public FiniteStateMachine(T context)
        {
            Context = context;
        }

        /// <summary>
        /// Call this every time the machine should update. Eg. every frame.
        /// </summary>
        public void Update(float deltaTime)
        {
            var currentState = CurrentState;

            if (currentState!= null)
            {
                currentState.Reason();

                // we only want to update the state if it's still the current one
                if (currentState == CurrentState)
                    CurrentState.Update(deltaTime);
            }
        }

        /// <summary>
        /// Preemptively add a state instance.
        /// Useful if the state doesn't have an empty constructor and therefore cannot be used with ChangeStateAuto.
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        /// <param name="state"></param>
        public void RegisterState<TState>(TState state) where TState : FsmState<T>
        {
            _states[typeof(TState)] = state;
        }

        /// <summary>
        /// Changes the active state to the given state type.
        /// If a state of that type isn't already registered, it will automatically create a new instance using the empty constructor.
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        /// <returns></returns>
        public TState ChangeStateAuto<TState>() where TState : FsmState<T>, new()
        {
            var type = typeof(TState);
            FsmState<T> state;

            if (!_states.TryGetValue(type, out state))
                _states[type] = state = new TState();

            return ChangeState((TState) state);
        }

        /// <summary>
        /// Changes the active state to the given state type. 
        /// An instance of that type should already had been registered to use this method.
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        /// <returns></returns>
        public TState ChangeState<TState>() where TState : FsmState<T>
        {
            var type = typeof(TState);
            FsmState<T> state;

            if (!_states.TryGetValue(type, out state))
                throw new InvalidOperationException(string.Format("A state of type '{0}' is not registered, did you mean to use ChangeStateAuto?", type));

            return ChangeState((TState)state);
        }

        /// <summary>
        /// Changes the active state to a specific state instance.
        /// This will (if not null) also register the state.
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        /// <param name="state"></param>
        /// <returns></returns>
        public TState ChangeState<TState>(TState state) where TState : FsmState<T>
        {
            if (CurrentState != null)
                CurrentState.End();

            PreviousState = CurrentState;
            CurrentState = state;

            if (CurrentState != null)
            {
                RegisterState(state);
                CurrentState.Initialize(this, Context);
                CurrentState.Begin();
            }

            return state;
        }
    }

    public class FsmState<T> : State<FiniteStateMachine<T>, T> { }
}