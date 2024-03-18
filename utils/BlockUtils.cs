using Colossal.Logging;
using Colossal.Mathematics;
using Game.Common;
using Game.Net;
using Game.Zones;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using UnityEngine;
using ZoningToolkit.Components;
using ZoningToolkit.Utilties;
using static Colossal.IO.AssetDatabase.AtlasFrame;
using static Game.UI.NameSystem;

namespace ZoningToolkit.utils
{
    internal class BlockUtils
    {
        private static ILog logger = LogManager.GetLogger($"{nameof(ZoningToolkit)}.BlockUtils").SetShowsErrorsInUI(false);
        public static float blockCurveDotProduct(Block block, Curve curve)
        {
            logger.getLogger().Info($"Block direction ${block.m_Direction}");
            logger.getLogger().Info($"Block position ${block.m_Position}");

            MathUtils.Distance(curve.m_Bezier.xz, block.m_Position.xz, out float closest_point_t);

            Vector2 tangentVectorAtCurve = GetTangent(curve.m_Bezier.xz, closest_point_t);
            Vector2 perpendicularToTangent = new Vector2(tangentVectorAtCurve.y, -tangentVectorAtCurve.x);

            float dotProduct = Vector2.Dot(perpendicularToTangent, block.m_Direction);

            logger.getLogger().Info($"Dot product: ${dotProduct}");

            return dotProduct;
        }

        public static void deleteBlock(float dotProduct, ZoningInfo newZoningInfo, Entity blockEntity, EntityCommandBuffer entityCommandBuffer)
        {
            if (dotProduct > 0)
            {
                if (newZoningInfo.zoningMode == ZoningMode.Right || newZoningInfo.zoningMode == ZoningMode.None)
                {
                    entityCommandBuffer.AddComponent(blockEntity, new Deleted());
                }
            }
            else
            {
                if (newZoningInfo.zoningMode == ZoningMode.Left || newZoningInfo.zoningMode == ZoningMode.None)
                {
                    entityCommandBuffer.AddComponent(blockEntity, new Deleted());
                }
            }
        }

        public static void editBlockSizes(float dotProduct, ZoningInfo newZoningInfo, ValidArea validArea, Block block, Entity entity, EntityCommandBuffer entityCommandBuffer)
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

        public static bool isAnyCellOccupied(ref DynamicBuffer<Cell> cells, ref Block block, ref ValidArea validArea)
        {
            logger.getLogger().Info($"Block size x: ${block.m_Size.x}, y: ${block.m_Size.y}");
            logger.getLogger().Info($"Valid area x: ${validArea.m_Area.x}, y: ${validArea.m_Area.y}, z: ${validArea.m_Area.z}, w: ${validArea.m_Area.w}");

            if (validArea.m_Area.y * validArea.m_Area.w == 0)
            {
                return false;
            }

            for (int z = validArea.m_Area.z; z < validArea.m_Area.w; z++)
            {
                for (int x = validArea.m_Area.x; x < validArea.m_Area.y; x++)
                {
                    logger.getLogger().Info($"z: ${z}, x: ${x}");

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

        public static Vector2 GetTangent(Bezier4x2 curve, float t)
        {
            // Calculate the derivative of the Bezier curve
            float2 derivative = 3 * math.pow(1 - t, 2) * (curve.b - curve.a) +
                                    6 * (1 - t) * t * (curve.c - curve.b) +
                                    3 * math.pow(t, 2) * (curve.d - curve.c);
            return new Vector2(derivative.x, derivative.y);
        }
    }
}
