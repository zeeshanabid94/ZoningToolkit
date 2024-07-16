import { ModRegistrar } from "cs2/modding";
import { ZoningToolkitPanel } from "mods/zoning-toolkit-panel";
import { ZoningToolkitMenuButton } from "./mods/zoning-toolkit-button";
import React from "react";

import { setupSubscriptions, teardownSubscriptions } from "./mods/state";

const register: ModRegistrar = (moduleRegistry) => {
    console.log("ZoningToolkit: Registering modules");

    moduleRegistry.find(".*").forEach((each) => {
        console.log(`Module: ${each}`);
    });

    // While launching game in UI development mode (include --uiDeveloperMode in the launch options)
    // - Access the dev tools by opening localhost:9444 in chrome browser.
    // - use the useModding() hook to access exposed UI, api and native coherent engine interfaces. 
    moduleRegistry.append('GameTopLeft', () => <ZoningToolkitUi/>);
}

class ZoningToolkitUi extends React.Component<{}> {
    componentDidMount() {
        setupSubscriptions()
    }

    componentWillUnmount() {
        teardownSubscriptions()
    }

    render() {
        return <><ZoningToolkitPanel /><ZoningToolkitMenuButton /></>
    }
}

export default register;