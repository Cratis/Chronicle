// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { LayoutContext } from './context/LayoutContext';
import { generatePath, Outlet, useParams } from 'react-router-dom';
import { NamespaceSelector } from './NamespaceSelector/NamespaceSelector';
import { IMenuItemGroup } from './Sidebar/MenuItem/MenuItem';
import { MenuProvider } from './context/MenuContext';
import { SidebarMenu } from './Sidebar/SidebarMenu';
import css from './DefaultLayout.module.css';
import { TopBar } from './TopBar/TopBar';
import { Footer } from './Footer';
import { ErrorBoundary } from '@cratis/components/Common';
import { useContext, useMemo } from 'react';
import * as Shared from 'Shared';
import { useWorkbenchContext, WorkbenchProvider } from './context/WorkbenchContext';

interface IDefaultLayoutProps {
    menu?: IMenuItemGroup[];
    basePath?: string;
}

const DefaultLayoutFrame = (props: IDefaultLayoutProps) => {
    const params = useParams<Shared.EventStoreAndNamespaceParams>();
    const sidebarBasePath = generatePath(props.basePath ?? '', params);
    const layoutContext = useContext(LayoutContext);
    const { namespace } = useWorkbenchContext();
    const menuParams = useMemo(() => ({ namespace }), [namespace]);

    return (
        <ErrorBoundary>
            <div className={layoutContext.layoutConfig.leftSidebarOpen ? '' : css.sidebarClosed}>
                <header className={css.appHeader}>
                    <TopBar />
                </header>

                <aside className={css.appLeftSidebar}>
                    <div className={css.sidebarContainer}>
                        <NamespaceSelector />
                        {props.menu && (
                            <MenuProvider params={menuParams}>
                                <SidebarMenu
                                    items={props.menu}
                                    basePath={sidebarBasePath}
                                />
                            </MenuProvider>
                        )}
                    </div>
                </aside>

                <main className={css.appOutlet}>
                    <ErrorBoundary>
                        {/* A different event store or namespace gets pages that have never seen the old one,
                            so every open query asks again for the data of the place it is now in. */}
                        <Outlet key={`${params.eventStore}/${params.namespace ?? ''}`} />
                    </ErrorBoundary>
                </main>
                <footer className={css.appFooter}>
                    <Footer />
                </footer>
            </div>
        </ErrorBoundary>
    );
};

export const DefaultLayout = (props: IDefaultLayoutProps) => (
    <WorkbenchProvider>
        <DefaultLayoutFrame {...props} />
    </WorkbenchProvider>
);
