import { ReactNode } from 'react';
import { Footer } from './Footer';
import { Topbar } from './Topbar/Topbar';
<<<<<<< HEAD
import { SidebarComponent } from './Sidebar/Sidebar';
=======
import { Sidebar } from './Sidebar';
import css from './Layout.module.css';
import { FaDAndD } from "react-icons/fa6";
>>>>>>> 6f37201b015dc1425534a39b2a53fae21b72eaaa

export interface LayoutProps {
    children: ReactNode;
}

export function Layout({ children }: LayoutProps) {
    return (
        <>
<<<<<<< HEAD
            <div>
                <Topbar />
                <div>
                    <SidebarComponent />
                </div>
                <div>
                    <div>{children}</div>
                    <Footer />
                </div>
=======
            <div className={css.layoutContainer}>
                <aside>
                    <Sidebar/>
                </aside>
                <main>
                    <header>
                        <Topbar/>
                    </header>
                    <h1 className="text-3xl font-bold underline">
                        <FaDAndD/>
                        Hello world!
                    </h1>
                    {children}
                </main>
                <footer>
                    <Footer/>
                </footer>
>>>>>>> 6f37201b015dc1425534a39b2a53fae21b72eaaa
            </div>
        </>
    );
}
