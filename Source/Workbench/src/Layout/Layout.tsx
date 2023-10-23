import { ReactNode } from 'react';
import { Footer } from './Footer';
import { Topbar } from './Topbar/Topbar';
import { Sidebar } from './Sidebar';
import css from './Layout.module.css';
export interface LayoutProps {
    children: ReactNode;
}

export function Layout({ children }: LayoutProps) {
    return (
        <>
            <div className={css.layoutContainer}>
                <aside>
                    <Sidebar/>
                </aside>
                <main>
                    <header>
                        <Topbar/>
                    </header>
                    <h1 className="text-3xl font-bold underline">
                        Hello world!
                    </h1>
                    {children}
                </main>
                <footer>
                    <Footer/>
                </footer>
            </div>
        </>
    );
}
