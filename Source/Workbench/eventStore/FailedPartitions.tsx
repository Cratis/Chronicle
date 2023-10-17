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
import { RouteParams } from './RouteParams';
import { Box, Button, Divider, Drawer, FormControl, Grid, InputLabel, MenuItem, Select, Stack, TextField, Toolbar, Typography } from '@mui/material';
import { DataGrid, GridCallbackDetails, GridColDef, GridRowSelectionModel, GridValueGetterParams } from '@mui/x-data-grid';
import * as icons from '@mui/icons-material';
import { RetryPartition } from 'API/events/store/observers/RetryPartition';
import { GetObservers } from 'API/events/store/observers/GetObservers';
import { FailedPartitionAttempts } from './FailedPartitionAttempts';
import { useParams } from 'react-router-dom';

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
        field: 'attempts',
        width: 100,
        valueGetter: (params: GridValueGetterParams<FailedPartition>) => {
            return params.row.attempts.length;
        }
    },
    {
        headerName: 'Partition',
        field: 'partition',
        width: 250
    },
    {
        headerName: 'Sequence Number',
        field: 'sequenceNumber',
        width: 120,
        valueGetter: (params: GridValueGetterParams<FailedPartition>) => {
            return params.row.attempts[params.row.attempts.length - 1].sequenceNumber;
        }

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

interface FailedPartitionsRouteParams extends RouteParams {
    observerId: string;
}



export const FailedPartitions = () => {
    const { microserviceId, observerId } = useParams() as unknown as FailedPartitionsRouteParams;
    const [retryCommand, setRetryCommandValues] = RetryPartition.use();

    const [es] = AllEventSequences.use();
    eventSequences = es;
    const [tenants] = AllTenants.use();
    const [selectedTenant, setSelectedTenant] = useState<TenantInfo>();
    const [selectedFailedPartition, setSelectedFailedPartition] = useState<FailedPartition>();
    const [observers, setObserversQuery] = GetObservers.use({
        microserviceId: microserviceId,
        tenantId: selectedTenant?.id ?? undefined!
    });

    const [failedPartitions] = AllFailedPartitions.use({
        microserviceId: microserviceId,
        tenantId: selectedTenant?.id ?? undefined!
    });

    useEffect(() => {
        if (tenants.data.length > 0) {
            setSelectedTenant(tenants.data[0]);
        }
    }, [tenants.data]);

    const tenantChanged = (tenant: TenantInfo) => {
        setSelectedTenant(tenant);
        setObserversQuery({
            microserviceId: microserviceId,
            tenantId: tenant.id
        });
    }

    const closePanel = () => {
        setSelectedFailedPartition(undefined);
    };

    const failedPartitionSelected = (selectionModel: GridRowSelectionModel, details: GridCallbackDetails) => {
        const selectedItems = selectionModel.map(_ => failedPartitions.data.find(__ => __.id == _)) as FailedPartition[];
        if (selectedItems.length > 0) {
            setSelectedFailedPartition(selectedItems[0]);
        }
    };

    for (const partition of failedPartitions.data) {
        const observer = observers.data.find(_ => _.observerId == partition.observerId);
        if (observer) {
            (partition as any).observerName = observer.name;
        }
    }

    const observerForSelectedPartition = observers.data.find(_ => _.observerId == selectedFailedPartition?.observerId)!

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
                            onChange={e => tenantChanged(tenants.data.find(_ => _.id == e.target.value)!)}>

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
                                    partition: selectedFailedPartition.partition.value.toString(),
                                });
                                retryCommand.execute();
                            }}>Retry</Button>
                    }
                </Toolbar>

                <Box sx={{ height: '100%', flex: 1 }}>
                    <DataGrid
                        columns={columns}
                        filterMode="client"
                        sortingMode="client"
                        getRowId={row => row.id}
                        rows={failedPartitions.data}
                        onRowSelectionModelChange={failedPartitionSelected}
                    />
                    <Drawer
                        anchor="right"
                        open={selectedFailedPartition != undefined} onClose={closePanel}
                        sx={{
                            '& .MuiDrawer-paper': { boxSizing: 'border-box', width: 500 },
                        }}>
                        <div style={{ padding: '24px' }}>
                            <Typography variant='h5'>Details</Typography>
                            {selectedFailedPartition && <FailedPartitionAttempts failedPartition={selectedFailedPartition} observer={observerForSelectedPartition} />}
                        </div>
                    </Drawer>
                </Box>
            </Stack>
        </>
    );
};
