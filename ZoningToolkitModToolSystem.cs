using Game;
using Game.Buildings;
using Game.Common;
using Game.Input;
using Game.Net;
using Game.Notifications;
using Game.Objects;
using Game.Prefabs;
using Game.Routes;
using Game.Tools;
using Game.Vehicles;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine.Scripting;
using ZoningToolkit.Components;
using ZoningToolkit.Utilties;

namespace ZoningToolkit
{
    public struct HighlightEntitiesJob : IJob
    {
        public EntityCommandBuffer commandBuffer;
        public Entity entityToHighlight;
        public void Execute()
        {
            this.commandBuffer.AddComponent<Highlighted>(this.entityToHighlight);
            this.commandBuffer.AddComponent<Updated>(this.entityToHighlight);
        }
    }

    public struct UnHighlightEntitiesJob : IJob
    {
        public EntityCommandBuffer commandBuffer;
        public Entity entityToUnhighlight;
        public void Execute()
        {
            this.commandBuffer.RemoveComponent<Highlighted>(this.entityToUnhighlight);
            this.commandBuffer.AddComponent<Updated>(this.entityToUnhighlight);
        }
    }

    partial class ZoningToolkitModToolSystem : ToolBaseSystem
    {
        // This holds certain state that we use in our class
        // and is reset every onUpdate call.
        private struct OnUpdateMemory
        {
            public JobHandle currentInputDeps;
            public EntityCommandBuffer commandBufferSystem;
        }

        private struct WorkingState
        {
            public Entity lastRaycastEntity;
            public NativeQueue<Entity> lastRaycastEntities;
        }

        // Fields related to the Tool System itself.
        private ProxyAction applyAction;
        private ProxyAction cancelAction;
        private ToolOutputBarrier toolOutputBarrier;
        private NetToolSystem netToolSystem;
        private ToolSystem toolSystem;
        private ToolBaseSystem previousToolSystem;
        public bool toolEnabled {  get; private set; }

        private ZoningToolkitModToolSystemStateMachine toolStateMachine;
        private OnUpdateMemory onUpdateMemory;
        private WorkingState workingState;

        public override string toolID => "Zoning Toolkit Tool";

        protected override void OnCreate()
        {
            this.getLogger().Info($"Creating {toolID}.");
            base.OnCreate();

            this.applyAction = InputManager.instance.FindAction("Tool", "Default Tool");
            this.cancelAction = InputManager.instance.FindAction("Tool", "Mouse Cancel");

            this.toolOutputBarrier = World.GetOrCreateSystemManaged<ToolOutputBarrier>();
            this.netToolSystem = World.GetOrCreateSystemManaged<NetToolSystem>();
            this.toolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            this.toolEnabled = false;
            this.toolStateMachine = new ZoningToolkitModToolSystemStateMachine(
                new Dictionary<(ZoningToolkitModToolSystemState previous, ZoningToolkitModToolSystemState next), StateCallback>
                {
                    { (ZoningToolkitModToolSystemState.Default, ZoningToolkitModToolSystemState.Selected), this.entityHighlighted },
                    { (ZoningToolkitModToolSystemState.Default, ZoningToolkitModToolSystemState.Default), this.hoverUpdate },
                    { (ZoningToolkitModToolSystemState.Default, ZoningToolkitModToolSystemState.Selecting), this.startDragSelecting },
                    { (ZoningToolkitModToolSystemState.Selecting, ZoningToolkitModToolSystemState.Selecting), this.keepDragging },
                    { (ZoningToolkitModToolSystemState.Selecting, ZoningToolkitModToolSystemState.Selected), this.stopDragging },

                }
            );

            this.getLogger().Info($"Done Creating {toolID}.");
        }

        private JobHandle stopDragging(ZoningToolkitModToolSystemState previousState, ZoningToolkitModToolSystemState nextState)
        {
            this.entityUnhighlighted(previousState, nextState);

            return this.onUpdateMemory.currentInputDeps;
        }
        private JobHandle hoverUpdate(ZoningToolkitModToolSystemState previousState, ZoningToolkitModToolSystemState nextState)
        {
            this.entityUnhighlighted(previousState, nextState);
            this.entityHighlighted(previousState, nextState);
            return this.onUpdateMemory.currentInputDeps;
        }

        private JobHandle startDragSelecting(ZoningToolkitModToolSystemState previousState, ZoningToolkitModToolSystemState nextState)
        {
            this.entityHighlighted(previousState, nextState);
            return this.onUpdateMemory.currentInputDeps;
        }

