// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AllConnectedClients, ConnectedClient } from 'Api/Clients';
import { DataPage } from '@cratis/components/DataPage';
import { Page } from 'Components/Common/Page';
import { Column } from 'primereact/column';
import strings from 'Strings';

export const ConnectedClients = () => {
    const lastSeenColumn = (client: ConnectedClient) => {
        return <>{new Date(client.lastSeen).toLocaleString()}</>;
    };

    const debuggerColumn = (client: ConnectedClient) => {
        return <>{client.isRunningWithDebugger ? strings.general.buttons.yes : strings.general.buttons.no}</>;
    };

    return (
        <Page title={strings.connectedClients.title}>
            <DataPage
                title={strings.connectedClients.title}
                query={AllConnectedClients}
                emptyMessage={strings.connectedClients.empty}>

                <DataPage.Columns>
                    <Column field='siloAddress' header={strings.connectedClients.columns.server} sortable />
                    <Column field='connectionId' header={strings.connectedClients.columns.connectionId} sortable />
                    <Column field='version' header={strings.connectedClients.columns.version} sortable />
                    <Column field='lastSeen' header={strings.connectedClients.columns.lastSeen} sortable body={lastSeenColumn} />
                    <Column field='isRunningWithDebugger' header={strings.connectedClients.columns.debugger} sortable body={debuggerColumn} />
                </DataPage.Columns>
            </DataPage>
        </Page>
    );
};
