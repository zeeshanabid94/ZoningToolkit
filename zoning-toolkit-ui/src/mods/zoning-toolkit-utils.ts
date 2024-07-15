import modeIconDefault from "../../assets/icons/mode_icon_default.svg";
import modeIconNone from "../../assets/icons/mode_icon_none.svg";
import modeIconLeft from "../../assets/icons/mode_icon_left.svg";
import modeIconRight from "../../assets/icons/mode_icon_right.svg";

export enum ZoningMode {
    DEFAULT = "Default",
    NONE = "None",
    LEFT = "Left",
    RIGHT = "Right",
}

export const zoneModeIconMap: Record<ZoningMode, string> = {
    [ZoningMode.DEFAULT]: modeIconDefault,
    [ZoningMode.NONE]: modeIconNone,
    [ZoningMode.LEFT]: modeIconLeft,
    [ZoningMode.RIGHT]: modeIconRight,
};

/** Get zoning mode from string */
export function getModeFromString(value: string): ZoningMode | undefined {
    return Object.values(ZoningMode).find((zoningMode) => zoningMode === value);
}
