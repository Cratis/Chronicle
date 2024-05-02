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
import { ErrorBoundary } from 'Components/Common/ErrorBoundary';
import { useContext, useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { Namespace } from 'API/Namespaces';

interface IDefaultLayoutProps {
    menu?: IMenuItemGroup[];
    basePath?: string;
}

export const DefaultLayout = (props: IDefaultLayoutProps) => {
    const params = useParams();
    const sidebarBasePath = generatePath(props.basePath ?? '', params);
    const layoutContext = useContext(LayoutContext);
    const navigate = useNavigate();
    const location = useLocation();
    const [namespace, setNamespace] = useState('');

    const namespaceSelected = (namespace: Namespace) => {
        setNamespace(namespace.name);
        const newRoute = location.pathname.replace(params.namespace!, namespace.name);
        navigate(newRoute);
    };

    return (
        <ErrorBoundary>
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
                        <NamespaceSelector onNamespaceSelected={namespaceSelected} />
                        {props.menu && (
                            <MenuProvider params={{ namespace }}>
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
                        <Outlet />
                    </ErrorBoundary>
                </main>
                <footer className={css.appFooter}>
                    <Footer />
                </footer>
            </div>
        </ErrorBoundary>
    );
};
