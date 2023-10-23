import { ReactNode } from 'react';
import { Footer } from './Footer';
import { Topbar } from './Topbar/Topbar';
import { SidebarComponent } from './Sidebar/Sidebar';

export interface LayoutProps {
    children: ReactNode;
}

export function Layout({ children }: LayoutProps) {
    return (
        <>
            <div>
                <Topbar />
                <div>
                    <SidebarComponent />
                </div>
                <div>
                    <div>{children}</div>
                    <Footer />
                </div>
            </div>
        </>
    );
}
