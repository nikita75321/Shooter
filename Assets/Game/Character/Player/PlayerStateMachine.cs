// using System;
// using System.Collections.Generic;
// using UnityEngine;

// public class PlayerStateMachine
// {
//     public event Action<PlayerState> OnStateChanged;
    
//     public PlayerState CurrentState { get; private set; }
//     private Player _player;
//     private Dictionary<PlayerState, StateTransition> _transitions;
//     private Dictionary<PlayerState, StateActions> _stateActions;

//     public PlayerStateMachine(Player player)
//     {
//         _player = player ?? throw new ArgumentNullException(nameof(player));
//         CurrentState = PlayerState.Idle;
        
//         InitializeTransitions();
//         InitializeStateActions();
//     }

//     private void InitializeTransitions()
//     {
//         _transitions = new Dictionary<PlayerState, StateTransition>
//         {
//             { 
//                 PlayerState.Idle, 
//                 new StateTransition(
//                     condition: () => !_player.Controller.isMoving && !_player.Weapon.IsShooting,
//                     priority: 0
//                 )
//             },
//             { 
//                 PlayerState.Walking, 
//                 new StateTransition(
//                     condition: () => _player.Controller.isMoving && !_player.Weapon.IsShooting,
//                     priority: 1
//                 )
//             },
//             { 
//                 PlayerState.Shooting, 
//                 new StateTransition(
//                     () => _player.Weapon.IsShooting && 
//                     !_player.Weapon.IsReloading &&
//                     _player.CurrentState != PlayerState.PickingUp,
//                     priority: 2
//                 )
//             },
//             { 
//                 PlayerState.Reloading, 
//                 new StateTransition(
//                     condition: () => _player.Weapon.IsReloading,
//                     priority: 3
//                 )
//             },
//             { 
//                 PlayerState.PickingUp, 
//                 new StateTransition(
//                     condition: () => _player.IsPickingUp,
//                     priority: 4
//                 )
//             },
//             { 
//                 PlayerState.Dead, 
//                 new StateTransition(
//                     condition: () => _player.Health.IsDead,
//                     priority: 5
//                 )
//             }
//         };
//     }

//     private void InitializeStateActions()
//     {
//         _stateActions = new Dictionary<PlayerState, StateActions>
//         {
//             { 
//                 PlayerState.Idle, 
//                 new StateActions(
//                     enterAction: () => {
//                         _player.OnIdle?.Invoke();
//                         if (_player.debugStateTransitions) Debug.Log("Entered Idle state");
//                     }
//                 )
//             },
//             { 
//                 PlayerState.Walking, 
//                 new StateActions(
//                     enterAction: () => {
//                         _player.OnWalk?.Invoke();
//                         if (_player.debugStateTransitions) Debug.Log("Entered Walking state");
//                     }
//                 )
//             },
//             { 
//                 PlayerState.Shooting, 
//                 new StateActions(
//                     enterAction: () => {
//                         _player.OnShoot?.Invoke();
//                         if (_player.debugStateTransitions) Debug.Log("Entered Shooting state");
//                     }
//                 )
//             },
//             { 
//                 PlayerState.Reloading, 
//                 new StateActions(
//                     enterAction: () => {
//                         _player.OnReload?.Invoke();
//                         if (_player.debugStateTransitions) Debug.Log("Entered Reloading state");
//                     }
//                 )
//             },
//             { 
//                 PlayerState.PickingUp, 
//                 new StateActions(
//                     enterAction: () => {
//                         _player.OnPickUp?.Invoke();
//                         if (_player.debugStateTransitions) Debug.Log("Entered PickingUp state");
//                     }
//                 )
//             },
//             { 
//                 PlayerState.Dead, 
//                 new StateActions(
//                     enterAction: () => {
//                         _player.OnDeath?.Invoke();
//                         if (_player.debugStateTransitions) Debug.Log("Entered Dead state");
//                     },
//                     exitAction: null
//                 )
//             }
//         };
//     }

//     public void SetStateDirectly(PlayerState newState)
//     {
//         if (CurrentState == newState) return;
        
//         // Exit current state
//         if (_stateActions.TryGetValue(CurrentState, out StateActions currentActions))
//         {
//             currentActions.ExitAction?.Invoke();
//         }

//         // Change state
//         PlayerState previousState = CurrentState;
//         CurrentState = newState;
        
//         // Enter new state
//         if (_stateActions.TryGetValue(CurrentState, out StateActions newActions))
//         {
//             newActions.EnterAction?.Invoke();
//         }
        
//         OnStateChanged?.Invoke(newState);
        
//         if (_player.debugStateTransitions)
//             Debug.Log($"State changed from {previousState} to {newState}");
//     }

//     public void UpdateState()
//     {
//         PlayerState? highestPriorityState = null;
//         int highestPriority = -1;

//         foreach (var transition in _transitions)
//         {
//             if (transition.Value.Condition() && 
//                 transition.Value.Priority > highestPriority)
//             {
//                 highestPriorityState = transition.Key;
//                 highestPriority = transition.Value.Priority;
//             }
//         }

//         if (highestPriorityState.HasValue && CurrentState != highestPriorityState.Value)
//         {
//             SetStateDirectly(highestPriorityState.Value);
//         }
//     }

//     private class StateTransition
//     {
//         public Func<bool> Condition { get; }
//         public int Priority { get; }

//         public StateTransition(Func<bool> condition, int priority)
//         {
//             Condition = condition ?? throw new ArgumentNullException(nameof(condition));
//             Priority = priority;
//         }
//     }

//     private class StateActions
//     {
//         public Action EnterAction { get; }
//         public Action ExitAction { get; }

//         public StateActions(Action enterAction, Action exitAction = null)
//         {
//             EnterAction = enterAction;
//             ExitAction = exitAction;
//         }
//     }
// }