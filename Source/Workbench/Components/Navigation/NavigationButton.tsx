// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Box, ListItemButton, ListItemIcon, ListItemText, SxProps, Theme, TypographyProps } from '@mui/material';
import { ChildElementWithChildren } from './NavigationPage';
import { ChildTypes, PropsForComponentWithChildTypes } from '../ComponentWithChildTypesUtility';
import { MouseEventHandler } from 'react';
import { theme } from '../../theme';

export enum NavigationButtonVariant {
    Header = 'Header',
    Primary = 'primary'
}

export interface NavigationButtonProps extends PropsForComponentWithChildTypes {
    title: string;
    icon?: JSX.Element;
    onClick?: MouseEventHandler<HTMLAnchorElement>;
    variant?: NavigationButtonVariant;
}

const Actions = ChildElementWithChildren();

function getBackgroundStyle(variant: NavigationButtonVariant): SxProps<Theme> {
    switch (variant) {
        case NavigationButtonVariant.Primary:
            return {
                bgcolor: theme.palette.primary.main,
                p: 0,
                m: 0
            };
    }
    return {};
}

function getButtonStyle(variant: NavigationButtonVariant): SxProps<Theme> {
    switch (variant) {
        case NavigationButtonVariant.Header:
            return {
                my: 0,
                pb: 2,
                pt: 2
            };

        case NavigationButtonVariant.Primary:
            return {
                pl: 4,
                py: 2,
                '&:hover, &focus': { '& svg': { opacity: 1 } }
            };
    }
    return {};
}

function getTypography(variant: NavigationButtonVariant): TypographyProps {
    switch (variant) {
        case NavigationButtonVariant.Header: {
            return {
                fontSize: 20,
                fontWeight: 'medium',
                letterSpacing: 0
            };
        }
    }

    return {};
}

export const NavigationButton = (props: NavigationButtonProps) => {
    const childTypes = ChildTypes.get(props);
    const variant = props.variant ?? NavigationButtonVariant.Primary;

    return (
        <Box sx={getBackgroundStyle(variant)}>
            <ListItemButton
                sx={getButtonStyle(variant)}
                component='a'
                onClick={props.onClick}>
                {props.icon && <ListItemIcon>{props.icon}</ListItemIcon>}
                <ListItemText
                    primaryTypographyProps={getTypography(variant)}>
                    {props.title}
                </ListItemText>
            </ListItemButton>
        </Box>
    );
};

NavigationButton.Actions = Actions;
