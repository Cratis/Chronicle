// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Box, ListItemButton, ListItemIcon, ListItemText } from '@mui/material';
import { ChildElementWithChildren } from './NavigationPage';
import { ChildTypes, PropsForComponentWithChildTypes } from '../ComponentWithChildTypesUtility';
import { MouseEventHandler } from 'react';
import { theme } from '../../theme';

export interface NavigationItemProps extends PropsForComponentWithChildTypes {
    title: string;
    icon?: JSX.Element;
    onClick?: MouseEventHandler<HTMLAnchorElement>;
}

const Actions = ChildElementWithChildren();

export const NavigationItem = (props: NavigationItemProps) => {
    const childTypes = ChildTypes.get(props);

    return (
        <Box
            sx={{
                bgcolor: theme.palette.divider,
                pb: 2,
            }}>

            <ListItemButton
                sx={{
                    px: 3,
                    pt: 2.5,
                    pb: 0,
                    '&:hover, &focus': { '& svg': { opacity: 1 } }
                }}
                component="a"
                onClick={props.onClick}>
                {props.icon && <ListItemIcon>{props.icon}</ListItemIcon>}
                <ListItemText>{props.title}</ListItemText>
            </ListItemButton>
        </Box>
    );
};

NavigationItem.Actions = Actions;
