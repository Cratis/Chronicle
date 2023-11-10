import { LayoutContext, LayoutProvider } from './context/LayoutContext';
import { Sidebar } from './Sidebar/Sidebar';
import { Topbar } from './Topbar/Topbar';
import css from './DefaultLayout.module.css';
import { Footer } from './Footer';
import { generatePath, Outlet, useParams } from "react-router-dom";
import { SidebarMenu } from "./Sidebar/SidebarMenu";
import { IMenuItem } from "./Sidebar/MenuItem/MenuItem";

interface IDefaultLayoutProps {
    leftMenuItems?: IMenuItem[];
    leftMenuBasePath?: string;
}

export function DefaultLayout({ leftMenuItems, leftMenuBasePath }: IDefaultLayoutProps) {
    const params = useParams();
    const lmBasePath = generatePath(leftMenuBasePath ?? '', params);
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
                                        <Sidebar>
                                            {leftMenuItems &&
                                                <SidebarMenu items={leftMenuItems} basePath={lmBasePath}/>}
                                        </Sidebar>
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
