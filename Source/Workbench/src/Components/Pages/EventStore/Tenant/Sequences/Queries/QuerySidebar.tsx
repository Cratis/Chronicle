import { FaAnglesRight, FaAnglesLeft } from 'react-icons/fa6';
import { Button } from 'primereact/button';
import css from './Queries.module.css';

import { useState } from 'react';

export interface QuerySidebarProps {
    children?: React.ReactNode;
}

export const QuerySidebar = (props: QuerySidebarProps) => {
    const { children } = props;
    const [isSidebarOpen, setSidebarOpen] = useState(false);

    const toggleSidebar = () => {
        setSidebarOpen(!isSidebarOpen);
    };

    return (
        <div className={`${css.queryContainer} ${isSidebarOpen ? '' : css.collapsed}`}>
            <Button
                text
                rounded
                unstyled
                onClick={toggleSidebar}
                icon={isSidebarOpen ? <FaAnglesLeft /> : <FaAnglesRight />}
                className={`${css.toggleButton} ${isSidebarOpen ? css.open : ''}`}
            />
            <div
                className={`${css.querySidebar} ${isSidebarOpen ? css.open : css.closed}`}
            >
                {children}
            </div>
        </div>
    );
};
