// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DataGrid, GridColDef, GridValueGetterParams } from '@mui/x-data-grid';
import { Box, Paper } from '@mui/material';
import { ConnectedClient } from 'API/clients/ConnectedClient';
import { AllConnectedClients } from 'API/clients/AllConnectedClients';

const columns: GridColDef[] = [
    {
        headerName: 'Id',
        field: 'connectionId',
        width: 250
    },
    {
        headerName: 'Client Uri',
        field: 'clientUri',
        width: 300
    },
    {
        headerName: 'Version',
        field: 'version',
        width: 300
    },
    {
        headerName: 'Last Seen',
        field: 'lastSeen',
        width: 300,
        valueGetter: (params: GridValueGetterParams<ConnectedClient>) => {
            return params.row.lastSeen.toLocaleString();
        }
    },
    {
        headerName: 'Debugger Attached',
        field: 'isRunningWithDebugger',
        width: 300
    }
];

export const ConnectedClients = () => {
    const [connectedClients] = AllConnectedClients.use();

    return (
        <Paper elevation={0} sx={{ height: '100%', padding: '24px' }}>
            <Box sx={{ height: '100%', flex: 1 }}>
                <DataGrid
                    columns={columns}
                    filterMode="client"
                    sortingMode="client"
                    getRowId={(row: ConnectedClient) => row.connectionId}
                    rows={connectedClients.data}
                />
            </Box>
        </Paper>
    );
};
