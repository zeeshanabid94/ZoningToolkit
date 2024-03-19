using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using Colossal.Logging;
using Colossal.Mathematics;
using Game;
using Game.Common;
using Game.Net;
using Game.Tools;
using Game.Zones;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;
using ZoningToolkit.Components;
using ZoningToolkit.utils;
using ZoningToolkit.Utilties;

namespace ZoningToolkit.Systems
{
    // [UpdateAfter(typeof(BlockSystem))]
    public partial class ZoningToolkitModSystem : GameSystemBase
    {

        private EntityQuery newEntityQuery;
        private EntityQuery updateEntityQuery;
        private ComponentTypeHandle<Block> blockComponentTypeHandle;
        private EntityTypeHandle entityTypeHandle;
        private ComponentTypeHandle<ValidArea> validAreaComponentTypeHandle;
        private ComponentTypeHandle<Deleted> deletedTypeHandle;
        public ComponentTypeHandle<Owner> ownerTypeHandle;

        public BufferTypeHandle<Cell> cellBufferTypeHandle;

        public ComponentLookup<Owner> ownerComponentLookup;
        [ReadOnly]
        protected ComponentLookup<Curve> curveComponentLookup;
        private ComponentLookup<ZoningInfo> zoningInfoComponentLookup;
        private ComponentLookup<Deleted> deletedLookup;
        private ComponentLookup<Applied> appliedLookup;
        private ComponentLookup<Updated> updatedLookup;
        private ComponentLookup<ZoningInfoUpdated> zoningInfoUpdatedLookup;
        private ModificationBarrier4B modificationBarrier4B;
        internal ZoningMode zoningMode;

        public NativeQueue<Entity>.ReadOnly entitiesToUpdate
        {
            set { entitiesToUpdate = value; }
            get { return entitiesToUpdate; }
        }

        protected override void OnCreate()
        {
            this.getLogger().Info("Creating ZoningToolkitMod GameSystem.");
            base.OnCreate();

            this.newEntityQuery = this.GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[] {
                    ComponentType.ReadWrite<Block>(),
                    ComponentType.ReadWrite<Owner>(),
                    ComponentType.ReadOnly<Cell>(),
                    ComponentType.ReadOnly<ValidArea>()
                },
                Any = new ComponentType[] {
                    ComponentType.ReadOnly<Created>(),
                    ComponentType.ReadOnly<Deleted>()
                }
            });

