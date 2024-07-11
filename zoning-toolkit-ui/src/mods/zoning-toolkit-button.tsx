import { Button } from "cs2/ui";
import React, { CSSProperties } from 'react';

import menuIcon from "../../assets/icons/menu_icon.svg";
import menuButtonStyles from "./zoning-toolkit-button.module.scss";
import { useModUIStore, withStore } from "./state";
import VanillaBindings from "./vanilla-bindings";

const { DescriptionTooltip } = VanillaBindings.components;

class ZoningToolkitMenuButtonInternal extends React.Component {
	constructor(props: {}) {
		super(props);
	}

	handleMenuButtonClick = () => {
		console.log("ZT Mouse clicked toolkit menu button");
		useModUIStore.getState().updateUiVisible(!useModUIStore.getState().uiVisible)
	}

	render() {
		const photomodeActive = useModUIStore.getState().photomodeActive

		const buttonStyle: CSSProperties = {
			// Menu button should be hidden in photo mode
			display: photomodeActive ? "none" : undefined,
		};

		return (
			<DescriptionTooltip
				description="Control/modify zoning along roads. Allows zoning on both sides of roads, on any one side, or no sides at all."
				direction="right"
				title="Zoning Toolkit"
			>
				<Button
					style={buttonStyle}
					variant="floating"
					onClick={this.handleMenuButtonClick}
				>
					<img src={menuIcon} className={menuButtonStyles.menuIcon} />
				</Button>
			</DescriptionTooltip>
		);
	}
}

export const ZoningToolkitMenuButton = withStore(ZoningToolkitMenuButtonInternal)