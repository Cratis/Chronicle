// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ICalloutProps, IconButton, DirectionalHint, TooltipHost, ITooltipHostStyles, TooltipDelay } from '@fluentui/react';
import { default as styles } from './ToolbarButton.module.scss';
import { useId } from '@fluentui/react-hooks';
import { ToolbarContext } from './ToolbarContext';
import { ToolbarDirection } from './ToolbarDirection';

export type ToolbarButtonClicked = () => void;

export interface IToolbarButtonProps {
    icon: string;
    tooltip?: string;
    onClick?: ToolbarButtonClicked;
}

const hostStyles: Partial<ITooltipHostStyles> = { root: { display: 'inline-block' } };

export const ToolbarButton = (props: IToolbarButtonProps) => {
    const tooltipId = useId('tooltip');
    const calloutProps: ICalloutProps = {
        gapSpace: 0
    };

    return (
        <ToolbarContext.Consumer>
            {context => {
                calloutProps.directionalHint = context.direction === ToolbarDirection.horizontal ?
                    DirectionalHint.bottomCenter :
                    DirectionalHint.rightCenter;

                return (
                    <TooltipHost
                        id={tooltipId}
                        delay={TooltipDelay.long}
                        content={props.tooltip}
                        calloutProps={calloutProps}
                        styles={hostStyles}>
                        <IconButton iconProps={{ iconName: props.icon }}
                            label={props.tooltip}
                            className={styles.button}
                            onClick={() => props.onClick?.()} />
                    </TooltipHost>
                );
            }}
        </ToolbarContext.Consumer>
    );
};
