import engine, { EventHandle } from 'cohtml/cohtml';
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
function updateEventFromCSharp<T>(namespace: string, event: string, callback: (input: T) => void): () => void {
    console.log("Subscribing to update events from game. Event " + event);
    const updateEvent = namespace + "." + event + ".update"
    const subscribeEvent = namespace + "." + event + ".subscribe"
    const unsubscribeEvent = namespace + "." + event + ".unsubscribe"

    let sub: EventHandle = engine.on(updateEvent, callback)
    engine.trigger(subscribeEvent)
    return () => {
        engine.trigger(unsubscribeEvent)
        sub.clear();
    };
}

function sendDataToCSharp<T>(namespace: string, event: String, newValue: T) {
    console.log(`Event triggered. Sending new value ${newValue}`);
    engine.trigger(namespace + "." + event, newValue);
}

export class ZoningToolkitPanel extends React.Component<{}, ZoningToolkitPanelProps> {
    state: ZoningToolkitPanelProps;
    subscriptionZoningMode?: () => void;
    subscriptionUpgradeEnabled?: () => void;
    subscriptionVisible?: () => void;

    constructor(props: ZoningToolkitPanelProps) {
        super(props);
        this.state = {
            isEnabled: false,
            isFocused: false,
            isUpgradeEnabled: false,
            isVisible: false,
            zoningMode: ZoningMode.DEFAULT
        };
    }

    componentDidMount() {
        this.subscriptionZoningMode = updateEventFromCSharp<string>('zoning_adjuster_ui_namespace', 'zoning_mode', (zoningMode) => {
            console.log(`Zoning mode fetched ${zoningMode}`);
            this.setState({ zoningMode: ZoningMode[zoningMode as keyof typeof ZoningMode] })
        })
        this.subscriptionUpgradeEnabled = updateEventFromCSharp<boolean>('zoning_adjuster_ui_namespace', 'upgrade_enabled', (upgradeEnabled) => {
            console.log(`Upgrade Enabled Toggled ${upgradeEnabled}`);
            this.setState({ isUpgradeEnabled: upgradeEnabled })
        })
        this.subscriptionVisible = updateEventFromCSharp<boolean>('zoning_adjuster_ui_namespace', 'visible', (visible) => {
            console.log(`UI visibility changed to ${visible}`);
            this.setState({ isVisible: visible })
        })
        this.setState({ isVisible: true })
    }

    componentWillUnmount() {
        this.subscriptionZoningMode?.();
        this.subscriptionUpgradeEnabled?.();
        this.subscriptionVisible?.();
    }

    selectZoningMode = (zoningMode: ZoningMode) => {
        console.log(`Button clicked. Zoning mode ${zoningMode}`);
        sendDataToCSharp('zoning_adjuster_ui_namespace', 'zoning_mode_update', zoningMode.toString());
    }

    selectUpdateZoning = () => {
        console.log(`Button clicked. Update zoning toggled. New value sending ${!this.state.isUpgradeEnabled}`);
        sendDataToCSharp('zoning_adjuster_ui_namespace', 'upgrade_enabled', !this.state.isUpgradeEnabled);
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

        const updateZoningButtonStyle = {
            ...buttonStyle,
            background: this.state.isUpgradeEnabled === true ? 'green' : 'gray',
        }

        const enabledButtonStyle = {
            ...buttonStyle,
            background: this.state.isEnabled === true ? 'green' : 'gray',
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
                    {this.renderButton("Update Zoning", updateZoningButtonStyle, this.selectUpdateZoning)}
                    </div>
                </div>
            </Draggable>
        );
    }
}