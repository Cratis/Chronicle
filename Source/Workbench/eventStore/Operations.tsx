// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Box, Divider, FormControl, Grid, InputLabel, MenuItem, Select, Stack, Toolbar, Typography } from '@mui/material';
import { DataGrid, GridColDef, GridValueGetterParams } from '@mui/x-data-grid';
import { useRouteParams } from './RouteParams';
import { useEffect, useState } from 'react';
import { AllTenants } from 'API/configuration/tenants/AllTenants';
import { TenantInfo } from 'API/configuration/tenants/TenantInfo';
import { AllOperations } from 'API/events/store/operations/AllOperations';
import { OperationInformation } from 'API/events/store/operations/OperationInformation';

const columns: GridColDef[] = [
    {
        headerName: 'Occurred',
        field: 'occurred',
        width: 200,
        valueGetter: (params: GridValueGetterParams<OperationInformation>) => {
            return params.row.occurred.toLocaleString();
        }
    },
    {
        headerName: 'Name',
        field: 'name',
        width: 300,
    },
    {
        headerName: 'Details',
        field: 'details',
        width: 300,
    }
]


export const Operations = () => {
    const { microserviceId } = useRouteParams();
    const [tenants] = AllTenants.use();
    const [selectedTenant, setSelectedTenant] = useState<TenantInfo>();
    const [operations] = AllOperations.use({
        microserviceId: microserviceId,
        tenantId: selectedTenant?.id ?? undefined!
    });

    useEffect(() => {
        if (tenants.data.length > 0) {
            setSelectedTenant(tenants.data[0]);
        }
    }, [tenants.data]);

    return (
        <>
            <Stack direction="column" style={{ height: '100%' }}>
                <Typography variant='h4'>Operations</Typography>
                <Divider sx={{ mt: 1, mb: 3 }} />

                <Toolbar>
                    <FormControl size="small" sx={{ m: 1, minWidth: 120 }}>
                        <InputLabel>Tenant</InputLabel>
                        <Select
                            label="Tenant"
                            autoWidth
                            value={selectedTenant?.id || ''}
                            onChange={e => setSelectedTenant(tenants.data.find(_ => _.id == e.target.value))}>

                            {tenants.data.map(tenant => {
                                return (
                                    <MenuItem key={tenant.id} value={tenant.id}>{tenant.name}</MenuItem>
                                );
                            })}
                        </Select>
                    </FormControl>
                </Toolbar>

                <Box sx={{ height: '100%', flex: 1 }}>
                    <DataGrid
                        columns={columns}
                        filterMode="client"
                        sortingMode="client"
                        getRowId={(row: OperationInformation) => row.id}
                        rows={operations.data}
                    />
                </Box>
            </Stack>
        </>
    )
};
