import React, { CSSProperties } from 'react';
import Draggable from 'react-draggable';
import { Panel, PanelSection, PanelSectionRow } from 'cs2/ui';

import updateToolIcon from "../../assets/icons/replace_tool_icon.svg";
import { useModUIStore, withStore } from './state';
import panelStyles from "./zoning-toolkit-panel.module.scss";
import VanillaBindings from './vanilla-bindings';
import { getModeFromString, zoneModeIconMap, ZoningMode } from './zoning-toolkit-utils';

interface ZoningModeButtonConfig {
    icon: string;
    mode: ZoningMode;
    tooltip?: string;
}

const zoningModeButtonConfigs: ZoningModeButtonConfig[] = [
    { icon: zoneModeIconMap.Default, mode: ZoningMode.DEFAULT, tooltip: "Default (both)" },
    { icon: zoneModeIconMap.Left, mode: ZoningMode.LEFT, tooltip: "Left" },
    { icon: zoneModeIconMap.Right, mode: ZoningMode.RIGHT, tooltip: "Right" },
    { icon: zoneModeIconMap.None, mode: ZoningMode.NONE, tooltip: "None" },
];

const { ToolButton } = VanillaBindings.components;

export class ZoningToolkitPanelInternal extends React.Component {
    subscriptionZoningMode?: () => void;
    subscriptionToolEnabled?: () => void;
    subscriptionVisible?: () => void;

    handleZoneModeSelect(zoningMode: ZoningMode) {
        useModUIStore.getState().updateZoningMode(zoningMode.toString());
    }

    handleZoneToolSelect(enabled: boolean) {
        useModUIStore.getState().updateIsToolEnabled(enabled);
    }

    render() {
        const currentZoningMode = getModeFromString(useModUIStore.getState().zoningMode);
        const isToolEnabled = useModUIStore.getState().isToolEnabled;

        const uiVisible = useModUIStore.getState().uiVisible;
        const photomodeActive = useModUIStore.getState().photomodeActive;

        const panelStyle: CSSProperties = {
			// Toolkit panel should be hidden in photo mode
            display: !uiVisible || photomodeActive ? "none" : undefined,
        };

        return (
            <Draggable bounds="parent" grid={[10, 10]}>
                <Panel
                    className={panelStyles.panel}
                    header="Zoning Toolkit"
                    style={panelStyle}
                >
                    <PanelSection>
                        <PanelSectionRow
                            left="Tool Mode"
                            right={(
                                <div className={panelStyles.panelToolModeRow}>
                                    {zoningModeButtonConfigs.map((config) => (
                                        <ToolButton
                                            key={config.mode}
                                            focusKey={VanillaBindings.common.focus.disabled}
                                            selected={currentZoningMode === config.mode}
                                            src={config.icon}
                                            tooltip={config.tooltip}
                                            onSelect={() => this.handleZoneModeSelect(config.mode)}
                                        />
                                    ))}
                                </div>
                            )}
                        />
                        <PanelSectionRow
                            left="Update Tool"
                            right={(
                                <ToolButton
                                    focusKey={VanillaBindings.common.focus.disabled}
                                    selected={isToolEnabled}
                                    src={updateToolIcon}
                                    tooltip="Toggle zoning update tool (for existing roads). Note that roads with zoned buildings will skip rezoning (for safety)."
                                    onSelect={() => this.handleZoneToolSelect(!isToolEnabled)}
                                />
                            )}
                        />
                    </PanelSection>
                </Panel>
            </Draggable>
        );
    }
}

export const ZoningToolkitPanel = withStore(ZoningToolkitPanelInternal)
