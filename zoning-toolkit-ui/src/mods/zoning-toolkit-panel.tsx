import engine, { EventHandle } from 'cohtml/cohtml';
import React from 'react';
import Draggable from 'react-draggable';
import * as GameUI from 'cs2/ui';
import { ModUIState, useModUIStore, withStore } from './state';
export interface ZoningToolkitPanelProps {
}

export enum ZoningMode {
    LEFT = "Left",
    RIGHT = "Right",
    NONE = "None",
    DEFAULT = "Default"
}

function getDirectionFromString(value: string): ZoningMode | undefined {
    return Object.values(ZoningMode).find((zoningMode) => zoningMode === value);
}

export class ZoningToolkitPanelInternal extends React.Component<ZoningToolkitPanelProps, ModUIState> {
    subscriptionZoningMode?: () => void;
    subscriptionToolEnabled?: () => void;
    subscriptionVisible?: () => void;

    renderZoningModeButton(zoningMode: ZoningMode, style: Object): JSX.Element {
        return this.renderButton(zoningMode.toString(), style, () => useModUIStore.getState().updateZoningMode(zoningMode.toString()))
    }

    renderButton(buttonLabel: string, buttonStyle: Object, onClick: () => void): JSX.Element {
        return (
            <button
                style={buttonStyle}
                onClick={onClick}
                id={buttonLabel}
            >
                {buttonLabel}
            </button>
        );
    }

    render() {
        const uiVisible = useModUIStore.getState().uiVisible;
        const zoningMode = getDirectionFromString(useModUIStore.getState().zoningMode);
        const isToolEnabled = useModUIStore.getState().isToolEnabled;
        const photomodeActive = useModUIStore.getState().photomodeActive;

        // Define the styles
        const windowStyle: React.CSSProperties = {
            position: "absolute",
            top: 100,
            right: 100,
            color: "white",
            backgroundColor: "rgba(38, 56, 65, 1)", // Light gray with 100% opacity
            borderRadius: "10px", // Rounded edges
            border: "none", // Removing any border or outline
            padding: '20px',
            width: 'auto',
            margin: '15px auto',
            textAlign: 'center',
            transition: 'box-shadow 0.3s ease-in-out',
            pointerEvents: 'auto',
            display: uiVisible && !photomodeActive === true ? 'block' : 'none'
        };

        const buttonStyle = {
            margin: '5px',
            padding: '10px 20px',
        };

        const leftButtonStyle = {
            ...buttonStyle,
            background: zoningMode === ZoningMode.LEFT ? 'green' : 'gray'
        }

        const rightButtonStyle = {
            ...buttonStyle,
            background: zoningMode === ZoningMode.RIGHT ? 'green' : 'gray'
        }

        const defaultButtonStyle = {
            ...buttonStyle,
            background: zoningMode === ZoningMode.DEFAULT ? 'green' : 'gray',
        }

        const noneButtonStyle = {
            ...buttonStyle,
            background: zoningMode === ZoningMode.NONE ? 'green' : 'gray',
        }

        const updateZoningButtonStyle = {
            ...buttonStyle,
            background: isToolEnabled === true ? 'green' : 'gray',
        }

        const closeButtonStyle = {
            position: 'absolute',
            top: '10px',
            right: '10px',
            cursor: 'pointer'
        }

        const columnStyle = {
            display: 'flex',
            flexDirection: 'row'
        }

        const zoningOptionsStyle = {
            border: "solid"
        }

        // Apply the styles to the elements
        return (
                            
            <Draggable grid={[50, 50]}>
                <div
                    style={windowStyle}
                    id="inner-div"
                >
                    <div id="zoning-mode-options">
                        Zoning Mode Options
                        <div id="button-list" style={zoningOptionsStyle}>
                            {this.renderZoningModeButton(ZoningMode.LEFT, leftButtonStyle)}
                            {this.renderZoningModeButton(ZoningMode.RIGHT, rightButtonStyle)}
                            {this.renderZoningModeButton(ZoningMode.DEFAULT, defaultButtonStyle)}
                            {this.renderZoningModeButton(ZoningMode.NONE, noneButtonStyle)}
                        </div>
                    </div>
                    <div id="button-list">

                        {this.renderButton("Zoning Tool", updateZoningButtonStyle, () => useModUIStore.getState().updateIsToolEnabled(!isToolEnabled))}
                    </div>
                </div>
            </Draggable>
        );
    }
}

export const ZoningToolkitPanel = withStore(ZoningToolkitPanelInternal)