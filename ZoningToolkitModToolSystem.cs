using Game;
using Game.Areas;
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
using Game.Zones;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine.Scripting;
using ZoningToolkit.Components;
using ZoningToolkit.utils;
using ZoningToolkit.Utilties;
using static Colossal.IO.AssetDatabase.AtlasFrame;

namespace ZoningToolkit
{
    public struct HighlightEntitiesJob : IJob
    {
        public EntityCommandBuffer commandBuffer;
        public Entity entityToHighlight;
        public ComponentLookup<Edge> edgeLookup;
        public void Execute()
        {
            this.commandBuffer.AddComponent<Highlighted>(this.entityToHighlight);
            this.commandBuffer.AddComponent<Updated>(this.entityToHighlight);

            if (edgeLookup.HasComponent(this.entityToHighlight))
            {
                Edge edge = edgeLookup[this.entityToHighlight];

                this.commandBuffer.AddComponent<Updated>(edge.m_Start);
                this.commandBuffer.AddComponent<Updated>(edge.m_End);
            }
        }
    }

    public struct UnHighlightEntitiesJob : IJob
    {
        public EntityCommandBuffer commandBuffer;
        public Entity entityToUnhighlight;
        public ComponentLookup<Edge> edgeLookup;
        public void Execute()
        {
            this.commandBuffer.RemoveComponent<Highlighted>(this.entityToUnhighlight);
            this.commandBuffer.AddComponent<Updated>(this.entityToUnhighlight);

            if (edgeLookup.HasComponent(this.entityToUnhighlight))
            {
                Edge edge = edgeLookup[this.entityToUnhighlight];

                this.commandBuffer.AddComponent<Updated>(edge.m_Start);
                this.commandBuffer.AddComponent<Updated>(edge.m_End);
            }
        }
    }

    public struct BackwardsCompatibilityZoningInfo : IJob
    {
        public Entity backwardsCompatibilityEntity;
        public ComponentLookup<Curve> curveComponentLookup;
        public ComponentLookup<ZoningInfo> zoningInfoComponentLookup;
        public BufferLookup<SubBlock> subBlockBufferLookup;
        public ComponentLookup<Block> blockComponentLookup;
        public EntityCommandBuffer entityCommandBuffer;

        public void Execute()
        {
            if (zoningInfoComponentLookup.HasComponent(backwardsCompatibilityEntity))
            {
                this.getLogger().Info("Nothing to do for this entity since it already has ZoningInfo component.");
            } else
            {
                this.getLogger().Info("Making entity backwards compatible.");
                // Calculate if there blocks on one side or both sides of the curve
                if (curveComponentLookup.HasComponent(backwardsCompatibilityEntity))
                {
                    Curve curve = curveComponentLookup[backwardsCompatibilityEntity];

                    bool leftBlock = false;
                    bool rightBlock = false;

                    if (subBlockBufferLookup.HasBuffer(backwardsCompatibilityEntity))
                    {
                        DynamicBuffer<SubBlock> subBlockBuffer = subBlockBufferLookup[backwardsCompatibilityEntity];

                        this.getLogger().Info("Going to iterate through blocks.");

                        foreach (var item in subBlockBuffer)
                        {
                            this.getLogger().Info("Processing block.");
                            Block block = blockComponentLookup[item.m_SubBlock];

                            float dotProduct = BlockUtils.blockCurveDotProduct(block, curve);

                            if (dotProduct > 0)
                            {
                                // block is on the left of curve
                                if (block.m_Size.y > 0)
                                {
                                    leftBlock = true;
                                }
                            }
                            else
                            {
                                // block is on the right of curve
                                if (block.m_Size.y > 0)
                                {
                                    rightBlock = true;
                                }
                            }
                            /*this.entityCommandBuffer.AddComponent<ZoningInfoUpdated>(item.m_SubBlock);*/
                            this.getLogger().Info("Block processed.");
                        }
                    }

                    this.getLogger().Info("Setting Zoning info now.");
                    if (leftBlock && rightBlock)
                    {
                        this.entityCommandBuffer.AddComponent(backwardsCompatibilityEntity, new ZoningInfo()
                        {
                            zoningMode = ZoningMode.Default
                        });
                    } else if (rightBlock)
                    {
                        this.entityCommandBuffer.AddComponent(backwardsCompatibilityEntity, new ZoningInfo()
                        {
                            zoningMode = ZoningMode.Right
                        });
                    } else if (leftBlock)
                    {
                        this.entityCommandBuffer.AddComponent(backwardsCompatibilityEntity, new ZoningInfo()
                        {
                            zoningMode = ZoningMode.Left
                        });
                    } else
                    {
                        this.entityCommandBuffer.AddComponent(backwardsCompatibilityEntity, new ZoningInfo()
                        {
                            zoningMode = ZoningMode.None
                        });
                    }
                }
                
            }
        }
    }

