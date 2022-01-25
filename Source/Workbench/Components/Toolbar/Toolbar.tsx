// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React, { ReactElement } from 'react';
import { default as styles } from './Toolbar.module.scss';
import { IToolbarItemProps } from './IToolbarItemProps';
import { ToolbarDirection } from './ToolbarDirection';
import { ToolbarContext } from './ToolbarContext';

export interface IToolbarProps extends React.AllHTMLAttributes<HTMLUListElement> {
    direction?: ToolbarDirection;
    children?: ReactElement<IToolbarItemProps> | ReactElement<IToolbarItemProps>[];
}

export const Toolbar = (props: IToolbarProps) => {
    const direction = props.direction || ToolbarDirection.horizontal;
    const children: ReactElement<IToolbarItemProps>[] = Array.isArray(props.children) ? props.children : [props.children as ReactElement];

    return (
        <ToolbarContext.Provider value={{ direction }}>
            <ul className={`${styles.toolbar} ${direction == ToolbarDirection.horizontal ? styles.horizontal : styles.vertical}`} style={props.style}>
                {
                    children.map((item, index) => {
                        return (
                            <li key={index} className={styles.toolbarItem}>{item}</li>
                        );
                    })}
            </ul>
        </ToolbarContext.Provider>
    );
};
