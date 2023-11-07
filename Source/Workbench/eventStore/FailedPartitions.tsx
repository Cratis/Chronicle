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
import { Box, Button, Divider, Drawer, FormControl, Grid, InputLabel, Link, MenuItem, Select, Stack, TextField, Toolbar, Typography } from '@mui/material';
import { DataGrid, GridCallbackDetails, GridColDef, GridRowSelectionModel, GridValueGetterParams } from '@mui/x-data-grid';
import * as icons from '@mui/icons-material';
import { RetryPartition } from 'API/events/store/observers/RetryPartition';
import { GetObservers } from 'API/events/store/observers/GetObservers';
import { FailedPartitionAttempts } from './FailedPartitionAttempts';
import { Route, Routes, useParams } from 'react-router-dom';

let eventSequences: QueryResultWithState<EventSequenceInformation[]>;

const UnspecifiedObserverId = '00000000-0000-0000-0000-000000000000';

interface FailedPartitionsRouteParams extends RouteParams {
    observerId: string;
}

export const FailedPartitionsNavigator = () => {
    return (
        <Routes>
            <Route path=":observerId" element={<FailedPartitions />} />
            <Route path="*" element={<FailedPartitions />} />
        </Routes>
    )
}

export const FailedPartitions = () => {
    const { microserviceId, observerId } = useParams() as unknown as FailedPartitionsRouteParams;
    const [retryCommand, setRetryCommandValues] = RetryPartition.use();

    const [es] = AllEventSequences.use();
    eventSequences = es;
    const [tenants] = AllTenants.use();
    const [selectedTenant, setSelectedTenant] = useState<TenantInfo>();
    const [selectedFailedPartition, setSelectedFailedPartition] = useState<FailedPartition>();
    const [selectedFailedPartitionForAttempts, setSelectedFailedPartitionForAttempts] = useState<FailedPartition>();
    const [observers, setObserversQuery] = GetObservers.use({
        microserviceId: microserviceId,
        tenantId: selectedTenant?.id ?? undefined!
    });

    const [failedPartitions] = AllFailedPartitions.use({
        microserviceId: microserviceId,
        tenantId: selectedTenant?.id ?? undefined!,
        observerId: observerId ?? UnspecifiedObserverId
    });

    const [augmentedFailedPartitions, setAugmentedFailedPartitions] = useState<FailedPartition[]>([]);

    const columns: GridColDef[] = [
        {
            headerName: 'Observer Id',
            field: 'observerId',
            width: 300
        },
        {
            headerName: 'Observer Name',
            field: 'observerName',
            width: 350
        },
        {
            headerName: 'Attempts',
            field: 'attempts',
            width: 100,
            renderCell: (params) => {
                return (
                    <>
                        <Link style={{ cursor: 'pointer' }} onClick={() => {
                            setSelectedFailedPartitionForAttempts(params.row as FailedPartition);
                        }}>{params.row.attempts.length}</Link>
                    </>
                );
            }
        },
        {
            headerName: 'Partition',
            field: 'partition',
            width: 300,
            valueGetter: (params: GridValueGetterParams<FailedPartition>) => {
                return Object.values(params.row.partition.value).join('');
            }
        },
        {
            headerName: 'Sequence #',
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
        },
        {
            headerName: 'Last Attempt',
            field: 'lastAttempt',
            width: 250,
            valueGetter: (params: GridValueGetterParams<FailedPartition>) => {
                return params.row.attempts[params.row.attempts.length - 1].occurred.toLocaleString();
            }
        }
    ];

    useEffect(() => {
        if (tenants.data.length > 0) {
            setSelectedTenant(tenants.data[0]);
        }
    }, [tenants.data]);

    useEffect(() => {
        if (selectedTenant) {
            setObserversQuery({
                microserviceId: microserviceId,
                tenantId: selectedTenant!.id
            });
        }
    }, [selectedTenant]);

    const tenantChanged = (tenant: TenantInfo) => {
        setSelectedTenant(tenant);
    }

    const closePanel = () => {
        setSelectedFailedPartitionForAttempts(undefined);
    };

    const failedPartitionSelected = (selectionModel: GridRowSelectionModel, details: GridCallbackDetails) => {
        const selectedItems = selectionModel.map(_ => failedPartitions.data.find(__ => __.id == _)) as FailedPartition[];
        if (selectedItems.length > 0) {
            setSelectedFailedPartition(selectedItems[0]);
        }
    };

    useEffect(() => {
        setAugmentedFailedPartitions(failedPartitions.data.map(partition => {
            const observer = observers.data.find(_ => _.observerId == partition.observerId);
            if (observer) {
                (partition as any).observerName = observer.name;
            }
            return partition;
        }));
    }, [failedPartitions.data, observers.data]);

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
                                var partition = Object.values(selectedFailedPartition.partition.value).join('')
                                setRetryCommandValues({
                                    observerId: selectedFailedPartition.observerId,
                                    microserviceId: microserviceId,
                                    tenantId: selectedTenant?.id,
                                    partition: partition.toString(),
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
                        rows={augmentedFailedPartitions}
                        onRowSelectionModelChange={failedPartitionSelected}
                    />
                    <Drawer
                        anchor="right"
                        open={selectedFailedPartitionForAttempts != undefined} onClose={closePanel}
                        sx={{
                            '& .MuiDrawer-paper': { boxSizing: 'border-box', width: 500 },
                        }}>
                        <div style={{ padding: '24px' }}>
                            <Typography variant='h5'>Details</Typography>
                            {selectedFailedPartitionForAttempts && <FailedPartitionAttempts failedPartition={selectedFailedPartitionForAttempts} observer={observerForSelectedPartition} />}
                        </div>
                    </Drawer>
                </Box>
            </Stack>
        </>
    );
};
