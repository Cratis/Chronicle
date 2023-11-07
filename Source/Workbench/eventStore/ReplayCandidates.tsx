// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Box, Divider, FormControl, Grid, InputLabel, MenuItem, Select, Stack, Toolbar, Typography } from '@mui/material';
import { DataGrid, GridColDef, GridValueGetterParams } from '@mui/x-data-grid';
import { useRouteParams } from './RouteParams';
import { useEffect, useState } from 'react';
import { AllTenants } from 'API/configuration/tenants/AllTenants';
import { TenantInfo } from 'API/configuration/tenants/TenantInfo';
import { AllReplayCandidates } from 'API/events/store/observers/AllReplayCandidates';
import { ReplayCandidate } from 'API/events/store/observers/ReplayCandidate';

const columns: GridColDef[] = [
    {
        headerName: 'Occurred',
        field: 'occurred',
        width: 200,
        valueGetter: (params: GridValueGetterParams<ReplayCandidate>) => {
            return params.row.occurred.toLocaleString();
        }
    },
    {
        headerName: 'ObserverId',
        field: 'observerId',
        width: 300,
    },
    {
        headerName: 'Reasons',
        field: 'reasons',
        width: 300,
        valueGetter: (params: GridValueGetterParams<ReplayCandidate>) => {
            return params.row.reasons.map(_ => _.type).join(', ');

        }
    }

]


export const ReplayCandidates = () => {
    const { microserviceId } = useRouteParams();
    const [tenants] = AllTenants.use();
    const [selectedTenant, setSelectedTenant] = useState<TenantInfo>();
    const [replayCandidates] = AllReplayCandidates.use({
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
                <Typography variant='h4'>Replay candidates</Typography>
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
                        getRowId={(row: ReplayCandidate) => row.id}
                        rows={replayCandidates.data}
                    />
                </Box>

            </Stack>


        </>
    )
};
