// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Button } from 'primereact/button';
import { Card } from 'primereact/card';
import { Column } from 'primereact/column';
import { DataTable } from 'primereact/datatable';
import { Dropdown } from 'primereact/dropdown';
import { Tag } from 'primereact/tag';
import { useState } from 'react';
import { MdAdd } from 'react-icons/md';

interface ObserverRow {
    name: string;
    events: string;
    lagEvents: number;
    throughput: string;
    lastError: string;
    status: 'success' | 'warning' | 'danger';
}

export const ObserverHealthWidget = ({ className }: { className?: string }) => {
    const [timeRange] = useState('Latest 14 entries');

    const rows: ObserverRow[] = [
        { name: 'OrderObserver', events: '43.5K', lagEvents: 364, throughput: '6.20k/min', lastError: '—', status: 'success' },
        { name: 'InvoiceReadModel', events: '29.0K', lagEvents: 20, throughput: '2.00k/min', lastError: '—', status: 'success' },
        { name: 'NotificationObserver', events: '1.5K', lagEvents: 85, throughput: '4.00k/min', lastError: '—', status: 'warning' },
        { name: 'CartObserver', events: '99.5K', lagEvents: 15, throughput: '10.12k/min', lastError: '—', status: 'success' },
        { name: 'RecommendModel', events: '2.5K', lagEvents: 22, throughput: '7.22k/min', lastError: 'Failed', status: 'danger' }
    ];

    const statusBody = (row: ObserverRow) => (
        <Tag
            value={row.status === 'success' ? 'Healthy' : row.status === 'warning' ? 'Lagging' : 'Failed'}
            severity={row.status}
            rounded
            className="text-xs"
        />
    );

    const nameBody = (row: ObserverRow) => (
        <div className="flex items-center gap-2">
            <span className={`h-2 w-2 rounded-full ${row.status === 'success' ? 'bg-green-400' : row.status === 'warning' ? 'bg-yellow-400' : 'bg-red-400'}`} />
            <span className="font-medium text-white">{row.name}</span>
        </div>
    );

    const eventsBody = (row: ObserverRow) => (
        <Tag value={row.events} severity="info" className="text-xs" />
    );

    return (
        <Card
            className={`shadow-lg h-full ${className ?? ''}`}
            pt={{
                root: { className: 'border border-gray-700/60' },
                body: { className: 'p-4' },
                content: { className: 'p-0' }
            }}>
            <div className="flex items-center justify-between mb-4">
                <div>
                    <h3 className="text-sm font-semibold text-white">Observer / Projections Health</h3>
                </div>
                <div className="flex items-center gap-2">
                    <Dropdown
                        value={timeRange}
                        options={['Latest 14 entries', 'Latest 50 entries', 'All']}
                        className="text-xs"
                        pt={{ root: { className: 'border-gray-700 text-xs' } }}
                    />
                    <Button icon={<MdAdd />} rounded text severity="secondary" size="small" />
                </div>
            </div>
            <DataTable
                value={rows}
                size="small"
                scrollable
                scrollHeight="220px"
                className="text-sm"
                pt={{
                    wrapper: { className: 'rounded-lg' }
                }}>
                <Column header="Status" body={nameBody} style={{ minWidth: '10rem' }} />
                <Column header="Events" body={eventsBody} style={{ minWidth: '6rem' }} />
                <Column field="lagEvents" header="Lag Events" style={{ minWidth: '7rem' }} />
                <Column field="throughput" header="Throughput" style={{ minWidth: '8rem' }} />
                <Column field="lastError" header="Last Error" style={{ minWidth: '7rem' }} />
                <Column header="" body={statusBody} style={{ minWidth: '6rem' }} />
            </DataTable>
        </Card>
    );
};
