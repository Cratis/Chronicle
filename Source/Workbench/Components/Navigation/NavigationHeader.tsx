// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ListItemButton, ListItemText } from '@mui/material';
import { ChildElementWithChildren } from './NavigationPage';
import { ChildTypes, PropsForComponentWithChildTypes } from '../ComponentWithChildTypesUtility';

export interface NavigationHeaderProps extends PropsForComponentWithChildTypes {
    title: string;
}

const Icon = ChildElementWithChildren();
const Actions = ChildElementWithChildren();

export const NavigationHeader = (props: NavigationHeaderProps) => {
    const childTypes = ChildTypes.get(props);
    const icon = childTypes.getSingleSpecificType(Icon);

    return (
        <ListItemButton component="a">
            {icon && icon}
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

NavigationHeader.Icon = Icon;
NavigationHeader.Actions = Actions;
