// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Button } from 'primereact/button';
import { Card } from 'primereact/card';
import { Column } from 'primereact/column';
import { DataTable } from 'primereact/datatable';
import { Dropdown } from 'primereact/dropdown';
import { Tag } from 'primereact/tag';
import { useState } from 'react';
import { MdRefresh, MdViewList, MdViewModule } from 'react-icons/md';

interface ActivityRow {
    eventType: string;
    streamId: string;
    stream: string;
    age: string;
    actionType: 'Observer' | 'Events';
}

export const RecentActivityWidget = ({ className }: { className?: string }) => {
    const [filter] = useState('All event types');

    const rows: ActivityRow[] = [
        { eventType: 'OrderObserver', streamId: '81fe-2746-4767', stream: '335 app', age: '5.2m ago', actionType: 'Observer' },
        { eventType: 'OrderToObserved', streamId: '96e-4570-4169', stream: '332 app', age: '1.2m ago', actionType: 'Events' },
        { eventType: 'InvoiceGenerated', streamId: '639-4140e-43100', stream: '8.5m ago', age: '9.2 ago', actionType: 'Events' },
        { eventType: 'PaymentProcessed', streamId: '638-4226-4169', stream: '10m ago', age: '9.2 ago', actionType: 'Events' },
        { eventType: 'OrderCancelled', streamId: '124-40268-4169', stream: '1.7m ago', age: '— ago', actionType: 'Events' },
        { eventType: 'Others', streamId: '602-40298-4368', stream: '5.3m ago', age: '— ago', actionType: 'Events' }
    ];

    const actionBody = (row: ActivityRow) => (
        <Tag
            value={row.actionType}
            severity={row.actionType === 'Observer' ? 'info' : 'secondary'}
            rounded
            className="text-xs"
        />
    );

    const eventTypeBody = (row: ActivityRow) => (
        <span className="font-medium text-white">{row.eventType}</span>
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
                    <h3 className="text-sm font-semibold text-white">Recent Activity</h3>
                    <span className="text-xs text-gray-500">Last updated 5m ago</span>
                </div>
                <div className="flex items-center gap-2">
                    <Button icon={<MdRefresh />} rounded text severity="secondary" size="small" />
                    <Button icon={<MdViewList />} rounded text severity="secondary" size="small" />
                    <Button icon={<MdViewModule />} rounded text severity="secondary" size="small" />
                    <Dropdown
                        value={filter}
                        options={['All event types', 'Observer only', 'Events only']}
                        className="text-xs"
                        pt={{ root: { className: 'border-gray-700 text-xs' } }}
                    />
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
                <Column header="Event Type" body={eventTypeBody} style={{ minWidth: '10rem' }} />
                <Column field="streamId" header="Stream ID" style={{ minWidth: '10rem' }} />
                <Column field="stream" header="Stream" style={{ minWidth: '7rem' }} />
                <Column field="age" header="" style={{ minWidth: '6rem' }} />
                <Column header="" body={actionBody} style={{ minWidth: '7rem' }} />
            </DataTable>
        </Card>
    );
};
