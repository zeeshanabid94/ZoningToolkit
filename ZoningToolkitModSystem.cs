using System.Diagnostics;
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
using ZoningToolkit.Utilties;

namespace ZoningToolkit.Systems
{
    // [UpdateAfter(typeof(BlockSystem))]
    public partial class ZoningToolkitModSystem : GameSystemBase
    {

        private EntityQuery updatedEntityQuery;

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
        private ComponentLookup<Created> createdLookup;
        private ComponentLookup<Updated> updatedLookup;
        private ComponentLookup<Applied> appliedLookup;
        private ModificationBarrier4B modificationBarrier4B;
        private NetToolSystem netToolSystem;
        private ToolRaycastSystem raycastSystem;
        public ZoningMode zoningMode;
        public bool upgradeEnabled;

        public NativeQueue<Entity>.ReadOnly entitiesToUpdate
        {
            set { entitiesToUpdate = value; }
            get { return entitiesToUpdate; }
        }

        protected override void OnCreate()
        {
            this.getLogger().Info("Creating ZoningToolkitMod GameSystem.");
            base.OnCreate();

            this.updatedEntityQuery = this.GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[] {
                    ComponentType.ReadWrite<Block>(),
                    ComponentType.ReadWrite<Owner>(),
                    ComponentType.ReadOnly<Cell>(),
                    ComponentType.ReadOnly<ValidArea>()
                },
                Any = new ComponentType[] {
                    ComponentType.ReadOnly<Updated>(),
                    ComponentType.ReadOnly<Applied>(),
                    ComponentType.ReadOnly<Created>(),
                    ComponentType.ReadOnly<Deleted>()
                }
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
            this.createdLookup = this.GetComponentLookup<Created>();
            this.updatedLookup = this.GetComponentLookup<Updated>();
            this.appliedLookup = this.GetComponentLookup<Applied>();

            // other systems to use
            this.modificationBarrier4B = World.GetOrCreateSystemManaged<ModificationBarrier4B>();
            this.netToolSystem = World.GetOrCreateSystemManaged<NetToolSystem>();
            this.raycastSystem = World.GetOrCreateSystemManaged<ToolRaycastSystem>();

            setZoningMode("Left");
            setUpgradeEnabled(false);
            this.RequireForUpdate(this.updatedEntityQuery);
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
            this.createdLookup.Update(ref CheckedStateRef);
            this.updatedLookup.Update(ref CheckedStateRef);
            this.appliedLookup.Update(ref CheckedStateRef);

            this.getLogger().Info("Creating Entity Command Buffer");

            EntityCommandBuffer entityCommandBuffer = this.modificationBarrier4B.CreateCommandBuffer();

            this.getLogger().Info("*************Printing updated blocks.*************");
            this.listEntityComponentsInQuery(this.updatedEntityQuery);

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
            }.Schedule(this.updatedEntityQuery, outJobHandle);
            outJobHandle = JobHandle.CombineDependencies(collectDeletedEntities, outJobHandle);

