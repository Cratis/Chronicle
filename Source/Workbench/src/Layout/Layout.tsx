import { ReactNode, useRef } from 'react';
import { Footer } from './Footer';
import { AppTopbarRef, Topbar } from './Topbar/Topbar';
import { Sidebar } from './Sidebar';

export interface LayoutProps {
    children: ReactNode;
}


export function Layout({ children }: LayoutProps) {
    const topbarRef = useRef<AppTopbarRef>(null);
    const sidebarRef = useRef<HTMLDivElement>(null);

    return (
        <>
            <div className='layout'>
                <Topbar ref={topbarRef}/>
                <div ref={sidebarRef} className='layout-sidebar'>
                    <Sidebar/>
                </div>
                <div className='layout-main-container'>
                    <div className='layout-main'>{children}</div>
                    <Footer/>
                </div>
                {/* <AppConfig /> */}
                <div className='layout-mask'></div>
            </div>
        </>
    );
}
