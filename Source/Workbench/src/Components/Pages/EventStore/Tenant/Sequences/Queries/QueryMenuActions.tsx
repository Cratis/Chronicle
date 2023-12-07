/* Copyright (c) Aksio Insurtech. All rights reserved.
   Licensed under the MIT license. See LICENSE file in the project root for full license information. */

import { EChartExample } from './EChartExample';
import { Menubar } from 'primereact/menubar';
import css from './Queries.module.css';
import { useState } from 'react';

export const QueryMenuActions = () => {
    const [showChart, setShowChart] = useState(false);

    const showChartHandler = () => {
        setShowChart((prev) => !prev);
    };

    const items = [
        {
            label: 'Run',
            icon: 'pi pi-play',
        },
        {
            label: 'Time range',
            icon: 'pi pi-chart-line',
            command: showChartHandler,
        },
        {
            label: 'Save',
            icon: 'pi pi-save',
        },
    ];

    return (
        <div className={css.actions}>
            <div>
                <Menubar model={items} />
            </div>
            {showChart && <EChartExample />}
        </div>
    );
};
