// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Box, Button, Divider, FormControl, Grid, InputLabel, MenuItem, Select, Stack, Toolbar, Typography } from '@mui/material';
import { DataGrid, GridColDef, GridRowSelectionModel, GridValueGetterParams } from '@mui/x-data-grid';
import { useRouteParams } from './RouteParams';
import { useEffect, useState } from 'react';
import { AllTenants } from 'API/configuration/tenants/AllTenants';
import { TenantInfo } from 'API/configuration/tenants/TenantInfo';
import { AllRecommendations } from 'API/events/store/recommendations/AllRecommendations';
import { RecommendationInformation } from 'API/events/store/recommendations/RecommendationInformation';
import * as icons from '@mui/icons-material';
import { Perform } from 'API/events/store/recommendations/Perform';
import { Ignore } from 'API/events/store/recommendations/Ignore';

const columns: GridColDef[] = [
    {
        headerName: 'Occurred',
        field: 'occurred',
        width: 200,
        valueGetter: (params: GridValueGetterParams<RecommendationInformation>) => {
            return params.row.occurred.toLocaleString();
        }
    },
    {
        headerName: 'Name',
        field: 'name',
        width: 300,
    },
    {
        headerName: 'Description',
        field: 'description',
        width: 300,
    }
]


export const Recommendations = () => {
    const { microserviceId } = useRouteParams();
    const [tenants] = AllTenants.use();
    const [performRecommendation, setPerformRecommendationCommandValues] = Perform.use();
    const [ignoreRecommendation, setIgnoreRecommendationCommandValues] = Ignore.use();
    const [selectedTenant, setSelectedTenant] = useState<TenantInfo>();
    const [selectedRecommendation, setSelectedRecommendation] = useState<RecommendationInformation>();
    const [recommendations] = AllRecommendations.use({
        microserviceId: microserviceId,
        tenantId: selectedTenant?.id ?? undefined!
    });

    useEffect(() => {
        if (tenants.data.length > 0) {
            setSelectedTenant(tenants.data[0]);
        }
    }, [tenants.data]);

    const recommendationSelected = (selection: GridRowSelectionModel) => {
        const recommendation = recommendations.data.find(_ => _.id == selection[0]);
        if (recommendation) {
            setSelectedRecommendation(recommendation);
        }
    };

    return (
        <>
            <Stack direction="column" style={{ height: '100%' }}>
                <Typography variant='h4'>Recommendations</Typography>
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

                    {selectedRecommendation &&
                        <>
                            <Button
                                startIcon={<icons.PlayArrow />}
                                onClick={() => {
                                    setPerformRecommendationCommandValues({
                                        microserviceId: microserviceId,
                                        tenantId: selectedTenant?.id,
                                        recommendationId: selectedRecommendation?.id,
                                    });
                                    performRecommendation.execute();
                                }}>Perform</Button>

                            <Button
                                startIcon={<icons.Remove />}
                                onClick={() => {
                                    setIgnoreRecommendationCommandValues({
                                        microserviceId: microserviceId,
                                        tenantId: selectedTenant?.id,
                                        recommendationId: selectedRecommendation?.id,
                                    });
                                    ignoreRecommendation.execute();
                                }}>Ignore</Button>
                        </>
                    }
                </Toolbar>

                <Box sx={{ height: '100%', flex: 1 }}>
                    <DataGrid
                        columns={columns}
                        filterMode="client"
                        sortingMode="client"
                        getRowId={(row: RecommendationInformation) => row.id}
                        onRowSelectionModelChange={recommendationSelected}
                        rows={recommendations.data}
                    />
                </Box>
            </Stack>
        </>
    )
};
