import engine, { EventHandle } from 'cohtml/cohtml';
import React from 'react';
import Draggable from 'react-draggable';
import * as GameUI from 'cs2/ui';
import { useModUIStore } from './state';
export interface ZoningToolkitPanelProps {
    zoningMode: ZoningMode,
    isFocused: Boolean,
    isVisible: Boolean,
    isEnabled: Boolean,
    isToolEnabled: Boolean
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
    subscriptionToolEnabled?: () => void;
    subscriptionVisible?: () => void;

    constructor(props: ZoningToolkitPanelProps) {
        super(props);
        this.state = {
            isEnabled: false,
            isFocused: false,
            isToolEnabled: false,
            isVisible: false,
            zoningMode: ZoningMode.DEFAULT
        };
    }

    componentDidMount() {
        this.subscriptionZoningMode = updateEventFromCSharp<string>('zoning_adjuster_ui_namespace', 'zoning_mode', (zoningMode) => {
            console.log(`Zoning mode fetched ${zoningMode}`);
            // TODO: Figure out a way to map string to enum more easily
            let zoningModeValue: ZoningMode
            switch (zoningMode) {
                case "Left":
                    zoningModeValue = ZoningMode.LEFT
                    break
                case "Right":
                    zoningModeValue = ZoningMode.RIGHT
                    break
                case "None":
                    zoningModeValue = ZoningMode.NONE
                    break
                case "Default":
                    zoningModeValue = ZoningMode.DEFAULT
                    break
                default:
                    zoningModeValue = ZoningMode.DEFAULT
                    break
            }

            console.log(`Setting zoning mode ${zoningModeValue}`);
            this.setState({ zoningMode: zoningModeValue })
        })
        this.subscriptionToolEnabled = updateEventFromCSharp<boolean>('zoning_adjuster_ui_namespace', 'tool_enabled', (toolEnabled) => {
            console.log(`Tool Enabled Toggled ${toolEnabled}`);
            this.setState({ isToolEnabled: toolEnabled })
        })
        this.subscriptionVisible = updateEventFromCSharp<boolean>('zoning_adjuster_ui_namespace', 'visible', (visible) => {
            console.log(`UI visibility changed to ${visible}`);
            this.setState({ isVisible: visible })
        })
        this.setState({ isVisible: true })
    }

    componentWillUnmount() {
        this.subscriptionZoningMode?.();
        this.subscriptionToolEnabled?.();
        this.subscriptionVisible?.();
    }

    selectZoningMode = (zoningMode: ZoningMode) => {
        console.log(`Button clicked. Zoning mode ${zoningMode}`);
        sendDataToCSharp('zoning_adjuster_ui_namespace', 'zoning_mode_update', zoningMode.toString());
    }

    selectToolEnabled = () => {
        console.log(`Button clicked. Tool Enabled toggled. New value sending ${!this.state.isToolEnabled}`);
        sendDataToCSharp('zoning_adjuster_ui_namespace', 'tool_enabled', !this.state.isToolEnabled);
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
        const uiVisible = useModUIStore.getState().uiVisible;

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
            display: uiVisible === true ? 'block' : 'none'
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
            background: this.state.isToolEnabled === true ? 'green' : 'gray',
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

        const zoningOptionsStyle = {
            border: "solid"
        }

        const { isVisible } = this.state;

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
                    
                    {this.renderButton("Zoning Tool", updateZoningButtonStyle, this.selectToolEnabled)}
                    </div>
                </div>
            </Draggable>
        );
    }
}