// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Box } from '@mui/material';
import { DataGrid, GridColDef } from '@mui/x-data-grid';
import { AllTenants } from 'API/configuration/tenants/AllTenants';

const columns: GridColDef[] = [
    {
        field: 'id',
        headerName: 'Id',
        width: 240,
    },
    {
        field: 'name',
        headerName: 'Name',
        width: 240,
    }
];


export const Tenants = () => {
    const [allTenants] = AllTenants.use();

    return (
        <Box sx={{ height: '100%', flex: 1 }}>
            <DataGrid
                columns={columns}
                filterMode="client"
                sortingMode="client"
                getRowId={row => row.id}
                rows={allTenants.data}
            />
        </Box>
    );
};
