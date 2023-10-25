import { LayoutContext, LayoutProvider } from "./context/LayoutContext";
import { Sidebar } from './Sidebar/Sidebar';
import { Topbar } from './Topbar/Topbar';
import css from './Layout.module.css';
import { ReactNode } from 'react';
import { Footer } from './Footer';

export interface LayoutProps {
    children: ReactNode;
}

export function Layout({ children }: LayoutProps) {

    return (
        <LayoutProvider>
            <LayoutContext.Consumer>
                {(value) => (
                    <>
                        <div
                            className={`
                            ${css.layoutContainer}
                            ${!value.layoutConfig.leftSidebarOpen ? css.sidebarClosed : ''}
                            ${value.layoutConfig.leftSidebarHidden ? css.sidebarHidden : ''}
                            `}>
                            {!value.layoutConfig.leftSidebarHidden && <aside><Sidebar/></aside>}
                            <main>
                                <header>
                                    <Topbar/>
                                </header>

                                {children}
                            </main>
                            <footer>
                                <Footer/>
                            </footer>
                        </div>
                    </>
                )}
            </LayoutContext.Consumer>

        </LayoutProvider>
    )
        ;
}