            this.updateEntityQuery = this.GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[] {
                    ComponentType.ReadWrite<Block>(),
                    ComponentType.ReadWrite<Owner>(),
                    ComponentType.ReadOnly<Cell>(),
                    ComponentType.ReadOnly<ValidArea>(),
                    ComponentType.ReadOnly<ZoningInfoUpdated>(),
                },
            });

            // Component to use
            this.blockComponentTypeHandle = this.GetComponentTypeHandle<Block>();
            this.ownerComponentLookup = this.GetComponentLookup<Owner>();
            this.curveComponentLookup = this.GetComponentLookup<Curve>(true);
            this.zoningInfoComponentLookup = this.GetComponentLookup<ZoningInfo>();
            this.entityTypeHandle = this.GetEntityTypeHandle();
            this.validAreaComponentTypeHandle = this.GetComponentTypeHandle<ValidArea>();
            this.deletedTypeHandle = this.GetComponentTypeHandle<Deleted>();
            this.ownerTypeHandle = this.GetComponentTypeHandle<Owner>();
            this.deletedLookup = this.GetComponentLookup<Deleted>();
            this.cellBufferTypeHandle = this.GetBufferTypeHandle<Cell>();
            this.appliedLookup = this.GetComponentLookup<Applied>();
            this.updatedLookup = this.GetComponentLookup<Updated>();
            this.zoningInfoUpdatedLookup = this.GetComponentLookup<ZoningInfoUpdated>();

            // other systems to use
            this.modificationBarrier4B = World.GetOrCreateSystemManaged<ModificationBarrier4B>();

            this.RequireAnyForUpdate(this.newEntityQuery, this.updateEntityQuery);
        }

        [Preserve]
        protected override void OnUpdate()
        {
            this.getLogger().Info("On Update ZoneToolkit System.");
            this.blockComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.ownerComponentLookup.Update(ref this.CheckedStateRef);
            this.curveComponentLookup.Update(ref this.CheckedStateRef);
            this.zoningInfoComponentLookup.Update(ref this.CheckedStateRef);
            this.entityTypeHandle.Update(ref this.CheckedStateRef);
            this.validAreaComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.deletedTypeHandle.Update(ref CheckedStateRef);
            this.ownerTypeHandle.Update(ref CheckedStateRef);
            this.deletedLookup.Update(ref CheckedStateRef);
            this.cellBufferTypeHandle.Update(ref CheckedStateRef);
            this.appliedLookup.Update(ref CheckedStateRef);
            this.updatedLookup.Update(ref CheckedStateRef);
            this.zoningInfoUpdatedLookup.Update(ref CheckedStateRef);

            EntityCommandBuffer entityCommandBuffer = this.modificationBarrier4B.CreateCommandBuffer();

            NativeParallelHashMap<float2, Entity> deletedEntitiesByStartPoint = new NativeParallelHashMap<float2, Entity>(32, Allocator.Temp);
            NativeParallelHashMap<float2, Entity> deletedEntitiesByEndPoint = new NativeParallelHashMap<float2, Entity>(32, Allocator.Temp);

            JobHandle outJobHandle = this.Dependency;

            JobHandle collectDeletedEntities = new CollectDeletedCurves()
            {
                curveLookup = curveComponentLookup,
                deletedTypeHandle = deletedTypeHandle,
                ownerTypeHandle = ownerTypeHandle,
                deletedLookup = deletedLookup,
                curvesByStartPoint = deletedEntitiesByStartPoint,
                curvesByEndPoint = deletedEntitiesByEndPoint
            }.Schedule(this.newEntityQuery, outJobHandle);
            outJobHandle = JobHandle.CombineDependencies(collectDeletedEntities, outJobHandle);

            if (!this.newEntityQuery.IsEmptyIgnoreFilter)
            {
                this.getLogger().Info("Updating zoning for newly created roads.");
                JobHandle jobHandle = new UpdateZoneData()
                {
                    blockComponentTypeHandle = this.blockComponentTypeHandle,
                    validAreaComponentTypeHandle = this.validAreaComponentTypeHandle,
                    curveComponentLookup = this.curveComponentLookup,
                    entityCommandBuffer = entityCommandBuffer,
                    entityTypeHandle = this.entityTypeHandle,
                    ownerComponentLookup = this.ownerComponentLookup,
                    zoningMode = this.zoningMode,
                    zoningInfoComponentLookup = this.zoningInfoComponentLookup,
                    bufferTypeHandle = this.cellBufferTypeHandle,
                    appliedLookup = this.appliedLookup,
                    entitiesByStartPoint = deletedEntitiesByStartPoint,
                    entitiesByEndPoint = deletedEntitiesByEndPoint,
                }.Schedule(this.newEntityQuery, outJobHandle);
                outJobHandle = JobHandle.CombineDependencies(outJobHandle, jobHandle);
            }

            if (!this.updateEntityQuery.IsEmptyIgnoreFilter)
            {
                this.getLogger().Info("Updating zoning for existing roads.");
                JobHandle jobHandle = new UpdateZoningInfo()
                {
                    blockComponentTypeHandle = this.blockComponentTypeHandle,
                    validAreaComponentTypeHandle = this.validAreaComponentTypeHandle,
                    curveComponentLookup = this.curveComponentLookup,
                    entityCommandBuffer = entityCommandBuffer,
                    entityTypeHandle = this.entityTypeHandle,
                    ownerComponentLookup = this.ownerComponentLookup,
                    zoningMode = this.zoningMode,
                    zoningInfoComponentLookup = this.zoningInfoComponentLookup,
                    zoningInfoUpdateComponentLookup = this.zoningInfoUpdatedLookup,
                    bufferTypeHandle = this.cellBufferTypeHandle,
                    updatedLookup = this.updatedLookup
                }.Schedule(this.updateEntityQuery, outJobHandle);
                outJobHandle = JobHandle.CombineDependencies(outJobHandle, jobHandle);
            }

            JobHandle disposeMaps = new DisposeHashMaps()
            {
                toDispose1 = deletedEntitiesByStartPoint,
                toDispose2 = deletedEntitiesByEndPoint
            }.Schedule(outJobHandle);

            this.Dependency = JobHandle.CombineDependencies(disposeMaps, outJobHandle);

            this.modificationBarrier4B.AddJobHandleForProducer(this.Dependency);
        }

        public void setZoningMode(string zoningMode)
        {
            this.getLogger().Info($"Changing zoning mode to ${zoningMode}");
            switch (zoningMode)
            {
                case "Left":
                    this.zoningMode = ZoningMode.Left;
                    break;
                case "Right":
                    this.zoningMode = ZoningMode.Right;
                    break;
                case "Default":
                    this.zoningMode = ZoningMode.Default;
                    break;
                case "None":
                    this.zoningMode = ZoningMode.None;
                    break;
                default:
                    this.zoningMode = ZoningMode.Default;
                    break;
            }
        }

        public Vector2 GetTangent(Bezier4x2 curve, float t)
        {
            // Calculate the derivative of the Bezier curve
            float2 derivative = 3 * math.pow(1 - t, 2) * (curve.b - curve.a) +
                                    6 * (1 - t) * t * (curve.c - curve.b) +
                                    3 * math.pow(t, 2) * (curve.d - curve.c);
            return new Vector2(derivative.x, derivative.y);
        }

        private struct UpdateZoningInfo : IJobChunk
        {
            [ReadOnly]
            public ZoningMode zoningMode;
            [ReadOnly]
            public EntityTypeHandle entityTypeHandle;
            public ComponentTypeHandle<Block> blockComponentTypeHandle;
            public ComponentTypeHandle<ValidArea> validAreaComponentTypeHandle;
            public BufferTypeHandle<Cell> bufferTypeHandle;
            public ComponentLookup<Owner> ownerComponentLookup;
            [ReadOnly]
            public ComponentLookup<Curve> curveComponentLookup;
            public ComponentLookup<ZoningInfo> zoningInfoComponentLookup;
            public ComponentLookup<ZoningInfoUpdated> zoningInfoUpdateComponentLookup;
            public EntityCommandBuffer entityCommandBuffer;
            public ComponentLookup<Updated> updatedLookup;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                this.getLogger().Info("Executing UpdateZoningInfo Job.");
                Stopwatch stopwatch = new Stopwatch();

                stopwatch.Start();
                NativeArray<Block> blocks = chunk.GetNativeArray(ref this.blockComponentTypeHandle);
                NativeArray<Entity> entities = chunk.GetNativeArray(this.entityTypeHandle);
                BufferAccessor<Cell> cellBuffers = chunk.GetBufferAccessor(ref this.bufferTypeHandle);
                NativeArray<ValidArea> validAreas = chunk.GetNativeArray(ref this.validAreaComponentTypeHandle);

                foreach (var pair in entities.Select((item, index) => new { Item = item, Index = index }))
                {
                    Entity entity = pair.Item;

                    if (ownerComponentLookup.HasComponent(entity))
                    {
                        Owner owner = ownerComponentLookup[entity];

                        ZoningInfo entityZoningInfo;

                        if (zoningInfoComponentLookup.HasComponent(owner.m_Owner))
                        {
                            entityZoningInfo = zoningInfoComponentLookup[owner.m_Owner];

                            this.getLogger().Info($"Found Zoning Info {entityZoningInfo}");

                            if (zoningInfoUpdateComponentLookup.HasComponent(entity))
                            {
                                this.getLogger().Info("Found ZoningInfoUpdate component on owner.");

                                Curve curve = curveComponentLookup[owner.m_Owner];
                                Block block = blocks[pair.Index];
                                this.getLogger().Info($"Processing Curve a: ${curve.m_Bezier.a}, b: ${curve.m_Bezier.b}, c: ${curve.m_Bezier.c}, d: ${curve.m_Bezier.d}, length: ${curve.m_Length}");
                                this.getLogger().Info($"Entity is {entity}.");
                                DynamicBuffer<Cell> cells = cellBuffers[pair.Index];
                                ValidArea validArea = validAreas[pair.Index];
                                this.getLogger().Info($"Block direction ${block.m_Direction}");
                                this.getLogger().Info($"Block position ${block.m_Position}");
                                this.getLogger().Info($"Valid Area: ${validArea.m_Area}");

                                float dotProduct = BlockUtils.blockCurveDotProduct(block, curve);

                                this.getLogger().Info($"Dot product: ${dotProduct}");
                                this.getLogger().Info($"Zoning mode is ${entityZoningInfo.zoningMode}");

                                if (BlockUtils.isAnyCellOccupied(ref cells, ref block, ref validArea))
                                {
                                    // Can't replace occupied cells. So skip.
                                    this.getLogger().Info("Cells are occupied. Replacing will not happen.");
                                } else
                                {
                                    BlockUtils.editBlockSizes(dotProduct, entityZoningInfo, validArea, block, entity, entityCommandBuffer);

                                    entityCommandBuffer.AddComponent(owner.m_Owner, entityZoningInfo);
                                }
                                
                                entityCommandBuffer.RemoveComponent<ZoningInfoUpdated>(entity);
                            }
                        } else
                        {
                            this.getLogger().Info("Zoning Info Component not found.");
                        }

                        
                    }
                }
            }
        }

            // [BurstCompile]
        private struct UpdateZoneData : IJobChunk
        {
            [ReadOnly]
            public ZoningMode zoningMode;
            [ReadOnly]
            public EntityTypeHandle entityTypeHandle;
            public ComponentTypeHandle<Block> blockComponentTypeHandle;

            public ComponentTypeHandle<ValidArea> validAreaComponentTypeHandle;

            public ComponentTypeHandle<Deleted> deletedTypeHandle;
            public BufferTypeHandle<Cell> bufferTypeHandle;
            public ComponentLookup<Owner> ownerComponentLookup;
            [ReadOnly]
            public ComponentLookup<Curve> curveComponentLookup;
            public ComponentLookup<ZoningInfo> zoningInfoComponentLookup;

            public ComponentLookup<Applied> appliedLookup;
            public EntityCommandBuffer entityCommandBuffer;
            public NativeParallelHashMap<float2, Entity> entitiesByStartPoint;
            public NativeParallelHashMap<float2, Entity> entitiesByEndPoint;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                this.getLogger().Info("Executing Zone Adjustment Job.");
                Stopwatch stopwatch = new Stopwatch();

                stopwatch.Start();
                NativeArray<Block> blocks = chunk.GetNativeArray(ref this.blockComponentTypeHandle);
                NativeArray<Entity> entities = chunk.GetNativeArray(this.entityTypeHandle);
                BufferAccessor<Cell> cellBuffers = chunk.GetBufferAccessor(ref this.bufferTypeHandle);
                NativeArray<ValidArea> validAreas = chunk.GetNativeArray(ref this.validAreaComponentTypeHandle);

                if (chunk.Has(ref deletedTypeHandle))
                {
                    // Do nothing for deleted blocks.
                    return;
                }

                for (int i = 0; i < blocks.Length; i++)
                {
                    this.getLogger().Info("Processing entity.");
                    Entity entity = entities[i];
                    ZoningInfo entityZoningInfo = new ZoningInfo()
                    {
                        zoningMode = this.zoningMode
                    };
                    if (this.ownerComponentLookup.HasComponent(entity))
                    {
                        Owner owner = this.ownerComponentLookup[entity];

                        if (this.curveComponentLookup.HasComponent(owner.m_Owner))
                        {
                            Curve curve = this.curveComponentLookup[owner.m_Owner];

                            if (!appliedLookup.HasComponent(owner.m_Owner))
                            {
                                if (zoningInfoComponentLookup.HasComponent(owner.m_Owner))
                                {
                                    entityZoningInfo = this.zoningInfoComponentLookup[owner.m_Owner];
                                }
                                else
                                {
                                    // For backwards compatibility
                                    entityZoningInfo = new ZoningInfo()
                                    {
                                        zoningMode = ZoningMode.Default
                                    };
                                }
                            }
                            else
                            {
                                Entity startDeletedEntity;
                                Entity endDeletedEntity;
                                bool isStartPresent = entitiesByStartPoint.TryGetValue(curve.m_Bezier.a.xz, out startDeletedEntity);
                                bool isEndPresent = entitiesByEndPoint.TryGetValue(curve.m_Bezier.d.xz, out endDeletedEntity);

                                if (isStartPresent && isEndPresent && startDeletedEntity == endDeletedEntity)
                                {
                                    this.getLogger().Info("Entity matches at start and end. This entity is probably a replacement.");
                                    entityZoningInfo = zoningInfoComponentLookup[startDeletedEntity];

                                } else if (isStartPresent && isEndPresent)
                                {
                                    this.getLogger().Info("Start and end deleted entity both present.");
                                    Curve startCurve = curveComponentLookup[startDeletedEntity];
                                    Curve endCurve = curveComponentLookup[endDeletedEntity];

                                    if (startCurve.m_Bezier.d.x == endCurve.m_Bezier.a.x && startCurve.m_Bezier.d.z == endCurve.m_Bezier.a.z)
                                    {
                                        this.getLogger().Info("New curve matches deleted entity by start point & end point.");
                                        // Deleted curve form the current complete curve.
                                        ZoningInfo startZoningInfo = zoningInfoComponentLookup[startDeletedEntity];
                                        ZoningInfo endZoningInfo = zoningInfoComponentLookup[endDeletedEntity];

                                        if (startZoningInfo.Equals(endZoningInfo))
                                        {
                                            // If zoning is same, choose that.
                                            this.getLogger().Info("Start and end curve zoning match.");
                                            entityZoningInfo = startZoningInfo;
                                        }
                                        else
                                        {
                                            this.getLogger().Info("Start and end curve zoning don't match. Setting defaut zone.");
                                            // Otherwise choose default zoning.
                                            entityZoningInfo = new ZoningInfo()
                                            {
                                                zoningMode = ZoningMode.Default
                                            };
                                        }
                                    }
                                }
                                else if (isEndPresent)
                                {
                                    this.getLogger().Info("New curve matches deleted entity by end point.");
                                    if (zoningInfoComponentLookup.HasComponent(endDeletedEntity))
                                    {
                                        entityZoningInfo = zoningInfoComponentLookup[endDeletedEntity];
                                    }
                                }
                                else if (isStartPresent)
                                {
                                    this.getLogger().Info("New curve matches deleted entity by start point.");
                                    if (zoningInfoComponentLookup.HasComponent(startDeletedEntity))
                                    {
                                        entityZoningInfo = zoningInfoComponentLookup[startDeletedEntity];
                                    }
                                }


                                if (zoningInfoComponentLookup.HasComponent(owner.m_Owner))
                                {
                                    entityZoningInfo = this.zoningInfoComponentLookup[owner.m_Owner];
                                }
                            }

                            this.getLogger().Info($"Processing Curve a: ${curve.m_Bezier.a}, b: ${curve.m_Bezier.b}, c: ${curve.m_Bezier.c}, d: ${curve.m_Bezier.d}, length: ${curve.m_Length}");

                            this.getLogger().Info($"Entity is {entity}.");

                            Block block = blocks[i];
                            DynamicBuffer<Cell> cells = cellBuffers[i];
                            ValidArea validArea = validAreas[i];

                            this.getLogger().Info($"Block direction ${block.m_Direction}");
                            this.getLogger().Info($"Block position ${block.m_Position}");
                            this.getLogger().Info($"Valid Area: ${validArea.m_Area}");

                            float dotProduct = BlockUtils.blockCurveDotProduct(block, curve);

                            this.getLogger().Info($"Dot product: ${dotProduct}");
                            this.getLogger().Info($"Zoning mode is ${entityZoningInfo.zoningMode}");

                            if (BlockUtils.isAnyCellOccupied(ref cells, ref block, ref validArea))
                            {
                                // Can't replace occupied cells. So skip.
                                this.getLogger().Info("Cells are occupied. Replacing will not happen.");
                                continue;
                            }

                            BlockUtils.editBlockSizes(dotProduct, entityZoningInfo, validArea, block, entity, entityCommandBuffer);

                            entityCommandBuffer.AddComponent(owner.m_Owner, entityZoningInfo);
                        }
                    }
                }

                entities.Dispose();
                blocks.Dispose();
                validAreas.Dispose();

                stopwatch.Stop();

                this.getLogger().Info($"Job took ${stopwatch.ElapsedMilliseconds}");
            }

        }
        private struct CollectDeletedCurves : IJobChunk
        {
            public ComponentTypeHandle<Owner> ownerTypeHandle;
            public ComponentTypeHandle<Deleted> deletedTypeHandle;

            public ComponentLookup<Curve> curveLookup;

            public ComponentLookup<Deleted> deletedLookup;

            public NativeParallelHashMap<float2, Entity> curvesByStartPoint;

            public NativeParallelHashMap<float2, Entity> curvesByEndPoint;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                this.getLogger().Info("Executing Collect Deleted Curves Job.");
                NativeArray<Owner> owners = chunk.GetNativeArray(ref this.ownerTypeHandle);

                if (chunk.Has(ref this.deletedTypeHandle))
                {
                    for (int i = 0; i < owners.Length; i++)
                    {
                        Owner owner = owners[i];

                        if (curveLookup.HasComponent(owner.m_Owner))
                        {
                            this.getLogger().Info("Adding curve to hash maps.");

                            Curve curve = curveLookup[owner.m_Owner];

                            curvesByStartPoint.Add(curve.m_Bezier.a.xz, owner.m_Owner);
                            curvesByEndPoint.Add(curve.m_Bezier.d.xz, owner.m_Owner);
                        }
                    }
                }
            }
        }

        private struct DisposeHashMaps : IJob
        {
            public NativeParallelHashMap<float2, Entity> toDispose1;
            public NativeParallelHashMap<float2, Entity> toDispose2;
            public void Execute()
            {
                toDispose1.Clear();
                toDispose2.Clear();
                toDispose1.Dispose();
                toDispose2.Dispose();
            }

        }
    }
}