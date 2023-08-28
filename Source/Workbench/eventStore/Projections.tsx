// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState } from 'react';
import { AllProjections, AllProjectionsArguments } from 'API/events/store/projections/AllProjections';
import { Projection } from 'API/events/store/projections/Projection';
import { DataGrid, GridCallbackDetails, GridColDef, GridRowSelectionModel } from '@mui/x-data-grid';
import { useRouteParams } from './RouteParams';
import { Box, Divider, Stack, Typography } from '@mui/material';

const columns: GridColDef[] = [
    {
        headerName: 'Id',
        field: 'id',
        width: 250
    },
    {
        headerName: 'Name',
        field: 'name',
        width: 700
    },
    {
        headerName: 'Active',
        field: 'isActive',
        width: 400
    },
    {
        headerName: 'Model Name',
        field: 'modelName',
        width: 400
    },
];

export const Projections = () => {
    const { microserviceId } = useRouteParams();

    const getAllProjectionsArguments = () => {
        return {
            microserviceId
        } as AllProjectionsArguments;
    };

    const [projections, refreshProjections] = AllProjections.use(getAllProjectionsArguments());
    const [selected, setSelected] = useState<Projection>();

    const projectionSelected = (selectionModel: GridRowSelectionModel, details: GridCallbackDetails) => {
        const selectedItems = selectionModel.map(_ => projections.data.find(__ => __.id == _)) as Projection[];
        if (selectedItems.length > 0) {
            setSelected(selectedItems[0]);
        }
    };

    return (
        <Stack direction="column" style={{ height: '100%' }}>
            <Typography variant='h4'>Projections</Typography>
            <Divider sx={{ mt: 1, mb: 3 }} />

            <DataGrid
                columns={columns}
                filterMode="client"
                sortingMode="client"
                getRowId={row => row.id}
                rows={projections.data}
                onRowSelectionModelChange={projectionSelected} />
        </Stack>
    );
};
