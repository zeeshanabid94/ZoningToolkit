import { ModRegistrar } from "cs2/modding";
import { HelloWorldComponent } from "mods/hello-world";
import { ZoningToolkitPanel } from "mods/zoning-toolkit-panel";
import { ButtonComponent } from "./mods/zoning-toolkit-button";
import React from "react";
import { setupSubscriptions, teardownSubscriptions } from "./mods/state";

const register: ModRegistrar = (moduleRegistry) => {
    console.log("Registering modules.");

    moduleRegistry.find(".*").forEach((each) => {
        console.log(`Module: ${each}`);
    })
    // While launching game in UI development mode (include --uiDeveloperMode in the launch options)
    // - Access the dev tools by opening localhost:9444 in chrome browser.
    // - You should see a hello world output to the console.
    // - use the useModding() hook to access exposed UI, api and native coherent engine interfaces. 
    // Good luck and have fun!
    moduleRegistry.append('Menu', HelloWorldComponent);
    moduleRegistry.append('GameTopRight', () => <App/>);
}

class App extends React.Component<{}> {
    componentDidMount() {
        setupSubscriptions()
    }

    componentWillUnmount() {
        teardownSubscriptions()
    }

    render() {
        return <><ZoningToolkitPanel /><ButtonComponent /></>
    }
}

export default register;