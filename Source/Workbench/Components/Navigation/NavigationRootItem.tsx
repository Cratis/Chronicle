// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ListItemButton, ListItemIcon, ListItemText } from '@mui/material';
import { ChildElementWithChildren } from './NavigationPage';
import { ChildTypes, PropsForComponentWithChildTypes } from '../ComponentWithChildTypesUtility';
import { MouseEventHandler } from 'react';

export interface NavigationRootItemProps extends PropsForComponentWithChildTypes {
    title: string;
    icon?: JSX.Element;
    onClick?: MouseEventHandler<HTMLAnchorElement>;
}

const Actions = ChildElementWithChildren();

export const NavigationRootItem = (props: NavigationRootItemProps) => {
    const childTypes = ChildTypes.get(props);

    return (
        <ListItemButton component="a" onClick={props.onClick}>
            {props.icon && <ListItemIcon>{props.icon}</ListItemIcon> }
            <ListItemText
                sx={{ my: 0 }}
                primaryTypographyProps={{
                    fontSize: 20,
                    fontWeight: 'medium',
                    letterSpacing: 0
                }}>
                {props.title}
            </ListItemText>
        </ListItemButton>
    );
};

NavigationRootItem.Actions = Actions;
