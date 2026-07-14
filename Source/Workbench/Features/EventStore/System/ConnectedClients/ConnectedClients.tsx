// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AllConnectedClients, ConnectedClient } from 'Api/Clients';
import { Page } from 'Components/Common/Page';
import { Column } from 'primereact/column';
import { DataTable, DataTableExpandedRows, DataTableValueArray } from 'primereact/datatable';
import { SelectButton } from 'primereact/selectbutton';
import { Toolbar } from 'primereact/toolbar';
import { useState } from 'react';
import strings from 'Strings';

enum ConnectedClientsView {
    Flat = 'flat',
    BySilo = 'bySilo'
}

export const ConnectedClients = () => {
    const [view, setView] = useState<ConnectedClientsView>(ConnectedClientsView.BySilo);
    const [expandedRows, setExpandedRows] = useState<DataTableValueArray | DataTableExpandedRows | undefined>(undefined);

    const [result] = AllConnectedClients.use();
    const clients = result.data ?? [];

    const viewOptions = [
        { label: strings.connectedClients.views.bySilo, value: ConnectedClientsView.BySilo },
        { label: strings.connectedClients.views.flat, value: ConnectedClientsView.Flat }
    ];

    const lastSeenColumn = (client: ConnectedClient) => <>{new Date(client.lastSeen).toLocaleString()}</>;

    const debuggerColumn = (client: ConnectedClient) =>
        <>{client.isRunningWithDebugger ? strings.general.buttons.yes : strings.general.buttons.no}</>;

    const siloGroupHeaderTemplate = (client: ConnectedClient) =>
        <span style={{ fontWeight: 'bold' }}>{strings.connectedClients.columns.server}: {client.siloAddress}</span>;

    return (
        <Page title={strings.connectedClients.title}>
            <Toolbar
                className='mb-3'
                end={
                    <SelectButton
                        value={view}
                        onChange={event => event.value && setView(event.value as ConnectedClientsView)}
                        options={viewOptions}
                        allowEmpty={false} />
                } />

            {view === ConnectedClientsView.BySilo
                ? <DataTable
                    value={clients}
                    rowGroupMode='subheader'
                    groupRowsBy='siloAddress'
                    sortMode='single'
                    sortField='siloAddress'
                    sortOrder={1}
                    expandableRowGroups
                    expandedRows={expandedRows}
                    onRowToggle={event => setExpandedRows(event.data)}
                    rowGroupHeaderTemplate={siloGroupHeaderTemplate}
                    dataKey='connectionId'
                    emptyMessage={strings.connectedClients.empty}
                    scrollable
                    scrollHeight='flex'>
                    <Column field='connectionId' header={strings.connectedClients.columns.connectionId} sortable />
                    <Column field='version' header={strings.connectedClients.columns.version} sortable />
                    <Column field='lastSeen' header={strings.connectedClients.columns.lastSeen} sortable body={lastSeenColumn} />
                    <Column field='isRunningWithDebugger' header={strings.connectedClients.columns.debugger} sortable body={debuggerColumn} />
                </DataTable>
                : <DataTable
                    value={clients}
                    sortMode='single'
                    sortField='siloAddress'
                    sortOrder={1}
                    dataKey='connectionId'
                    emptyMessage={strings.connectedClients.empty}
                    scrollable
                    scrollHeight='flex'>
                    <Column field='siloAddress' header={strings.connectedClients.columns.server} sortable />
                    <Column field='connectionId' header={strings.connectedClients.columns.connectionId} sortable />
                    <Column field='version' header={strings.connectedClients.columns.version} sortable />
                    <Column field='lastSeen' header={strings.connectedClients.columns.lastSeen} sortable body={lastSeenColumn} />
                    <Column field='isRunningWithDebugger' header={strings.connectedClients.columns.debugger} sortable body={debuggerColumn} />
                </DataTable>
            }
        </Page>
    );
};
