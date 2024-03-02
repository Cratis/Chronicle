// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


import { withViewModel } from 'MVVM';
import { LayoutContext } from './context/LayoutContext';
import { generatePath, Outlet, useParams } from 'react-router-dom';
import { NamespaceSelector } from './NamespaceSelector/NamespaceSelector';
import { IMenuItemGroup } from './Sidebar/MenuItem/MenuItem';
import { MenuProvider } from './context/MenuContext';
import { SidebarMenu } from './Sidebar/SidebarMenu';
import css from './DefaultLayout.module.css';
import { TopBar } from './TopBar/TopBar';
import { Footer } from './Footer';
import { ErrorBoundary } from 'Components/Common/ErrorBoundary';
import { DefaultLayoutViewModel } from './DefaultLayoutViewModel';
import { useContext } from 'react';

interface IDefaultLayoutProps {
    leftMenuItems?: IMenuItemGroup[];
    leftMenuBasePath?: string;
}

export const DefaultLayout = withViewModel<DefaultLayoutViewModel, IDefaultLayoutProps>(DefaultLayoutViewModel, ({ viewModel, props }) => {
    const params = useParams();
    const lmBasePath = generatePath(props.leftMenuBasePath ?? '', params);
    const layoutContext = useContext(LayoutContext);

    return (

        <div
            className={`${!layoutContext.layoutConfig.leftSidebarOpen
                ? css.sidebarClosed
                : ''
                }`}>
            <header className={css.appHeader}>
                <TopBar />
            </header>

            <aside className={css.appLeftSidebar}>
                <div className={css.sidebarContainer}>
                    <NamespaceSelector
                        namespaces={viewModel.namespaces}
                        currentNamespace={viewModel.currentNamespace}
                        onNamespaceSelected={(namespace) => viewModel.currentNamespace = namespace}
                    />
                    {props.leftMenuItems && (
                        <MenuProvider params={{ namespace: viewModel.currentNamespace }}>
                            <SidebarMenu
                                items={props.leftMenuItems}
                                basePath={lmBasePath}
                            />
                        </MenuProvider>
                    )}
                </div>
            </aside>

            <main className={css.appOutlet}>
                <ErrorBoundary>
                    <Outlet />
                </ErrorBoundary>
            </main>
            <footer className={css.appFooter}>
                <Footer />
            </footer>
        </div>
    );
});
