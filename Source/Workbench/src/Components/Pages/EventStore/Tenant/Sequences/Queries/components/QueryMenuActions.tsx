/* Copyright (c) Aksio Insurtech. All rights reserved.
   Licensed under the MIT license. See LICENSE file in the project root for full license information. */

import { EventHistogram } from '../Histogram/Histogram';
import { Menubar } from 'primereact/menubar';
import css from '../Queries.module.css';
import { useState } from 'react';

export const QueryMenuActions = () => {
    const [showChart, setShowChart] = useState(false);
    const [activeItem, setActiveItem] = useState<string | null>(null);

    const handleMenuItemClick = (label: string) => {
        setActiveItem((prev) => (prev === label ? null : label));

        if (label === 'Time range') {
            setShowChart((prev) => !prev);
        } else {
            setShowChart(false);
        }
    };

    const getMenuItemClass = (label: string) => {
        return label === 'Time range'
            ? showChart
                ? css.activeMenuItem
                : ''
            : label === activeItem
            ? css.activeMenuItem
            : '';
    };

    const items = [
        {
            label: 'Run',
            icon: 'pi pi-play',
            command: () => handleMenuItemClick('Run'),
        },
        {
            label: 'Time range',
            icon: 'pi pi-chart-line',
            command: () => handleMenuItemClick('Time range'),
        },
        {
            label: 'Save',
            icon: 'pi pi-save',
            command: () => handleMenuItemClick('Save'),
        },
    ];

    return (
        <div className={css.actions}>
            <Menubar
                model={items.map((item) => ({
                    ...item,
                    className: getMenuItemClass(item.label),
                }))}
            />
            {showChart && (
                <>
                    Timeline here
                    <EventHistogram eventLog={''} />
                </>
            )}
        </div>
    );
};
