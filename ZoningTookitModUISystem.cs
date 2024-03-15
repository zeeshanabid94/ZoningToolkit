using System;
using System.Collections.Generic;
using Colossal.UI.Binding;
using Game;
using Game.Prefabs;
using Game.Tools;
using Game.UI;
using Unity.Collections;
using Unity.Entities;
using ZoningToolkit.Utilties;

namespace ZoningToolkit.Systems
{
    partial class ZoningToolkitModUISystem : UISystemBase
    {
        private string kGroup = "zoning_adjuster_ui_namespace";
        private ZoningToolkitModSystem zoningToolkitModSystem;
        private bool activateModUI = false;
        private bool deactivateModUI = false;
        private bool modUIVisible = false;
        private ToolSystem toolSystem;
        private NativeQueue<Entity> entitiesToUpdate;
        private ZoningToolkitModToolSystem zoningToolkitModToolSystem;

        public override GameMode gameMode => GameMode.Game;

        protected override void OnCreate()
        {
            base.OnCreate();

            this.zoningToolkitModSystem = World.GetExistingSystemManaged<ZoningToolkitModSystem>();
            this.toolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            this.zoningToolkitModToolSystem = World.GetOrCreateSystemManaged<ZoningToolkitModToolSystem>();

            this.toolSystem.EventPrefabChanged = (Action<PrefabBase>)Delegate.Combine(toolSystem.EventPrefabChanged, new Action<PrefabBase>(OnPrefabChanged));
            this.toolSystem.EventToolChanged = (Action<ToolBaseSystem>)Delegate.Combine(toolSystem.EventToolChanged, new Action<ToolBaseSystem>(OnToolChange));

            this.AddUpdateBinding(new GetterValueBinding<string>(this.kGroup, "zoning_mode", () => zoningToolkitModSystem.zoningMode.ToString()));
            this.AddUpdateBinding(new GetterValueBinding<bool>(this.kGroup, "upgrade_enabled", () => zoningToolkitModSystem.upgradeEnabled));
            this.AddUpdateBinding(new GetterValueBinding<bool>(this.kGroup, "visible", () => modUIVisible));

            this.AddBinding(new TriggerBinding<string>(this.kGroup, "zoning_mode_update", zoningMode => {
                    this.getLogger().Info($"Zoning mode updated to ${zoningMode}.");
                    this.zoningToolkitModSystem.setZoningMode(zoningMode);
                })
            );
            this.AddBinding(new TriggerBinding<bool>(this.kGroup, "upgrade_enabled", upgrade_enabled => {
                    this.getLogger().Info($"Upgrade Enabled updated to ${upgrade_enabled}.");
                    this.selectUpdateZoning();
                })
            );

            
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            this.entitiesToUpdate.Dispose();
        }

        private void selectUpdateZoning()
        {
            if (this.zoningToolkitModToolSystem.toolEnabled)
            {
                this.zoningToolkitModToolSystem.DisableTool();
            } else
            {
                this.zoningToolkitModToolSystem.EnableTool();
            }
        }

        private void OnToolChange(ToolBaseSystem tool)
        {
            this.getLogger().Info("Tool changed!");

            if (tool is NetToolSystem)
            {
                if (tool.GetPrefab() is RoadPrefab)
                {
                    this.getLogger().Info("Prefab is RoadPrefab!");
                    RoadPrefab roadPrefab = (RoadPrefab)tool.GetPrefab();
                    this.getLogger().Info($"Road prefab information.");
                    this.getLogger().Info($"Road Type {roadPrefab.m_RoadType}.");
                    this.getLogger().Info($"Road Zone Block {roadPrefab.m_ZoneBlock}.");

                    if (roadPrefab.m_ZoneBlock != null)
                    {
                        activateModUI = true;
                    }
                    else
                    {
                        deactivateModUI = true;
                    }
                }
                else
                {
                    deactivateModUI = true;
                }
            }
            else
            {
                deactivateModUI = true;
            }
        }

        private void OnPrefabChanged(PrefabBase prefabBase)
        {
            this.getLogger().Info("Prefab changed!");

            if (prefabBase is RoadPrefab)
            {
                this.getLogger().Info("Prefab is RoadPrefab!");
                RoadPrefab roadPrefab = (RoadPrefab)prefabBase;
                this.getLogger().Info($"Road prefab information.");
                this.getLogger().Info($"Road Type {roadPrefab.m_RoadType}.");
                this.getLogger().Info($"Road Zone Block {roadPrefab.m_ZoneBlock}.");

                if (roadPrefab.m_ZoneBlock != null)
                {
                    activateModUI = true;
                }
                else
                {
                    deactivateModUI = true;
                }
            }
            else
            {
                this.getLogger().Info("Prefab is not RoadPrefab!");
                deactivateModUI = true;
            }
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (activateModUI)
            {
                this.getLogger().Info("Activating Mod UI.");

                // unset the trigger
                activateModUI = false;

                if (!modUIVisible)
                {
                   modUIVisible = true;
                }
            }

            if (deactivateModUI)
            {
                this.getLogger().Info("Deactivating Mod UI.");

                // Unset trigger
                deactivateModUI = false;

                if (modUIVisible)
                {
                    modUIVisible= false;
                }
            }
        }
    }
}
