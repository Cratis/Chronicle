// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Box, Grid, Paper } from '@mui/material';
import { ReactElement } from 'react';
import { NavigationContainer } from './NavigationContainer';
import { ChildTypes, PropsForComponentWithChildTypes } from '../ComponentWithChildTypesUtility';

export function ChildElementWithChildren() {
    return ({ children }: { children: ReactElement | ReactElement[] }) => <>{children}</>;
}

const Navigation = ChildElementWithChildren();
const Content = ChildElementWithChildren();

export const NavigationPage = (props: PropsForComponentWithChildTypes) => {
    const childTypes = ChildTypes.get(props);
    const navigation = childTypes.getSingleSpecificType(Navigation);
    const content = childTypes.getSingleSpecificType(Content);

    return (
        <>
            <Grid container sx={{ height: '100%' }}>
                <Grid item xs={2}>
                    <Paper elevation={1} sx={{ width: '100%', height: '100%' }}>
                        <NavigationContainer>
                            {navigation && navigation}
                        </NavigationContainer>
                    </Paper>
                </Grid>
                <Grid item xs={10}>
                    <Paper elevation={0} sx={{ height: '100%', padding: '24px' }}>
                        {content && content}
                    </Paper>
                </Grid>
            </Grid>
        </>
    );
};

NavigationPage.Navigation = Navigation;
NavigationPage.Content = Content;
