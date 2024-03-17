using System;
using System.Collections.Generic;
using Colossal.UI.Binding;
using Game;
using Game.Prefabs;
using Game.Tools;
using Game.UI;
using Unity.Collections;
using Unity.Entities;
using ZoningToolkit.Components;
using ZoningToolkit.Utilties;

namespace ZoningToolkit.Systems
{
    internal struct UIState
    {
        public bool visible;
        public ZoningMode zoningMode;
        public bool applyToNewRoads;
        public bool toolEnabled;
    }
    partial class ZoningToolkitModUISystem : UISystemBase
    {
        private string kGroup = "zoning_adjuster_ui_namespace";
        private ZoningToolkitModSystem zoningToolkitModSystem;
        private bool activateModUI = false;
        private bool deactivateModUI = false;
        private UIState uiState;
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
            this.uiState = new UIState()
            {
                visible = false,
                zoningMode = ZoningMode.Default,
                applyToNewRoads = false,
            };

            this.toolSystem.EventPrefabChanged = (Action<PrefabBase>)Delegate.Combine(toolSystem.EventPrefabChanged, new Action<PrefabBase>(OnPrefabChanged));
            this.toolSystem.EventToolChanged = (Action<ToolBaseSystem>)Delegate.Combine(toolSystem.EventToolChanged, new Action<ToolBaseSystem>(OnToolChange));

            this.AddUpdateBinding(new GetterValueBinding<string>(this.kGroup, "zoning_mode", () => this.uiState.zoningMode.ToString()));
            this.AddUpdateBinding(new GetterValueBinding<bool>(this.kGroup, "tool_enabled", () => this.uiState.toolEnabled));
            this.AddUpdateBinding(new GetterValueBinding<bool>(this.kGroup, "visible", () => this.uiState.visible));

            this.AddBinding(new TriggerBinding<string>(this.kGroup, "zoning_mode_update", zoningMode => {
                    this.getLogger().Info($"Zoning mode updated to ${zoningMode}.");
                    this.uiState.zoningMode = (ZoningMode) Enum.Parse(typeof(ZoningMode), zoningMode);
                })
            );
            this.AddBinding(new TriggerBinding<bool>(this.kGroup, "tool_enabled", tool_enabled => {
                    this.getLogger().Info($"Tool Enabled updated to ${tool_enabled}.");
                    this.uiState.toolEnabled = tool_enabled;
                })
            );

            
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            this.entitiesToUpdate.Dispose();
        }

        private void toggleTool(bool enableTool)
        {
            if (enableTool)
            {
                this.zoningToolkitModToolSystem.EnableTool();
            } else
            {
                this.zoningToolkitModToolSystem.DisableTool();
            }
        }

        private void OnToolChange(ToolBaseSystem tool)
        {
            this.getLogger().Info("Tool changed!");

            if (tool is NetToolSystem || tool is ZoningToolkitModToolSystem)
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

                if (!this.uiState.visible)
                {
                   uiState.visible = true;
                }
            }

            if (deactivateModUI)
            {
                this.getLogger().Info("Deactivating Mod UI.");

                // Unset trigger
                deactivateModUI = false;

                if (this.uiState.visible)
                {
                    this.uiState.visible = false;
                }
            }

            // Update Tool and System info from UI
            if (this.uiState.zoningMode != this.zoningToolkitModToolSystem.workingState.zoningMode) {
                this.getLogger().Info("Updating Tool System Zoning mode");
                this.zoningToolkitModToolSystem.workingState.zoningMode = this.uiState.zoningMode;
            }

            if (this.uiState.zoningMode != this.zoningToolkitModSystem.zoningMode)
            {
                this.getLogger().Info("Updating Mod System Zoning mode");
                this.zoningToolkitModSystem.zoningMode = this.uiState.zoningMode;
            }

            if (this.uiState.toolEnabled != this.zoningToolkitModToolSystem.toolEnabled)
            {
                this.getLogger().Info("Enabling/Disabling tool");
                toggleTool(this.uiState.toolEnabled);
            }
        }
    }
}
