import engine, { EventHandle } from "cohtml/cohtml";
import React from "react";
import { create } from "zustand";
import { ZoningMode } from "./zoning-toolkit-panel";

export interface ModUIState {
    uiVisible: boolean,
    photomodeActive: boolean,
    zoningMode: string,
    isFocused: boolean,
    isEnabled: boolean,
    isToolEnabled: boolean,
    updateZoningMode: (newValue: string) => void,
    updateIsToolEnabled: (newValue: boolean) => void,
    updatePhotomodeActive: (newValue: boolean) => void,
    updateUiVisible: (newValue: boolean) => void
}

const allSubscriptions: Map<string, (() => void)> = new Map();

export const setupSubscriptions = () => {
    console.log("Creating subscriptions.");

    // Init subscriptions from Mod UI System
    const subscriptionZoningModeEventString = 'zoning_adjuster_ui_namespace.zoning_mode'
    if (!allSubscriptions.has(subscriptionZoningModeEventString)) {
        const subscriptionZoningMode = updateEventFromCSharp<string>('zoning_adjuster_ui_namespace', 'zoning_mode', (zoningMode) => {
            console.log(`Zoning mode fetched ${zoningMode}`);
            useModUIStore.getState().updateZoningMode(zoningMode);
        })
        allSubscriptions.set(subscriptionZoningModeEventString, subscriptionZoningMode);
    }
    
    const subscriptionToolEnabledEventString = 'zoning_adjuster_ui_namespace.tool_enabled'
    if (!allSubscriptions.has(subscriptionToolEnabledEventString)) {
        const subscriptionToolEnabled = updateEventFromCSharp<boolean>('zoning_adjuster_ui_namespace', 'tool_enabled', (toolEnabled) => {
            console.log(`Tool Enabled Toggled ${toolEnabled}`);
            useModUIStore.getState().updateIsToolEnabled(toolEnabled);
        })
        allSubscriptions.set(subscriptionToolEnabledEventString, subscriptionToolEnabled)
    }


    const subscriptionVisibleEventString = 'zoning_adjuster_ui_namespace.visible'
    if (!allSubscriptions.has(subscriptionVisibleEventString)) {
        const subscriptionVisible = updateEventFromCSharp<boolean>('zoning_adjuster_ui_namespace', 'visible', (visible) => {
            console.log(`UI visibility changed to ${visible}`);
            useModUIStore.getState().updateUiVisible(visible);
        })
        allSubscriptions.set(subscriptionVisibleEventString, subscriptionVisible)
    }
}

export const teardownSubscriptions = () => {
    console.log("Destroying subscriptions.");

    // Unsubscribe by calling the callbacks
    allSubscriptions.forEach((callback, eventString) => {
        console.log(`Unsubscribing from event ${eventString}`)
        callback()
    })
}

const setupStore = () => {
    console.log("Initializing store.");
    const useModUIStore = create<ModUIState>((set) => ({
        uiVisible: false,
        photomodeActive: false,
        zoningMode:"Default",
        isFocused: false,
        isEnabled: false,
        isToolEnabled: false,
        updateUiVisible: (newValue: boolean) => set((state) => {
            console.log(`Updating UI Visible to ${newValue}`)
            return ({
                uiVisible: newValue
            })
        }),
        updatePhotomodeActive: (newValue: boolean) => set((state) => {
            console.log(`Updating Photomode Active to ${newValue}`)
            return ({
                photomodeActive: newValue
            })
        }),
        updateIsToolEnabled: (newValue: boolean) => set((state) => {
            console.log(`Updating IsToolEnabled ${newValue}`)
            sendDataToCSharp('zoning_adjuster_ui_namespace', 'tool_enabled', newValue);
            return ({
                isToolEnabled: newValue
            })
        }),
        updateZoningMode: (newValue: string) => set((state) => {
            console.log(`Updating ZoningMode ${newValue}`)
            sendDataToCSharp('zoning_adjuster_ui_namespace', 'zoning_mode_update', newValue);
            return ({
                zoningMode: newValue
            })
        })
    }))

    console.log("Store initialized.")
    return useModUIStore;
}

export const useModUIStore = setupStore()

export function updateEventFromCSharp<T>(namespace: string, event: string, callback: (input: T) => void): () => void {
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

export function sendDataToCSharp<T>(namespace: string, event: String, newValue: T) {
    console.log(`Event triggered. Sending new value ${newValue}`);
    engine.trigger(namespace + "." + event, newValue);
}

// This type represents the props that will be added by the HOC, which are the state and actions from the store.
type InjectedStoreProps = ModUIState;

// Utility type for adding props from the HOC to the wrapped component.
type HOCProps<C> = C extends React.ComponentType<infer P> ? P : never;

export function withStore<T extends React.ComponentType<HOCProps<T>>>(WrappedComponent: T) {
    type Props = HOCProps<T>;

    console.log("Creating HOC.")
    return class WithStore extends React.Component<Props> {
        state = useModUIStore.getState();


        unsubscribe!: () => void;

        componentDidMount() {
            this.unsubscribe = useModUIStore.subscribe(storeState => {
                this.setState(storeState);
            });
        }

        componentWillUnmount() {
            this.unsubscribe();
        }

        render() {
            // Spread the store state onto the wrapped component's props.
            // Exclude store-specific props from the component's own props to prevent type errors.
            const { ...componentProps } = this.props as any;
            return <WrappedComponent {...componentProps} {...this.state} />;
        }
    };
}