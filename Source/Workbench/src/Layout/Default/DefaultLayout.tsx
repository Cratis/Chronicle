import { LayoutContext, LayoutProvider } from './context/LayoutContext';
import { Topbar } from './Topbar/Topbar';
import { Footer } from './Footer';
import { generatePath, Outlet, useParams } from "react-router-dom";
import { SidebarMenu } from "./Sidebar/SidebarMenu";
import { IMenuItemGroup } from "./Sidebar/MenuItem/MenuItem";
import { TenantSelector } from "./TenantSelector/TenantSelector";
import { useState } from "react";
import { MenuProvider } from "./context/MenuContext";

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
                            className={`${
                                !value.layoutConfig.leftSidebarOpen
                                    ? css.sidebarClosed
                                    : ''
                            } ${
                                value.layoutConfig.leftSidebarHidden
                                    ? css.sidebarHidden
                                    : ''
                            }`}
                        >
                            <header className={css.appHeader}>
                                <Topbar/>
                            </header>

                            {!value.layoutConfig.leftSidebarHidden && (
                                <aside className={css.appLeftSidebar}>
                                    <div className={css.sidebarContainer}>
                                        <TenantSelector className="mb-4 mt-1" onTenantSelected={(tenant) => setTenantId(tenant.id)}/>
                                        {leftMenuItems &&
                                            <MenuProvider params={{ tenantId: tenantId }}>
                                                <SidebarMenu items={leftMenuItems} basePath={lmBasePath}/>
                                            </MenuProvider>}
                                    </div>
                                </aside>
                            )}
                            <main className={css.appOutlet}>
                                <Outlet/>
                            </main>
                            <footer className={css.appFooter}>
                                <Footer/>
                            </footer>
                        </div>
                    </>
                )}
            </LayoutContext.Consumer>
        </LayoutProvider>
    );
}
