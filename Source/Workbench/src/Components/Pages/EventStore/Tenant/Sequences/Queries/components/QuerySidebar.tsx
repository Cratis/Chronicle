/* Copyright (c) Aksio Insurtech. All rights reserved.
   Licensed under the MIT license. See LICENSE file in the project root for full license information. */

import { FaAnglesRight, FaAnglesLeft } from 'react-icons/fa6';
import { useEffect, useState } from 'react';
import { Button } from 'primereact/button';
import css from '../Queries.module.css';

export interface QuerySidebarProps {
    children?: React.ReactNode;
    tabIndex?: number;
}

export const QuerySidebar = (props: QuerySidebarProps) => {
    const { children, tabIndex } = props;
    const [isSidebarOpen, setSidebarOpen] = useState(false);

    const toggleSidebar = () => {
        setSidebarOpen(!isSidebarOpen);
    };

    useEffect(() => {}, [tabIndex]);

    return (
        <div className={`${css.queryContainer} ${isSidebarOpen ? '' : css.collapsed}`}>
            <Button
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
