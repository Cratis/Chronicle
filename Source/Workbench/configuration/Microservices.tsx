// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Box } from '@mui/material';
import { DataGrid, GridColDef } from '@mui/x-data-grid';
import { AllMicroservices } from 'API/configuration/microservices/AllMicroservices';

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

export const Microservices = () => {
    const [allMicroservices] = AllMicroservices.use();

    return (
        <Box sx={{ height: 400 }}>
            <DataGrid
                columns={columns}
                filterMode="client"
                sortingMode="client"
                getRowId={row => row.id}
                rows={allMicroservices.data}
            />
        </Box>
    );
};
