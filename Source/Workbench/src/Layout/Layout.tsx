import { ReactNode } from 'react';
import { Footer } from './Footer';
import { Topbar } from './Topbar/Topbar';
import { Sidebar } from './Sidebar';

export interface LayoutProps {
    children: ReactNode;
}

export function Layout({ children }: LayoutProps) {

    // Layout module css classes file

    return (
        <>
            <div >
                <Topbar />
                <div >
                    <Sidebar />
                </div>
                <div>
                    <div>{children}</div>
                    <Footer />
                </div>
            </div>
        </>
    );
}
