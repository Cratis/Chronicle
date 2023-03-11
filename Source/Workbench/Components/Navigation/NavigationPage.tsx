// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Grid, Paper } from '@mui/material';
import { ReactElement } from 'react';
import { NavigationContainer } from './NavigationContainer';
import { NavigationItem } from './NavigationItem';
import { ChildTypes, PropsForComponentWithChildTypes } from '../ComponentWithChildTypesUtility';
import { useNavigate, Routes, Route } from 'react-router-dom';
import { NavigationButton, NavigationButtonVariant } from './NavigationButton';

export interface NavigationPageProps extends PropsForComponentWithChildTypes {
    navigationItems?: NavigationItem[];
    baseRoute?: string;
}

export function ChildElementWithChildren() {
    return ({ children }: { children: ReactElement | ReactElement[] }) => <>{children}</>;
}

const Navigation = ChildElementWithChildren();
const Content = ChildElementWithChildren();

export const NavigationPage = (props: NavigationPageProps) => {
    const childTypes = ChildTypes.get(props);
    const navigation = childTypes.getSingleSpecificType(Navigation);
    const content = childTypes.getSingleSpecificType(Content);
    const navigate = useNavigate();

    return (
        <>
            <Grid container sx={{ height: '100%' }}>
                <Grid item xs={2}>
                    <Paper elevation={1} sx={{ width: '100%', height: '100%' }}>
                        <NavigationContainer>
                            {props.navigationItems && props.navigationItems.map((item, index) => {
                                return (
                                    <NavigationButton
                                        key={index}
                                        variant={NavigationButtonVariant.Header}
                                        title={item.title}
                                        icon={item.icon}
                                        onClick={() => navigate(item.path)} />);
                            })}

                            <Routes>
                                {props.navigationItems && props.navigationItems.map((item, index) => {
                                    return (
                                        <Route key={index} path={`${item.path}/*`} element={
                                            <>
                                                {item.children?.map((child, childIndex) => {
                                                    return (
                                                        <NavigationButton
                                                            key={childIndex}
                                                            variant={NavigationButtonVariant.Primary}
                                                            title={child.title}
                                                            icon={child.icon}
                                                            onClick={() => navigate(`${item.path}/${child.path}`)} />);
                                                })}
                                            </>
                                        } />
                                    );
                                })}
                            </Routes>

                            {navigation && navigation}
                        </NavigationContainer>
                    </Paper>
                </Grid>
                <Grid item xs={10}>
                    <Paper elevation={0} sx={{ height: '100%', padding: '24px' }}>
                        <Routes>
                            {props.navigationItems && props.navigationItems.map((item, index) => {
                                return (
                                    <Route key={index} path={`${item.path}`} element={item.content}>
                                        <>
                                            {item.children?.map((child, childIndex) => {
                                                return (
                                                    <Route key={`${index}-${childIndex}`} path={`${child.path}`} element={child.content} />
                                                );
                                            })}
                                        </>
                                    </Route>
                                );
                            })}
                        </Routes>
                        {content && content}
                    </Paper>
                </Grid>
            </Grid>
        </>
    );
};

NavigationPage.Navigation = Navigation;
NavigationPage.Content = Content;
