import { ModRegistrar } from "cs2/modding";
import { HelloWorldComponent } from "mods/hello-world";
import { ZoningToolkitPanel, ZoningMode } from "mods/zoning-toolkit-panel";
import { ButtonComponent } from "./mods/zoning-toolkit-button";

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

    const zoningTookitComponent = () => <ZoningToolkitPanel />
    const zoningToolkitPanelButton = () => <ButtonComponent/>
    moduleRegistry.append('GameTopRight', zoningTookitComponent);
    moduleRegistry.append('GameBottomRight', zoningToolkitPanelButton);
}

export default register;