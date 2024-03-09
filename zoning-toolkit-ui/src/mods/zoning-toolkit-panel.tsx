import React from 'react';
import Draggable from 'react-draggable';

export interface ZoningToolkitPanelProps {
    zoningMode: ZoningMode,
    isFocused: Boolean,
    isVisible: Boolean,
    isEnabled: Boolean,
    isUpgradeEnabled: Boolean
}

export enum ZoningMode {
    LEFT = "Left",
    RIGHT = "Right",
    NONE = "None",
    DEFAULT = "Default"
}

export class ZoningToolkitPanel extends React.Component<{}, ZoningToolkitPanelProps> {
    constructor(props: ZoningToolkitPanelProps) {
        super(props);
        this.state = {
            isEnabled: false,
            isFocused: false,
            isUpgradeEnabled: false,
            isVisible: true,
            zoningMode: ZoningMode.DEFAULT
        };
    }

    selectZoningMode = (zoningMode: ZoningMode) => {
        console.log(`Button clicked. Zoning mode ${zoningMode}`);
        // sendDataToCSharp('zoning_adjuster_ui_namespace', 'zoning_mode_update', zoningMode);
    }

    renderZoningModeButton(zoningMode: ZoningMode, style: Object): JSX.Element {
        return this.renderButton(zoningMode.toString(), style, () => this.selectZoningMode(zoningMode))
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
            display: this.state.isVisible === true ? 'block' : 'none'
        };

        const buttonStyle = {
            margin: '5px',
            padding: '10px 20px',
        };

        const leftButtonStyle = {
            ...buttonStyle,
            background: this.state.zoningMode === ZoningMode.LEFT ? 'green' : 'gray'
        }

        const rightButtonStyle = {
            ...buttonStyle,
            background: this.state.zoningMode === ZoningMode.RIGHT ? 'green' : 'gray'
        }

        const defaultButtonStyle = {
            ...buttonStyle,
            background: this.state.zoningMode === ZoningMode.DEFAULT ? 'green' : 'gray',
        }

        const noneButtonStyle = {
            ...buttonStyle,
            background: this.state.zoningMode === ZoningMode.NONE ? 'green' : 'gray',
        }

        const enabledButtonStyle = {
            ...buttonStyle,
            background: this.state.isEnabled === true ? 'green' : 'gray',
        }


        const upgradeEnabledStyle = {
            ...buttonStyle,
            background: this.state.isUpgradeEnabled === true ? 'green' : 'gray',
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

        const { isVisible } = this.state;

        // Apply the styles to the elements
        return (
            <Draggable grid={[50, 50]}>
                <div
                    style={windowStyle}
                    id="inner-div"
                >
                    <div id="button-list">
                    {this.renderZoningModeButton(ZoningMode.LEFT, leftButtonStyle)}
                    {this.renderZoningModeButton(ZoningMode.RIGHT, rightButtonStyle)}
                    {this.renderZoningModeButton(ZoningMode.DEFAULT, defaultButtonStyle)}
                    {this.renderZoningModeButton(ZoningMode.NONE, noneButtonStyle)}
                    </div>
                </div>
            </Draggable>
        );
    }
}