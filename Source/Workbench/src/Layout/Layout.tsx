import { SidebarComponent } from './Sidebar/Sidebar';
import { Topbar } from './Topbar/Topbar';
import css from './Layout.module.css';
import { ReactNode } from 'react';
import { Footer } from './Footer';

export interface LayoutProps {
    children: ReactNode;
}

export function Layout({ children }: LayoutProps) {
    return (
        <>
            <div className={css.layoutContainer}>
                <aside>
                    <SidebarComponent />
                </aside>
                <main>
                    <header>
                        <Topbar />
                    </header>
                    {children}
                </main>
                <footer>
                    <Footer />
                </footer>
            </div>
        </>
    );
}
