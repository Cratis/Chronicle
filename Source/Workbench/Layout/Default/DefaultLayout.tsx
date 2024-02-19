// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { LayoutContext, LayoutProvider } from './context/LayoutContext';
import { generatePath, Outlet, useParams } from 'react-router-dom';
import { TenantSelector } from './TenantSelector/TenantSelector';
import { IMenuItemGroup } from './Sidebar/MenuItem/MenuItem';
import { MenuProvider } from './context/MenuContext';
import { SidebarMenu } from './Sidebar/SidebarMenu';
import css from './DefaultLayout.module.css';
import { TopBar } from './TopBar/TopBar';
import { Footer } from './Footer';
import { useState } from 'react';
import { ErrorBoundary } from 'Components/Common/ErrorBoundary';

interface IDefaultLayoutProps {
    leftMenuItems?: IMenuItemGroup[];
    leftMenuBasePath?: string;
}

export function DefaultLayout({ leftMenuItems, leftMenuBasePath }: IDefaultLayoutProps) {
    const params = useParams();
    const lmBasePath = generatePath(leftMenuBasePath ?? '', params);
    const [tenantId, setTenantId] = useState<string>('default');

    return (
        <LayoutProvider>
            <LayoutContext.Consumer>
                {(value) => (
                    <>
                        <div
                            className={`${!value.layoutConfig.leftSidebarOpen
                                    ? css.sidebarClosed
                                    : ''
                                } ${value.layoutConfig.leftSidebarHidden
                                    ? css.sidebarHidden
                                    : ''
                                }`}
                        >
                            <header className={css.appHeader}>
                                <TopBar />
                            </header>

                            {!value.layoutConfig.leftSidebarHidden && (
                                <aside className={css.appLeftSidebar}>
                                    <div className={css.sidebarContainer}>
                                        <TenantSelector
                                            onTenantSelected={(tenant) =>
                                                setTenantId(tenant.id)
                                            }
                                        />
                                        {leftMenuItems && (
                                            <MenuProvider params={{ tenantId: tenantId }}>
                                                <SidebarMenu
                                                    items={leftMenuItems}
                                                    basePath={lmBasePath}
                                                />
                                            </MenuProvider>
                                        )}
                                    </div>
                                </aside>
                            )}
                            <main className={css.appOutlet}>
                                <ErrorBoundary>
                                    <Outlet />
                                </ErrorBoundary>
                            </main>
                            <footer className={css.appFooter}>
                                <Footer />
                            </footer>
                        </div>
                    </>
                )}
            </LayoutContext.Consumer>
        </LayoutProvider>
    );
}
