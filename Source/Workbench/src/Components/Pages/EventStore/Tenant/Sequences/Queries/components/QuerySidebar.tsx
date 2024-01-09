/* Copyright (c) Aksio Insurtech. All rights reserved.
   Licensed under the MIT license. See LICENSE file in the project root for full license information. */

import { FaAnglesRight, FaAnglesLeft } from 'react-icons/fa6';
import { Button } from 'primereact/button';
import css from '../Queries.module.css';

export interface QuerySidebarProps {
    children?: React.ReactNode;
    isSidebarOpen: boolean;
    toggleSidebar: () => void;
}

export const QuerySidebar = (props: QuerySidebarProps) => {
    const { children, isSidebarOpen, toggleSidebar } = props;

    return (
        <div className={`${css.queryContainer} ${isSidebarOpen ? '' : css.collapsed}`}>
            <Button
                unstyled
                onClick={toggleSidebar}
                icon={isSidebarOpen ? <FaAnglesLeft /> : <FaAnglesRight />}
                className={`${css.toggleButton} ${isSidebarOpen ? css.open : css.closed}`}
            />
            <div
                className={`${css.querySidebar} ${isSidebarOpen ? css.open : css.closed}`}
            >
                {children}
            </div>
        </div>
    );
};
