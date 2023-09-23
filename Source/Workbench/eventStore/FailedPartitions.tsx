// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useState, useMemo } from 'react';
import { AllFailedPartitions } from 'API/events/store/failed-partitions/AllFailedPartitions';
import { AllTenants } from 'API/configuration/tenants/AllTenants';
import { TenantInfo } from 'API/configuration/tenants/TenantInfo';
import { FailedPartition } from 'API/events/store/failed-partitions/FailedPartition';
import { AllEventSequences } from 'API/events/store/sequences/AllEventSequences';
import { EventSequenceInformation } from 'API/events/store/sequences/EventSequenceInformation';
import { QueryResultWithState } from '@aksio/applications/queries';
import { useRouteParams } from './RouteParams';
import { Box, Button, Divider, FormControl, Grid, InputLabel, MenuItem, Select, Stack, TextField, Toolbar, Typography } from '@mui/material';
import { DataGrid, GridCallbackDetails, GridColDef, GridRowSelectionModel, GridValueGetterParams } from '@mui/x-data-grid';
import { Label } from '@mui/icons-material';
import * as icons from '@mui/icons-material';
import { RetryPartition } from 'API/events/store/observers/RetryPartition';

let eventSequences: QueryResultWithState<EventSequenceInformation[]>;

const columns: GridColDef[] = [
    {
        headerName: 'Observer Id',
        field: 'observerId',
        width: 250
    },
    {
        headerName: 'Observer Name',
        field: 'observerName',
        width: 250
    },
    {
        headerName: 'Attempts',
        field: 'numberOfAttemptsOnSinceInitialized',
        width: 100
    },
    {
        headerName: 'Partition',
        field: 'partition',
        width: 250
    },
    {
        headerName: 'Sequence Number',
        field: 'currentError',
        width: 120
    },
    {
        headerName: 'Occurred',
        field: 'initialPartitionFailedOn',
        width: 250,
        valueGetter: (params: GridValueGetterParams<FailedPartition>) => {
            return params.row.attempts[0].occurred.toLocaleString();
        }
    }
];

export const FailedPartitions = () => {
    const { microserviceId } = useRouteParams();
    const [retryCommand, setRetryCommandValues] = RetryPartition.use();

    const [es] = AllEventSequences.use();
    eventSequences = es;
    const [tenants] = AllTenants.use();
    const [selectedTenant, setSelectedTenant] = useState<TenantInfo>();
    const [selectedFailedPartition, setSelectedFailedPartition] = useState<FailedPartition>();

    const [failedPartitions] = AllFailedPartitions.use({
        microserviceId: microserviceId,
        tenantId: selectedTenant?.id ?? undefined!
    });

    useEffect(() => {
        if (tenants.data.length > 0) {
            setSelectedTenant(tenants.data[0]);
        }
    }, [tenants.data]);

    const closePanel = () => {
        setSelectedFailedPartition(undefined);
    };

    const failedPartitionSelected = (selectionModel: GridRowSelectionModel, details: GridCallbackDetails) => {
        const selectedItems = selectionModel.map(_ => failedPartitions.data.find(__ => __.id == _)) as FailedPartition[];
        if (selectedItems.length > 0) {
            setSelectedFailedPartition(selectedItems[0]);
        }
    };

    return (
        <>
            <Stack direction="column" style={{ height: '100%' }}>
                <Typography variant='h4'>Failed partitions</Typography>
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

                    {selectedFailedPartition &&
                        <Button
                        startIcon={<icons.RestartAlt />}
                        onClick={() => {
                            setRetryCommandValues({
                                observerId: selectedFailedPartition.observerId,
                                microserviceId: microserviceId,
                                tenantId: selectedTenant?.id,
                                partitionId: selectedFailedPartition.partition,
                            });
                            retryCommand.execute();
                        }}>Retry</Button>
                    }
                </Toolbar>

                <Box sx={{ height: '100%', flex: 1 }}>
                    <Grid container spacing={2} sx={{ height: '100%' }}>
                        <Grid item xs={8}>
                            <DataGrid
                                columns={columns}
                                filterMode="client"
                                sortingMode="client"
                                getRowId={row => row.id}
                                rows={failedPartitions.data}
                                onRowSelectionModelChange={failedPartitionSelected}
                            />
                        </Grid>
                        <Grid item xs={4}>
                            <Box>
                                <Typography variant='h5'>Details</Typography>
                                {selectedFailedPartition &&
                                    <>

                                        {/* <Typography variant='h6'>{selectedFailedPartition?.observerName}</Typography> */}
                                        <FormControl size='small' sx={{ m: 1, minWidth: '90%' }}>
                                            <TextField label='Occurred' disabled
                                                defaultValue={(selectedFailedPartition?.attempts[0].occurred || new Date()).toISOString().toLocaleString()} />
                                        </FormControl>
                                        <Label>Messages</Label>
                                        {
                                            (selectedFailedPartition?.attempts[0].messages) && selectedFailedPartition.attempts[0].messages.map((value, index) => <TextField key={index} disabled defaultValue={value.toString()} title={value.toString()} />)
                                        }
                                        <FormControl size='small' sx={{ m: 1, minWidth: '90%' }}>
                                            <TextField label="Stack Trace" disabled defaultValue={selectedFailedPartition?.attempts[0].stackTrace} multiline title={selectedFailedPartition?.attempts[0].stackTrace.toString()} />
                                        </FormControl>
                                    </>}
                            </Box>
                        </Grid>
                    </Grid>
                </Box>
            </Stack>
        </>
    );
};
