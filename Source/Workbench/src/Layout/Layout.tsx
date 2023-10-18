import { AppTopbarRef, ChildContainerProps } from './layout';
import React, { useRef } from 'react';
import { Footer } from './Footer';
import { Topbar } from './Topbar';
import { Sidebar } from './Sidebar';

export function Layout({ children }: ChildContainerProps) {
    const topbarRef = useRef<AppTopbarRef>(null);
    const sidebarRef = useRef<HTMLDivElement>(null);

    return (
        <React.Fragment>
            <div className='layout'>
                <Topbar ref={topbarRef} />
                <div ref={sidebarRef} className='layout-sidebar'>
                    <Sidebar />
                </div>
                <div className='layout-main-container'>
                    <div className='layout-main'>{children}</div>
                    <Footer />
                </div>
                {/* <AppConfig /> */}
                <div className='layout-mask'></div>
            </div>
        </React.Fragment>
    );
}
