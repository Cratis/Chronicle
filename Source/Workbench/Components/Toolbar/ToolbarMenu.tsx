// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ReactElement, useState, useRef } from 'react';
import { default as styles } from './ToolbarMenu.module.scss';
import { IToolbarItemProps } from './IToolbarItemProps';
import { ToolbarButton } from './ToolbarButton';
import { ToolbarContext } from './ToolbarContext';
import { ToolbarDirection } from './ToolbarDirection';
import { Guid } from '@cratis/fundamentals';
import { Toolbar } from './Toolbar';

export interface IToolbarMenuProps {
    icon: string;
    tooltip?: string;
    children?: ReactElement<IToolbarItemProps> | ReactElement<IToolbarItemProps>[];
}

export const ToolbarMenu = (props: IToolbarMenuProps) => {
    const [id] = useState(Guid.create());
    const menu = useRef<HTMLUListElement>(null);
    const [showMenu, setShowMenu] = useState(false);
    const children: ReactElement<IToolbarItemProps>[] = Array.isArray(props.children) ? props.children : [props.children as ReactElement];

    const style = showMenu ? {} : { display: 'none' };

    document.body.addEventListener('click', (e) => {
        let withinMenu = false;
        let current = e.target as HTMLElement | null | undefined;
        do {
            if (current?.id === id.toString()) {
                withinMenu = true;
                break;
            }
            current = current?.parentElement;
        } while (current);

        if (!withinMenu) {
            setShowMenu(false);
        }
    });

    return (
        <>
            <div id={id.toString()}>
                <ToolbarButton icon={props.icon} tooltip={props.tooltip} onClick={() => setShowMenu(showMenu !== true)} />
                <ToolbarContext.Consumer>
                    {context => {
                        const direction = context.direction === ToolbarDirection.horizontal ? ToolbarDirection.vertical : ToolbarDirection.horizontal
                        return (
                            <div className={`${styles.toolbarMenu} ${context.direction === ToolbarDirection.horizontal ? styles.verticalMenu : styles.horizontalMenu}`} style={style}>
                                <Toolbar direction={direction} style={{margin:0}}>
                                    {props.children}
                                </Toolbar>
                            </div>);
                    }}
                </ToolbarContext.Consumer>
            </div>
        </>
    );
};
