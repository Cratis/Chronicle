// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AllConnectedClients, ConnectedClientDetails } from 'Features/Clients';
import { Page } from 'Components/Common/Page';
import { Column } from '@cratis/components/DataTables';
import { Dropdown } from '@cratis/components/Dropdown';
import { DataTable } from 'Components/DataTable';
import { Toolbar } from 'primereact/toolbar';
import { useState } from 'react';
import strings from 'Strings';

enum ConnectedClientsView {
    Flat = 'flat',
    BySilo = 'bySilo'
}

export const ConnectedClients = () => {
    const [view, setView] = useState<ConnectedClientsView>(ConnectedClientsView.BySilo);

    const [result] = AllConnectedClients.use();
    const clients = result.data ?? [];

    const viewOptions = [
        { label: strings.connectedClients.views.bySilo, value: ConnectedClientsView.BySilo },
        { label: strings.connectedClients.views.flat, value: ConnectedClientsView.Flat }
    ];

    const lastSeenColumn = (client: ConnectedClientDetails) => <>{new Date(client.lastSeen).toLocaleString()}</>;

    const debuggerColumn = (client: ConnectedClientDetails) =>
        <>{client.isRunningWithDebugger ? strings.general.buttons.yes : strings.general.buttons.no}</>;

    const siloGroupHeaderTemplate = (client: ConnectedClientDetails) =>
        <span style={{ fontWeight: 'bold' }}>{strings.connectedClients.columns.server}: {client.siloAddress}</span>;

    return (
        <Page title={strings.connectedClients.title}>
            <Toolbar.Root className='mb-3'>
                <Toolbar.End>
                    <Dropdown<ConnectedClientsView>
                        value={view}
                        options={viewOptions}
                        optionLabel='label'
                        optionValue='value'
                        aria-label={strings.connectedClients.title}
                        onChange={event => event.value && setView(event.value)} />
                </Toolbar.End>
            </Toolbar.Root>

            {view === ConnectedClientsView.BySilo
                ? <DataTable
                    value={clients}
                    groupField='siloAddress'
                    groupHeaderTemplate={siloGroupHeaderTemplate}
                    dataKey='connectionId'
                    emptyMessage={strings.connectedClients.empty}
                    scrollable
                    scrollHeight='flex'>
                    <Column field='connectionId' header={strings.connectedClients.columns.connectionId} sortable />
                    <Column field='clientType' header={strings.connectedClients.columns.clientType} sortable />
                    <Column field='version' header={strings.connectedClients.columns.version} sortable />
                    <Column field='machineName' header={strings.connectedClients.columns.machineName} sortable />
                    <Column field='processId' header={strings.connectedClients.columns.processId} sortable />
                    <Column field='processPath' header={strings.connectedClients.columns.processPath} sortable />
                    <Column field='lastSeen' header={strings.connectedClients.columns.lastSeen} sortable body={lastSeenColumn} />
                    <Column field='isRunningWithDebugger' header={strings.connectedClients.columns.debugger} sortable body={debuggerColumn} />
                </DataTable>
                : <DataTable
                    value={clients}
                    dataKey='connectionId'
                    emptyMessage={strings.connectedClients.empty}
                    scrollable
                    scrollHeight='flex'>
                    <Column field='siloAddress' header={strings.connectedClients.columns.server} sortable />
                    <Column field='connectionId' header={strings.connectedClients.columns.connectionId} sortable />
                    <Column field='clientType' header={strings.connectedClients.columns.clientType} sortable />
                    <Column field='version' header={strings.connectedClients.columns.version} sortable />
                    <Column field='machineName' header={strings.connectedClients.columns.machineName} sortable />
                    <Column field='processId' header={strings.connectedClients.columns.processId} sortable />
                    <Column field='processPath' header={strings.connectedClients.columns.processPath} sortable />
                    <Column field='lastSeen' header={strings.connectedClients.columns.lastSeen} sortable body={lastSeenColumn} />
                    <Column field='isRunningWithDebugger' header={strings.connectedClients.columns.debugger} sortable body={debuggerColumn} />
                </DataTable>
            }
        </Page>
    );
};
