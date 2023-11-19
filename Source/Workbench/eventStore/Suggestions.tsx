// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Box, Button, Divider, FormControl, Grid, InputLabel, MenuItem, Select, Stack, Toolbar, Typography } from '@mui/material';
import { DataGrid, GridColDef, GridRowSelectionModel, GridValueGetterParams } from '@mui/x-data-grid';
import { useRouteParams } from './RouteParams';
import { useEffect, useState } from 'react';
import { AllTenants } from 'API/configuration/tenants/AllTenants';
import { TenantInfo } from 'API/configuration/tenants/TenantInfo';
import { AllSuggestions } from 'API/events/store/suggestions/AllSuggestions';
import { SuggestionInformation } from 'API/events/store/suggestions/SuggestionInformation';
import * as icons from '@mui/icons-material';
import { Perform } from 'API/events/store/suggestions/Perform';
import { Ignore } from 'API/events/store/suggestions/Ignore';

const columns: GridColDef[] = [
    {
        headerName: 'Occurred',
        field: 'occurred',
        width: 200,
        valueGetter: (params: GridValueGetterParams<SuggestionInformation>) => {
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


export const Suggestions = () => {
    const { microserviceId } = useRouteParams();
    const [tenants] = AllTenants.use();
    const [performSuggestion, setPerformSuggestionCommandValues] = Perform.use();
    const [ignoreSuggestion, setIgnoreSuggestionCommandValues] = Ignore.use();
    const [selectedTenant, setSelectedTenant] = useState<TenantInfo>();
    const [selectedSuggestion, setSelectedSuggestion] = useState<SuggestionInformation>();
    const [suggestions] = AllSuggestions.use({
        microserviceId: microserviceId,
        tenantId: selectedTenant?.id ?? undefined!
    });

    useEffect(() => {
        if (tenants.data.length > 0) {
            setSelectedTenant(tenants.data[0]);
        }
    }, [tenants.data]);

    const suggestionSelected = (selection: GridRowSelectionModel) => {
        const suggestion = suggestions.data.find(_ => _.id == selection[0]);
        if (suggestion) {
            setSelectedSuggestion(suggestion);
        }
    };

    return (
        <>
            <Stack direction="column" style={{ height: '100%' }}>
                <Typography variant='h4'>Suggestions</Typography>
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

                    {selectedSuggestion &&
                        <>
                            <Button
                                startIcon={<icons.PlayArrow />}
                                onClick={() => {
                                    setPerformSuggestionCommandValues({
                                        microserviceId: microserviceId,
                                        tenantId: selectedTenant?.id,
                                        suggestionId: selectedSuggestion?.id,
                                    });
                                    performSuggestion.execute();
                                }}>Perform</Button>

                            <Button
                                startIcon={<icons.Remove />}
                                onClick={() => {
                                    setIgnoreSuggestionCommandValues({
                                        microserviceId: microserviceId,
                                        tenantId: selectedTenant?.id,
                                        suggestionId: selectedSuggestion?.id,
                                    });
                                    ignoreSuggestion.execute();
                                }}>Ignore</Button>
                        </>
                    }
                </Toolbar>

                <Box sx={{ height: '100%', flex: 1 }}>
                    <DataGrid
                        columns={columns}
                        filterMode="client"
                        sortingMode="client"
                        getRowId={(row: SuggestionInformation) => row.id}
                        onRowSelectionModelChange={suggestionSelected}
                        rows={suggestions.data}
                    />
                </Box>
            </Stack>
        </>
    )
};
