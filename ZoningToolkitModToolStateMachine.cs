using Game.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Jobs;
using ZoningToolkit.Utilties;

namespace ZoningToolkit
{
    internal delegate JobHandle StateCallback(ZoningToolkitModToolSystemState previousState, ZoningToolkitModToolSystemState nextState);
    internal enum ZoningToolkitModToolSystemState
    {
        Default,
        Selecting,
        Selected
    }

    internal class ZoningToolkitModToolSystemStateMachine
    {
        private ZoningToolkitModToolSystemState currentState;
        private Dictionary<(ZoningToolkitModToolSystemState previous, ZoningToolkitModToolSystemState next), StateCallback> transitions;
        internal ZoningToolkitModToolSystemStateMachine(Dictionary<(ZoningToolkitModToolSystemState previous, ZoningToolkitModToolSystemState next), StateCallback> stateTransitions)
        {
            this.currentState = ZoningToolkitModToolSystemState.Default;
            this.transitions = stateTransitions;
        }

        private void tryRunCallback(ZoningToolkitModToolSystemState previousState, ZoningToolkitModToolSystemState nextState)
        {
            if (this.transitions.TryGetValue((previousState, nextState), out StateCallback transitionCallback))
            {
                this.getLogger().Info($"Callback for transition from ${previousState} to {nextState}");
                transitionCallback(previousState, nextState);
            }
        }

        public void transition(ProxyAction applyAction, ProxyAction cancelAction)
        {
            ZoningToolkitModToolSystemState previousState = this.currentState;
            switch (this.currentState)
            {
                case ZoningToolkitModToolSystemState.Default:
                    if (applyAction.WasPressedThisFrame() && applyAction.WasReleasedThisFrame())
                    {
                        this.getLogger().Info("Single Click!");
                        this.currentState = ZoningToolkitModToolSystemState.Selected;
                    }
                    else if (applyAction.WasPressedThisFrame() && !applyAction.WasReleasedThisFrame())
                    {
                        this.getLogger().Info("Press and Hold Click!");
                        this.currentState = ZoningToolkitModToolSystemState.Selecting;
                    }
                    else if (applyAction.IsPressed())
                    {
                        this.getLogger().Info("Holding Click!");
                        this.currentState = ZoningToolkitModToolSystemState.Selecting;
                    }
                    else if (applyAction.WasReleasedThisFrame())
                    {
                        this.getLogger().Info("Click Released!");
                        this.currentState = ZoningToolkitModToolSystemState.Selected;
                    }
                    break;
                case ZoningToolkitModToolSystemState.Selecting:
                    if (applyAction.IsPressed())
                    {
                        this.getLogger().Info("Holding Click!");
                        this.currentState = ZoningToolkitModToolSystemState.Selecting;
                    }
                    else if (!applyAction.IsPressed() && applyAction.WasReleasedThisFrame())
                    {
                        this.getLogger().Info("Click Released!");
                        this.currentState = ZoningToolkitModToolSystemState.Selected;
                    }
                    break;
                case ZoningToolkitModToolSystemState.Selected:
                    // Schedule job here
                    this.getLogger().Info("Resetting state to Default!");
                    this.currentState = ZoningToolkitModToolSystemState.Default;
                    break;
                default:
                    this.getLogger().Info("Default case.");
                    this.currentState = ZoningToolkitModToolSystemState.Default;
                    break;
            }

            tryRunCallback(previousState, this.currentState);
        }

        public void reset()
        {
            this.currentState = ZoningToolkitModToolSystemState.Default;
        }
    }
}
