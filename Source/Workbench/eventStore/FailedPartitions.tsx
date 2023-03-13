// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import {
    IDropdownStyles,
    Label,
    Selection,
    SelectionMode,
    Panel,
    TextField
} from '@fluentui/react';
import { useEffect, useState, useMemo } from 'react';
import { AllFailedPartitions } from 'API/events/store/failed-partitions/AllFailedPartitions';
import { AllTenants } from 'API/configuration/tenants/AllTenants';
import { TenantInfo } from 'API/configuration/tenants/TenantInfo';
import { RecoverFailedPartitionState } from 'API/events/store/failed-partitions/RecoverFailedPartitionState';
import { useBoolean } from '@fluentui/react-hooks';
import { AllEventSequences } from 'API/events/store/sequences/AllEventSequences';
import { EventSequenceInformation } from 'API/events/store/sequences/EventSequenceInformation';
import { QueryResultWithState } from '@aksio/cratis-applications-frontend/queries';
import { useRouteParams } from './RouteParams';
import { Box, FormControl, InputLabel, MenuItem, Select, Stack, Toolbar } from '@mui/material';
import { DataGrid, GridColDef, GridValueGetterParams } from '@mui/x-data-grid';


const commandBarDropdownStyles: Partial<IDropdownStyles> = { dropdown: { width: 200, marginLeft: 8, marginTop: 8 } };

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
        headerName: 'Event Sequence',
        field: 'eventSequenceId',
        width: 120,
        valueGetter: (params: GridValueGetterParams<RecoverFailedPartitionState>) => {
            return eventSequences.data.find(_ => _.id == params.row.eventSequenceId)?.name ?? params.row.id;
        }
    },
    {
        headerName: 'Occurred',
        field: 'initialPartitionFailedOn',
        width: 250,
        valueGetter: (params: GridValueGetterParams<RecoverFailedPartitionState>) => {
            return params.row.initialPartitionFailedOn ? params.row.initialPartitionFailedOn.toLocaleString() : '';
        }
    }
];

export const FailedPartitions = () => {
    const { microserviceId } = useRouteParams();

    const [es] = AllEventSequences.use();
    eventSequences = es;
    const [tenants] = AllTenants.use();
    const [selectedTenant, setSelectedTenant] = useState<TenantInfo>();
    const [isDetailsPanelOpen, { setTrue: openPanel, setFalse: dismissPanel }] = useBoolean(false);
    const [selectedItem, setSelectedItem] = useState<RecoverFailedPartitionState>();

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
        setSelectedItem(undefined);
        dismissPanel();
    };

    const selection = useMemo(
        () => new Selection({
            selectionMode: SelectionMode.single,
            onSelectionChanged: () => {
                const selected = selection.getSelection();
                if (selected.length === 1) {
                    setSelectedItem(selected[0] as RecoverFailedPartitionState);
                    openPanel();
                }
            },
            items: failedPartitions.data as any[]
        }), [failedPartitions.data]);


    return (
        <>
            <Stack direction="column" style={{ height: '100%' }}>
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

                <Box sx={{ height: 400 }}>
                    <DataGrid
                        columns={columns}
                        filterMode="client"
                        sortingMode="client"
                        getRowId={row => row.id}
                        rows={failedPartitions.data}
                    />
                </Box>
            </Stack>
            <Panel
                isLightDismiss
                isOpen={isDetailsPanelOpen}
                onDismiss={closePanel}
                headerText={selectedItem?.observerName}>
                <TextField label="Occurred" disabled defaultValue={selectedItem?.initialPartitionFailedOn.toLocaleDateString() ?? new Date().toLocaleString()} />
                <Label>Messages</Label>
                {
                    (selectedItem?.messages) && selectedItem.messages.map((value, index) => <TextField key={index} disabled defaultValue={value.toString()} title={value.toString()} />)
                }
                <TextField label="Stack Trace" disabled defaultValue={selectedItem?.stackTrace} multiline title={selectedItem?.stackTrace.toString()} />
                {/* {

                    (selectedItem) && Object.keys(selectedItem).map(_ => <TextField key={_} label={_} disabled defaultValue={selectedItem![_]} />)
                } */}
            </Panel>
        </>

    );
};