        private JobHandle keepDragging(ZoningToolkitModToolSystemState previousState, ZoningToolkitModToolSystemState nextState)
        {
            this.entityHighlighted(previousState, nextState);
            return this.onUpdateMemory.currentInputDeps;
        }

        private JobHandle entityUnhighlighted(ZoningToolkitModToolSystemState previousState, ZoningToolkitModToolSystemState nextState)
        {
            while (this.workingState.lastRaycastEntities.TryDequeue(out Entity entity))
            {
                this.getLogger().Info("Dequeued entity from queue.");
                if (this.workingState.lastRaycastEntity != entity)
                {
                    this.getLogger().Info("Removing highlight from entity.");
                    JobHandle unhighlightJob = new UnHighlightEntitiesJob()
                    {
                        entityToUnhighlight = entity,
                        commandBuffer = this.onUpdateMemory.commandBufferSystem
                    }.Schedule(onUpdateMemory.currentInputDeps);
                    this.onUpdateMemory.currentInputDeps = JobHandle.CombineDependencies(unhighlightJob, this.onUpdateMemory.currentInputDeps);
                }
            }

            this.workingState.lastRaycastEntities.Enqueue(this.workingState.lastRaycastEntity);

            return this.onUpdateMemory.currentInputDeps;
        }
        private JobHandle entityHighlighted(ZoningToolkitModToolSystemState previousState, ZoningToolkitModToolSystemState nextState)
        {
            this.getLogger().Info("Highlighting Entity.");
            if (this.GetRaycastResult(out Entity entity, out RaycastHit raycastHit))
            {
                this.getLogger().Info($"Raycast hit entity {entity} at {raycastHit}");
                if (this.workingState.lastRaycastEntity != entity)
                {
                    this.getLogger().Info("Highlighting entity.");
                    this.workingState.lastRaycastEntity = entity;
                    this.workingState.lastRaycastEntities.Enqueue(entity);
                    JobHandle highlightJob = new HighlightEntitiesJob()
                    {
                        entityToHighlight = entity,
                        commandBuffer = this.onUpdateMemory.commandBufferSystem
                    }.Schedule(onUpdateMemory.currentInputDeps);
                    this.onUpdateMemory.currentInputDeps = JobHandle.CombineDependencies(highlightJob, this.onUpdateMemory.currentInputDeps);
                }
            } else
            {
                this.getLogger().Info("No entity hit.");
            }

            return this.onUpdateMemory.currentInputDeps;
        }

        protected override void OnStartRunning()
        {
            this.getLogger().Info($"Started running tool {toolID}");
            base.OnStartRunning();
            this.applyAction.shouldBeEnabled = true;
            this.cancelAction.shouldBeEnabled = true;
            this.applyAction.ClearDisplayProperties();
            this.cancelAction.ClearDisplayProperties();
            this.onUpdateMemory = default;
            this.workingState = new WorkingState()
            {
                lastRaycastEntity = Entity.Null,
                lastRaycastEntities = new NativeQueue<Entity>(Allocator.Persistent)
            };
            this.toolStateMachine.reset();
        }

        protected override void OnStopRunning()
        {
            this.getLogger().Info($"Stopped running tool {toolID}");
            base.OnStopRunning();
            this.applyAction.shouldBeEnabled = false;
            this.cancelAction.shouldBeEnabled = false;
            this.workingState.lastRaycastEntities.Dispose();
        }

        public override void InitializeRaycast()
        {
            base.InitializeRaycast();

            this.m_ToolRaycastSystem.typeMask = TypeMask.Lanes | TypeMask.Net;
            this.m_ToolRaycastSystem.netLayerMask = Layer.Road;
            this.m_ToolRaycastSystem.areaTypeMask = Game.Areas.AreaTypeMask.Surfaces;
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            this.onUpdateMemory = new OnUpdateMemory()
            {
                currentInputDeps = inputDeps,
                commandBufferSystem = this.toolOutputBarrier.CreateCommandBuffer()
            };

            this.toolStateMachine.transition(applyAction, cancelAction);

            this.toolOutputBarrier.AddJobHandleForProducer(this.onUpdateMemory.currentInputDeps);
            return inputDeps;
        }

        public override PrefabBase GetPrefab()
        {
            return this.netToolSystem.GetPrefab();
        }

        public override bool TrySetPrefab(PrefabBase prefab)
        {
            return this.netToolSystem.TrySetPrefab(prefab) && this.toolEnabled;
        }

        internal void EnableTool()
        {
            if (!this.toolEnabled)
            {
                this.toolEnabled = true;

                if (this.toolSystem.activeTool != this)
                {
                    this.previousToolSystem = this.toolSystem.activeTool;
                    this.toolSystem.activeTool = this;
                }
            }   
        }

