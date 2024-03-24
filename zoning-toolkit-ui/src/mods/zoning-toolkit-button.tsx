import { ModuleRegistryExtend } from "cs2/modding";
import { Button, FloatingButton } from "cs2/ui";
import React from 'react';
import iconStyles from "./icon.module.scss";
import icon from "../../assets/Zoning Toolkit Icon.png";

interface ButtonState {
	isHovered: boolean
}

export class ButtonComponent extends React.Component<{}, ButtonState> {
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

	render() {
		const style: React.CSSProperties = {
			position: "absolute",
			top: "-25%",
			right: "5%",
			zIndex: 100,
			pointerEvents: "auto",
			borderRadius: "50%",
			border: "2px solid black"
		};

		const hoverStyle: React.CSSProperties = {
			...style,
			border: '2px solid white',
		}

		return <div style={this.state.isHovered ? hoverStyle : style}>
			<Button
				variant={"round"}
				onMouseEnter={this.handleMouseEnter}
				onMouseLeave={this.handleMouseLeave}
		>
				<img src={icon} className={iconStyles.icon} />
			</Button>
			{/*<div className={iconStyles.cssIcon}/>*/}
		</div>
	}
}
