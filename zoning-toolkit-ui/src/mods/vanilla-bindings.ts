import { getModule } from "cs2/modding";
import { ButtonProps, TooltipProps } from "cs2/ui";
import { FC, ReactNode } from "react";

/** NOTE: Vanilla prop types are a best-guess currently... */
export interface DescriptionTooltipProps extends Omit<TooltipProps, 'tooltip'> {
  title: string | null;
  description: string | null;
  content?: ReactNode | string | null;
}

/** Tooltip with title and description */
const DescriptionTooltip: FC<DescriptionTooltipProps> = getModule(
	"game-ui/common/tooltip/description-tooltip/description-tooltip.tsx",
	"DescriptionTooltip"
);

/** NOTE: Vanilla prop types are a best-guess currently... */
export interface ToolButtonProps extends ButtonProps {
  /** Icon source */
  src: string;
  tooltip?: string;
}

/** Toolbar icon button (with selection state) */
const ToolButton: FC<ToolButtonProps> = getModule(
	"game-ui/game/components/tool-options/tool-button/tool-button.tsx",
  "ToolButton",
);

/** Manually exported/bound modules that are not exported directly by CS2; use with caution! */
export default {
  components: {
    DescriptionTooltip,
    ToolButton,
  },
  common: {
    focus: {
      auto: getModule("game-ui/common/focus/focus-key.ts", "FOCUS_AUTO"),
      disabled: getModule("game-ui/common/focus/focus-key.ts", "FOCUS_DISABLED"),
    }
  }
};
