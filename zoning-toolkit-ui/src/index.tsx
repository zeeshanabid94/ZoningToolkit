import { ModRegistrar } from "cs2/modding";
import { HelloWorldComponent } from "mods/hello-world";
import { ZoningToolkitPanel, ZoningMode } from "mods/zoning-toolkit-panel";

const register: ModRegistrar = (moduleRegistry) => {
    console.log("Registering modules.");
    // While launching game in UI development mode (include --uiDeveloperMode in the launch options)
    // - Access the dev tools by opening localhost:9444 in chrome browser.
    // - You should see a hello world output to the console.
    // - use the useModding() hook to access exposed UI, api and native coherent engine interfaces. 
    // Good luck and have fun!
    moduleRegistry.append('Menu', HelloWorldComponent);

    const zoningTookitComponent = () => <ZoningToolkitPanel />
    moduleRegistry.append('GameTopRight', zoningTookitComponent);
}

export default register;