    public partial struct UpdateZoningInfo : IJob
    {
        public NativeHashSet<Entity> entityHashSet;
        public ComponentLookup<Curve> curveComponentLookup;
        public ComponentLookup<ZoningInfo> zoningInfoComponentLookup;
        public BufferLookup<SubBlock> subBlockBufferLookup;
        public ComponentLookup<Edge> edgeLookup;
        public EntityCommandBuffer entityCommandBuffer;
        public ZoningInfo newZoningInfo;
        public void Execute()
        {
            NativeArray<Entity> entities = entityHashSet.ToNativeArray(Allocator.TempJob);
            foreach (Entity entity in entities)
            {
                if (curveComponentLookup.HasComponent(entity))
                {
                    this.getLogger().Info("Entity has curve component. Updating Zoning Info.");

                    this.entityCommandBuffer.AddComponent<ZoningInfo>(entity, newZoningInfo);

                    if (subBlockBufferLookup.HasBuffer(entity))
                    {
                        DynamicBuffer<SubBlock> subBlockBuffer = subBlockBufferLookup[entity];

                        foreach (var item in subBlockBuffer)
                        {
                            this.entityCommandBuffer.AddComponent<ZoningInfoUpdated>(item.m_SubBlock);
                        }
                    }
                }

                this.entityCommandBuffer.RemoveComponent<Highlighted>(entity);
                this.entityCommandBuffer.AddComponent<Updated>(entity);

                if (edgeLookup.HasComponent(entity))
                {
                    Edge edge = edgeLookup[entity];

                    this.entityCommandBuffer.AddComponent<Updated>(edge.m_Start);
                    this.entityCommandBuffer.AddComponent<Updated>(edge.m_End);
                }
            }

            entities.Dispose();
            entityHashSet.Clear();
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

        // This holds certain state that the Tool keeps
        // throughout its lifetime.
        internal struct WorkingState
        {
            internal Entity lastRaycastEntity;
            internal NativeHashSet<Entity> lastRaycastEntities;
            internal ZoningMode zoningMode;
        }

        // Fields related to the Tool System itself.
        private ProxyAction applyAction;
        private ProxyAction cancelAction;
        private DisplayNameOverride applyActionNameOverride;
        private ToolOutputBarrier toolOutputBarrier;
        private NetToolSystem netToolSystem;
        private ToolSystem toolSystem;
        private ToolBaseSystem previousToolSystem;
        private ZoningToolkitModToolSystemStateMachine toolStateMachine;
        private TypeHandle typeHandle;
        private OnUpdateMemory onUpdateMemory;

        internal bool toolEnabled { get; private set; }
        internal WorkingState workingState;

        public override string toolID => "Zoning Toolkit Tool";

        protected override void OnCreateForCompiler()
        {
            base.OnCreateForCompiler();

            this.typeHandle.__AssignHandles(ref this.CheckedStateRef);
        }
        protected override void OnCreate()
        {
            this.getLogger().Info($"Creating {toolID}.");
            base.OnCreate();

            this.applyAction = InputManager.instance.FindAction("Tool", "Apply");
            this.cancelAction = InputManager.instance.FindAction("Tool", "Cancel");
            this.applyActionNameOverride = new DisplayNameOverride(nameof(ZoningToolkitModToolSystem), this.applyAction, "Updated Zoning Side", 20);

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
            JobHandle updateZoningInfoJob = new UpdateZoningInfo()
            {
                curveComponentLookup = this.typeHandle.__Game_Net_Curve_RW_ComponentLookup,
                zoningInfoComponentLookup = this.typeHandle.__Game_Zoning_Info_RW_ComponentLookup,
                entityCommandBuffer = this.onUpdateMemory.commandBufferSystem,
                entityHashSet = this.workingState.lastRaycastEntities,
                subBlockBufferLookup = this.typeHandle.__Game_SubBlock_RW_BufferLookup,
                edgeLookup = this.typeHandle.__Game_Edge_RW_ComponentLookup,
                newZoningInfo = new ZoningInfo()
                {
                    zoningMode = this.workingState.zoningMode
                }
            }.Schedule(this.onUpdateMemory.currentInputDeps);
            this.onUpdateMemory.currentInputDeps = JobHandle.CombineDependencies(this.onUpdateMemory.currentInputDeps, updateZoningInfoJob);
            return this.onUpdateMemory.currentInputDeps;
        }
        private JobHandle hoverUpdate(ZoningToolkitModToolSystemState previousState, ZoningToolkitModToolSystemState nextState)
        {
            this.entityHighlighted(previousState, nextState);
            return this.onUpdateMemory.currentInputDeps;
        }

        private JobHandle startDragSelecting(ZoningToolkitModToolSystemState previousState, ZoningToolkitModToolSystemState nextState)
        {
            this.selectEntity(previousState, nextState);
            return this.onUpdateMemory.currentInputDeps;
        }

        private JobHandle keepDragging(ZoningToolkitModToolSystemState previousState, ZoningToolkitModToolSystemState nextState)
        {
            this.selectEntity(previousState, nextState);
            return this.onUpdateMemory.currentInputDeps;
        }

        private JobHandle selectEntity(ZoningToolkitModToolSystemState previousState, ZoningToolkitModToolSystemState nextState)
        {
            this.getLogger().Info("Trying to Select Entity.");
            if (this.GetRaycastResult(out Entity entity, out RaycastHit raycastHit))
            {
                if (!this.workingState.lastRaycastEntities.Contains(entity))
                {
                    this.workingState.lastRaycastEntities.Add(entity);
                    JobHandle highlightJob = new HighlightEntitiesJob()
                    {
                        entityToHighlight = entity,
                        commandBuffer = this.onUpdateMemory.commandBufferSystem,
                        edgeLookup = this.typeHandle.__Game_Edge_RW_ComponentLookup
                    }.Schedule(onUpdateMemory.currentInputDeps);
                    this.onUpdateMemory.currentInputDeps = JobHandle.CombineDependencies(highlightJob, this.onUpdateMemory.currentInputDeps);
                }
            }

            return this.onUpdateMemory.currentInputDeps;
        }
        private JobHandle entityHighlighted(ZoningToolkitModToolSystemState previousState, ZoningToolkitModToolSystemState nextState)
        {
            this.getLogger().Debug("Trying to Highlight Entity.");
            Entity previousRaycastEntity = this.workingState.lastRaycastEntity;
            if (this.GetRaycastResult(out Entity entity, out RaycastHit raycastHit))
            {
                this.getLogger().Debug($"Raycast hit entity {entity} at {raycastHit}");
                if (this.workingState.lastRaycastEntity != entity)
                {
                    this.getLogger().Info("Highlighting entity.");

                    if (previousRaycastEntity != Entity.Null)
                    {
                        JobHandle unhighlightJob = new UnHighlightEntitiesJob()
                        {
                            commandBuffer = this.onUpdateMemory.commandBufferSystem,
                            entityToUnhighlight = previousRaycastEntity,
                            edgeLookup = this.typeHandle.__Game_Edge_RW_ComponentLookup
                        }.Schedule(onUpdateMemory.currentInputDeps);
                        this.onUpdateMemory.currentInputDeps = JobHandle.CombineDependencies(unhighlightJob, this.onUpdateMemory.currentInputDeps);
                    }

                    this.workingState.lastRaycastEntity = entity;
                    JobHandle highlightJob = new HighlightEntitiesJob()
                    {
                        entityToHighlight = this.workingState.lastRaycastEntity,
                        commandBuffer = this.onUpdateMemory.commandBufferSystem,
                        edgeLookup = this.typeHandle.__Game_Edge_RW_ComponentLookup
                    }.Schedule(onUpdateMemory.currentInputDeps);
                    this.onUpdateMemory.currentInputDeps = JobHandle.CombineDependencies(highlightJob, this.onUpdateMemory.currentInputDeps);

                    /*JobHandle backwardsCompatJob = new BackwardsCompatibilityZoningInfo()
                    {
                        curveComponentLookup = this.typeHandle.__Game_Net_Curve_RW_ComponentLookup,
                        zoningInfoComponentLookup = this.typeHandle.__Game_Zoning_Info_RW_ComponentLookup,
                        entityCommandBuffer = this.onUpdateMemory.commandBufferSystem,
                        subBlockBufferLookup = this.typeHandle.__Game_SubBlock_RW_BufferLookup,
                        backwardsCompatibilityEntity = this.workingState.lastRaycastEntity,
                        blockComponentLookup = this.typeHandle.__Game_Block_RW_ComponentLookup
                    }.Schedule(onUpdateMemory.currentInputDeps);
                    this.onUpdateMemory.currentInputDeps = JobHandle.CombineDependencies(backwardsCompatJob, this.onUpdateMemory.currentInputDeps);*/
                }
            } else
            {
                this.getLogger().Info("No entity hit.");
                this.workingState.lastRaycastEntity = Entity.Null;

                if (previousRaycastEntity != Entity.Null)
                {
                    JobHandle unhighlightJob = new UnHighlightEntitiesJob()
                    {
                        commandBuffer = this.onUpdateMemory.commandBufferSystem,
                        entityToUnhighlight = previousRaycastEntity,
                        edgeLookup = this.typeHandle.__Game_Edge_RW_ComponentLookup
                    }.Schedule(onUpdateMemory.currentInputDeps);
                    this.onUpdateMemory.currentInputDeps = JobHandle.CombineDependencies(unhighlightJob, this.onUpdateMemory.currentInputDeps);
                }
            }

            return this.onUpdateMemory.currentInputDeps;
        }

        protected override void OnStartRunning()
        {
            this.getLogger().Info($"Started running tool {toolID}");
            base.OnStartRunning();
            this.toolEnabled = true;
            this.applyAction.shouldBeEnabled = true;
            this.cancelAction.shouldBeEnabled = true;
            this.onUpdateMemory = default;
            this.workingState.lastRaycastEntity = Entity.Null;
            this.workingState.lastRaycastEntities = new NativeHashSet<Entity>(32, Allocator.Persistent);
            this.applyActionNameOverride.state = DisplayNameOverride.State.GlobalHint;
            this.toolStateMachine.reset();
        }

        protected override void OnStopRunning()
        {
            this.getLogger().Info($"Stopped running tool {toolID}");
            base.OnStopRunning();
            this.applyAction.shouldBeEnabled = false;
            this.cancelAction.shouldBeEnabled = false;
            this.applyActionNameOverride.state = DisplayNameOverride.State.Off;
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
            if (this.m_FocusChanged)
                return inputDeps;

            base.requireZones = true;
            base.requireAreas |= AreaTypeMask.Lots;
            this.getLogger().Info($"Apply Action enabled: {this.applyAction.enabled}");
            this.getLogger().Info($"Apply Action: {this.applyAction.WasPressedThisFrame()}");
            if (this.GetPrefab() != null)
            {
                this.UpdateInfoview(this.m_ToolSystem.actionMode.IsEditor() ? Entity.Null : this.m_PrefabSystem.GetEntity(this.GetPrefab()));
            }
            this.typeHandle.__UpdateComponents(ref this.CheckedStateRef);
            this.onUpdateMemory = new OnUpdateMemory()
            {
                currentInputDeps = inputDeps,
                commandBufferSystem = this.toolOutputBarrier.CreateCommandBuffer()
            };

            this.toolStateMachine.transition(applyAction, cancelAction);

            this.toolOutputBarrier.AddJobHandleForProducer(this.onUpdateMemory.currentInputDeps);
            return this.onUpdateMemory.currentInputDeps;
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
            public ComponentLookup<Curve> __Game_Net_Curve_RW_ComponentLookup;

            public ComponentLookup<ZoningInfo> __Game_Zoning_Info_RW_ComponentLookup;

            public BufferLookup<SubBlock> __Game_SubBlock_RW_BufferLookup;

            public ComponentLookup<Block> __Game_Block_RW_ComponentLookup;

            public ComponentLookup<Edge> __Game_Edge_RW_ComponentLookup;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void __AssignHandles(ref SystemState state)
            {
                __Game_Net_Curve_RW_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: false);
                __Game_Zoning_Info_RW_ComponentLookup = state.GetComponentLookup<ZoningInfo>(isReadOnly: false);
                __Game_SubBlock_RW_BufferLookup = state.GetBufferLookup<SubBlock>(isReadOnly: false);
                __Game_Block_RW_ComponentLookup = state.GetComponentLookup<Block>(isReadOnly: false);
                __Game_Edge_RW_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: false);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void __UpdateComponents(ref SystemState state)
            {
                __Game_Net_Curve_RW_ComponentLookup.Update(ref state);
                __Game_Zoning_Info_RW_ComponentLookup.Update(ref state);
                __Game_SubBlock_RW_BufferLookup.Update(ref state);
                __Game_Block_RW_ComponentLookup.Update(ref state);
                __Game_Edge_RW_ComponentLookup.Update(ref state);
            }
        }
    }
}
