using System;
using System.Runtime.CompilerServices;
using Colossal.IO.AssetDatabase.Internal;
using Game.Common;
using Game.Net;
using Game.UI;
using Unity.Collections;
using Unity.Entities;
using ZoningToolkit.Systems;

namespace ZoningToolkit.Utilties
{
    public static class EntityUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void listEntityComponents(this ZoningToolkitModSystem gameSystemBase, Entity entity)
        {
            gameSystemBase.EntityManager.GetComponentTypes(entity).ForEach(compoenentType => {
                Console.WriteLine($"Entity has component ${compoenentType.GetManagedType()}");
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void listEntityComponents(this UISystemBase uiSystemBase, Entity entity)
        {
            uiSystemBase.EntityManager.GetComponentTypes(entity).ForEach(compoenentType => {
                Console.WriteLine($"Entity has component ${compoenentType.GetManagedType()}");
            });
        }

        /*        public static void listCurveComponentData(this ZoningToolkitModSystem system, Entity entity)
                {
                    if (system.ownerComponentLookup.HasComponent(entity))
                    {
                        Owner owner = system.ownerComponentLookup[entity];
                        if (system.curveComponentLookup.HasComponent(owner.m_Owner))
                        {
                            Curve curve = system.curveComponentLookup[owner.m_Owner];
                            Console.WriteLine("****** Printing curve data *****");
                            Console.WriteLine($"Curve a: ${curve.m_Bezier.a}, b: ${curve.m_Bezier.b}, c: ${curve.m_Bezier.c}, d: ${curve.m_Bezier.d}, length: ${curve.m_Length}");
                        }
                    }
                }*/

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void listEntityComponentsInQuery(this ZoningToolkitModSystem system, EntityQuery entityQuery)
        {
            NativeArray<Entity> entities = entityQuery.ToEntityArray(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                system.getLogger().Info("****** Listing entity componenets ******");
                Entity entity = entities[i];
                system.listEntityComponents(entity);
                system.getLogger().Info("***** Printing owner info ******");
                if (system.ownerComponentLookup.HasComponent(entity))
                {
                    Owner owner = system.ownerComponentLookup[entity];
                    system.listEntityComponents(owner.m_Owner);
                }
            }
        }
    }
}