        internal void DisableTool()
        {
            if (this.toolEnabled)
            {
                this.toolEnabled = false;

                if (this.toolSystem.activeTool != this.previousToolSystem)
                {
                    this.toolSystem.activeTool = this.previousToolSystem;
                }
            }
        }

        private struct TypeHandle
        {
            [ReadOnly]
            public ComponentTypeHandle<Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

            [ReadOnly]
            public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

            [ReadOnly]
            public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

            [ReadOnly]
            public BufferLookup<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferLookup;

            [ReadOnly]
            public ComponentLookup<LocalTransformCache> __Game_Tools_LocalTransformCache_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<Game.Net.Node> __Game_Net_Node_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<Game.Tools.EditorContainer> __Game_Tools_EditorContainer_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<Game.Objects.Elevation> __Game_Objects_Elevation_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<Attached> __Game_Objects_Attached_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<Position> __Game_Routes_Position_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<Connected> __Game_Routes_Connected_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<Icon> __Game_Notifications_Icon_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

            [ReadOnly]
            public BufferLookup<Game.Areas.Node> __Game_Areas_Node_RO_BufferLookup;

            [ReadOnly]
            public BufferLookup<RouteWaypoint> __Game_Routes_RouteWaypoint_RO_BufferLookup;

            [ReadOnly]
            public BufferLookup<AggregateElement> __Game_Net_AggregateElement_RO_BufferLookup;

            [ReadOnly]
            public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

            [ReadOnly]
            public ComponentLookup<Temp> __Game_Tools_Temp_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<Target> __Game_Common_Target_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<Debug> __Game_Tools_Debug_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<Vehicle> __Game_Vehicles_Vehicle_RO_ComponentLookup;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void __AssignHandles(ref SystemState state)
            {
                __Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Transform>(isReadOnly: true);
                __Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
                __Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
                __Game_Buildings_InstalledUpgrade_RO_BufferLookup = state.GetBufferLookup<InstalledUpgrade>(isReadOnly: true);
                __Game_Tools_LocalTransformCache_RO_ComponentLookup = state.GetComponentLookup<LocalTransformCache>(isReadOnly: true);
                __Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: true);
                __Game_Net_Node_RO_ComponentLookup = state.GetComponentLookup<Game.Net.Node>(isReadOnly: true);
                __Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
                __Game_Tools_EditorContainer_RO_ComponentLookup = state.GetComponentLookup<Game.Tools.EditorContainer>(isReadOnly: true);
                __Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
                __Game_Objects_Elevation_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Elevation>(isReadOnly: true);
                __Game_Objects_Attached_RO_ComponentLookup = state.GetComponentLookup<Attached>(isReadOnly: true);
                __Game_Routes_Position_RO_ComponentLookup = state.GetComponentLookup<Position>(isReadOnly: true);
                __Game_Routes_Connected_RO_ComponentLookup = state.GetComponentLookup<Connected>(isReadOnly: true);
                __Game_Notifications_Icon_RO_ComponentLookup = state.GetComponentLookup<Icon>(isReadOnly: true);
                __Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
                __Game_Areas_Node_RO_BufferLookup = state.GetBufferLookup<Game.Areas.Node>(isReadOnly: true);
                __Game_Routes_RouteWaypoint_RO_BufferLookup = state.GetBufferLookup<RouteWaypoint>(isReadOnly: true);
                __Game_Net_AggregateElement_RO_BufferLookup = state.GetBufferLookup<AggregateElement>(isReadOnly: true);
                __Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
                __Game_Tools_Temp_RO_ComponentLookup = state.GetComponentLookup<Temp>(isReadOnly: true);
                __Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
                __Game_Common_Target_RO_ComponentLookup = state.GetComponentLookup<Target>(isReadOnly: true);
                __Game_Tools_Debug_RO_ComponentLookup = state.GetComponentLookup<Debug>(isReadOnly: true);
                __Game_Vehicles_Vehicle_RO_ComponentLookup = state.GetComponentLookup<Vehicle>(isReadOnly: true);
            }
        }

        struct UpdateExistingZoning : IJob
        {
            [ReadOnly]
            public EntityCommandBuffer commandBuffer;

            [ReadOnly]
            public NativeQueue<Entity> entities;

            public void Execute()
            {
                while (entities.TryDequeue(out Entity entity))
                {
                    this.getLogger().Info("Adding Zoning Update Required Component to entity");
                    commandBuffer.AddComponent<ZoningUpdateRequired>(entity);
                }

            }
        }
    }
}
