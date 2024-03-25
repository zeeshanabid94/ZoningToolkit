import { ModuleRegistryExtend } from "cs2/modding";
import { Button, FloatingButton, Tooltip } from "cs2/ui";
import React from 'react';
import iconStyles from "./icon.module.scss";
import icon from "../../assets/Zoning Toolkit Icon.png";
import { useModUIStore, withStore } from "./state";
import Draggable from "react-draggable";

interface ButtonState {
	isHovered: boolean
}

class ButtonComponentInternal extends React.Component<{}, ButtonState> {
	constructor(props: {}) {
		super(props);
		// Initialize state
		this.state = { isHovered: false };
	}

	// Method to handle mouse enter
	handleMouseEnter = () => {
		console.log("Mouse on button.")
		this.setState({ isHovered: true });
	};

	// Method to handle mouse leave
	handleMouseLeave = () => {
		console.log("Mouse off button.")
		this.setState({ isHovered: false });
	};

	// Method to handle click
	handleMouseClick = () => {
		console.log("Mouse clicked button.");
		useModUIStore.getState().updateUiVisible(!useModUIStore.getState().uiVisible)
	}

	render() {
		const photomodeActive = useModUIStore.getState().photomodeActive

		let style: React.CSSProperties = {
			position: "relative",
			zIndex: 100,
			pointerEvents: "auto",
			borderRadius: "50%",
			border: "2px solid black"
		};

		const hoverStyle: React.CSSProperties = {
			...style,
			border: '2px solid white',
		}

		if (photomodeActive) {
			style = {
				...style,
				display: "none"
			}
		}

		return <Draggable grid={[1, 1]}>
			<div style={this.state.isHovered ? hoverStyle : style}>
				<Button
					variant={"round"}
					onMouseEnter={this.handleMouseEnter}
					onMouseLeave={this.handleMouseLeave}
					onClick={this.handleMouseClick}
				>
					<img src={icon} className={iconStyles.icon} />
				</Button>
				{/*<div className={iconStyles.cssIcon}/>*/}
			</div>
		</Draggable>
		
	}
}

export const ButtonComponent = withStore(ButtonComponentInternal)