            if (netToolSystem.actualMode != NetToolSystem.Mode.Replace)
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
                    updateLookup = this.updatedLookup,
                    createdLookup = this.createdLookup,
                    appliedLookup = this.appliedLookup,
                    entitiesByStartPoint = deletedEntitiesByStartPoint,
                    entitiesByEndPoint = deletedEntitiesByEndPoint,
                    upgradedEntity = null
                }.Schedule(this.updatedEntityQuery, outJobHandle);
                outJobHandle = JobHandle.CombineDependencies(outJobHandle, jobHandle);
            }
            else
            {
/*                this.getLogger().Info("Updating zoning for existing roads.");
                raycastSystem.GetRaycastResult(out RaycastResult result);

                if (result.m_Owner != null)
                {
                    this.getLogger().Info("********* Raycase owner components **********");
                    this.listEntityComponents(result.m_Owner);
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
                        updateLookup = this.updatedLookup,
                        createdLookup = this.createdLookup,
                        appliedLookup = this.appliedLookup,
                        entitiesByStartPoint = deletedEntitiesByStartPoint,
                        entitiesByEndPoint = deletedEntitiesByEndPoint,
                        upgradedEntity = result.m_Owner,
                        upgradeEnabled = upgradeEnabled,
                        entitiesToUpdate = entitiesToUpdate
                    }.Schedule(this.updatedEntityQuery, outJobHandle);
                    outJobHandle = JobHandle.CombineDependencies(outJobHandle, jobHandle);
                }*/
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

        public void setUpgradeEnabled(bool enabled)
        {
            this.upgradeEnabled = enabled;
        }

        public Vector2 GetTangent(Bezier4x2 curve, float t)
        {
            // Calculate the derivative of the Bezier curve
            float2 derivative = 3 * math.pow(1 - t, 2) * (curve.b - curve.a) +
                                    6 * (1 - t) * t * (curve.c - curve.b) +
                                    3 * math.pow(t, 2) * (curve.d - curve.c);
            return new Vector2(derivative.x, derivative.y);
        }

        // [BurstCompile]
        private struct UpdateZoneData : IJobChunk
        {
            private readonly static ILog logger = LogManager.GetLogger($"{nameof(ZoningToolkit)}.UpdateZoneData").SetShowsErrorsInUI(false);
            [ReadOnly]
            public ZoningMode zoningMode;
            [ReadOnly]
            public Entity? upgradedEntity;
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

            public ComponentLookup<Updated> updateLookup;
            public ComponentLookup<Created> createdLookup;
            public ComponentLookup<Applied> appliedLookup;
            public EntityCommandBuffer entityCommandBuffer;
            public NativeParallelHashMap<float2, Entity> entitiesByStartPoint;
            public NativeParallelHashMap<float2, Entity> entitiesByEndPoint;
            public bool upgradeEnabled;
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                logger.Info("Executing Zone Adjustment Job.");
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
                    logger.Info("Processing entity.");
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

                                if (isStartPresent && isEndPresent)
                                {
                                    logger.Info("Start and end deleted entity both present.");
                                    Curve startCurve = curveComponentLookup[startDeletedEntity];
                                    Curve endCurve = curveComponentLookup[endDeletedEntity];

                                    if (startCurve.m_Bezier.d.x == endCurve.m_Bezier.a.x && startCurve.m_Bezier.d.z == endCurve.m_Bezier.a.z)
                                    {
                                        logger.Info("New curve matches deleted entity by start point & end point.");
                                        // Deleted curve form the current complete curve.
                                        ZoningInfo startZoningInfo = zoningInfoComponentLookup[startDeletedEntity];
                                        ZoningInfo endZoningInfo = zoningInfoComponentLookup[endDeletedEntity];

                                        if (startZoningInfo.Equals(endZoningInfo))
                                        {
                                            // If zoning is same, choose that.
                                            logger.Info("Start and end curve zoning match.");
                                            entityZoningInfo = startZoningInfo;
                                        }
                                        else
                                        {
                                            logger.Info("Start and end curve zoning don't match. Setting defaut zone.");
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
                                    logger.Info("New curve matches deleted entity by end point.");
                                    if (zoningInfoComponentLookup.HasComponent(endDeletedEntity))
                                    {
                                        entityZoningInfo = zoningInfoComponentLookup[endDeletedEntity];
                                    }
                                }
                                else if (isStartPresent)
                                {
                                    logger.Info("New curve matches deleted entity by start point.");
                                    if (zoningInfoComponentLookup.HasComponent(startDeletedEntity))
                                    {
                                        entityZoningInfo = zoningInfoComponentLookup[startDeletedEntity];
                                    }
                                }

                                if (upgradeEnabled == false || (upgradedEntity != null && upgradedEntity != owner.m_Owner)) {
                                    this.getLogger().Info("Upgraded entity doesn't matches owner entity. Setting zoning info from owner...");
                                    if (zoningInfoComponentLookup.HasComponent(owner.m_Owner)) {
                                        entityZoningInfo = this.zoningInfoComponentLookup[owner.m_Owner];
                                    }
                                }

                                if (upgradeEnabled == true)
                                {
                                    logger.Info("Entity found in entities to update.");
                                }
                                else
                                {
                                    if (zoningInfoComponentLookup.HasComponent(owner.m_Owner))
                                    {
                                        entityZoningInfo = this.zoningInfoComponentLookup[owner.m_Owner];
                                    }
                                }
                            }

                            logger.Info($"Processing Curve a: ${curve.m_Bezier.a}, b: ${curve.m_Bezier.b}, c: ${curve.m_Bezier.c}, d: ${curve.m_Bezier.d}, length: ${curve.m_Length}");

                            logger.Info($"Entity is {entity}.");

                            Block block = blocks[i];
                            DynamicBuffer<Cell> cells = cellBuffers[i];
                            ValidArea validArea = validAreas[i];

                            logger.Info($"Block direction ${block.m_Direction}");
                            logger.Info($"Block position ${block.m_Position}");
                            logger.Info($"Valid Area: ${validArea.m_Area}");

                            MathUtils.Distance(curve.m_Bezier.xz, block.m_Position.xz, out float closest_point_t);

                            Vector2 tangentVectorAtCurve = GetTangent(curve.m_Bezier.xz, closest_point_t);
                            Vector2 perpendicularToTangent = new Vector2(tangentVectorAtCurve.y, -tangentVectorAtCurve.x);

                            float dotProduct = Vector2.Dot(perpendicularToTangent, block.m_Direction);

                            logger.Info($"Dot product: ${dotProduct}");
                            logger.Info($"Zoning mode is ${entityZoningInfo.zoningMode}");


                            if (isAnyCellOccupied(ref cells, ref block, ref validArea) && upgradedEntity == null)
                            {
                                // Can't replace occupied cells. So skip.
                                continue;
                            }

                            editBlockSizes(dotProduct, entityZoningInfo, validArea, block, entity);

                            entityCommandBuffer.AddComponent(owner.m_Owner, entityZoningInfo);
                        }
                    }
                }

                entities.Dispose();
                blocks.Dispose();
                validAreas.Dispose();

                stopwatch.Stop();

                logger.Info($"Job took ${stopwatch.ElapsedMilliseconds}");
            }

            private void editBlockSizes(float dotProduct, ZoningInfo newZoningInfo, ValidArea validArea, Block block, Entity entity)
            {
                if (dotProduct > 0)
                {
                    if (newZoningInfo.zoningMode == ZoningMode.Right || newZoningInfo.zoningMode == ZoningMode.None)
                    {
                        // entityCommandBuffer.AddComponent(entity, new Deleted());
                        // entityCommandBuffer.RemoveComponent<Updated>(entity);
                        // entityCommandBuffer.RemoveComponent<Created>(entity);

                        validArea.m_Area.w = 0;

                        entityCommandBuffer.SetComponent(entity, validArea);

                        block.m_Size.y = 0;

                        entityCommandBuffer.SetComponent(entity, block);
                    }
                    else
                    {
                        validArea.m_Area.w = 6;

                        entityCommandBuffer.SetComponent(entity, validArea);

                        block.m_Size.y = 6;

                        entityCommandBuffer.SetComponent(entity, block);
                    }
                }
                else
                {
                    if (newZoningInfo.zoningMode == ZoningMode.Left || newZoningInfo.zoningMode == ZoningMode.None)
                    {
                        // entityCommandBuffer.AddComponent(entity, new Deleted());
                        // entityCommandBuffer.RemoveComponent<Updated>(entity);
                        // entityCommandBuffer.RemoveComponent<Created>(entity);

                        validArea.m_Area.w = 0;

                        entityCommandBuffer.SetComponent(entity, validArea);

                        block.m_Size.y = 0;

                        entityCommandBuffer.SetComponent(entity, block);
                    }
                    else
                    {
                        validArea.m_Area.w = 6;

                        entityCommandBuffer.SetComponent(entity, validArea);

                        block.m_Size.y = 6;

                        entityCommandBuffer.SetComponent(entity, block);
                    }
                }

            }

            private bool isAnyCellOccupied(ref DynamicBuffer<Cell> cells, ref Block block, ref ValidArea validArea)
            {
                logger.Info($"Block size x: ${block.m_Size.x}, y: ${block.m_Size.y}");
                logger.Info($"Valid area x: ${validArea.m_Area.x}, y: ${validArea.m_Area.y}, z: ${validArea.m_Area.z}, w: ${validArea.m_Area.w}");

                if (validArea.m_Area.y * validArea.m_Area.w == 0)
                {
                    return false;
                }

                for (int z = validArea.m_Area.z; z < validArea.m_Area.w; z++)
                {
                    for (int x = validArea.m_Area.x; x < validArea.m_Area.y; x++)
                    {
                        logger.Info($"z: ${z}, x: ${x}");

                        int index = z * block.m_Size.x + x;
                        Cell cell = cells[index];

                        if ((cell.m_State & CellFlags.Occupied) != 0)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            private Vector2 GetTangent(Bezier4x2 curve, float t)
            {
                // Calculate the derivative of the Bezier curve
                float2 derivative = 3 * math.pow(1 - t, 2) * (curve.b - curve.a) +
                                        6 * (1 - t) * t * (curve.c - curve.b) +
                                        3 * math.pow(t, 2) * (curve.d - curve.c);
                return new Vector2(derivative.x, derivative.y);
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
                toDispose1.Dispose();
                toDispose2.Dispose();
            }

        }
    }
}