// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect } from 'react';

import { AppendedEventWithJsonAsContent as AppendedEvent } from 'API/events/store/sequence/AppendedEventWithJsonAsContent';
import { EventTypeInformation } from 'API/events/store/types/EventTypeInformation';
import { DataGrid, GridCallbackDetails, GridColDef, GridRowSelectionModel, GridValueGetterParams } from '@mui/x-data-grid';
import { Box } from '@mui/material';

export type EventSelected = (item: AppendedEvent) => void;

export interface EventListProps {
    items: AppendedEvent[];
    eventTypes: EventTypeInformation[];
    onEventSelected?: EventSelected;
}

export const EventList = (props: EventListProps) => {
    const eventListColumns: GridColDef[] = [

        {
            headerName: 'Sequence',
            field: 'metadata.sequenceNumber',
            width: 100,
            valueGetter: (params: GridValueGetterParams<AppendedEvent>) => {
                return params.row.metadata.sequenceNumber;
            }
        },
        {
            headerName: 'Name',
            field: 'metadata.type',
            width: 300,
            valueGetter: (params: GridValueGetterParams<AppendedEvent>) => {
                const eventType = props.eventTypes.find(_ => _.identifier == params.row.metadata.type.id);
                return eventType?.name || '[n/a]';
            }
        },
        {
            headerName: 'Occurred',
            field: 'context.occurred',
            width: 300,
            valueGetter: (params: GridValueGetterParams<AppendedEvent>) => {
                return new Date(params.row.context.occurred).toLocaleString();
            }
        }
    ];

    useEffect(() => {
        const detailsList = document.querySelector('.ms-DetailsList.eventList');
        if (detailsList) {
            detailsList.parentElement!.style!.height = '100%';
        }
    }, []);

    const eventTypeSelected = (selectionModel: GridRowSelectionModel, details: GridCallbackDetails) => {
        selectionModel.forEach((selected) => {
            const selectedEvent = props.items.find(_ => _.metadata.sequenceNumber == selected);
            props.onEventSelected?.(selectedEvent!);
        });
    };

    return (
        <Box sx={{ height: 400 }}>
            <DataGrid
                columns={eventListColumns}
                filterMode="server"
                sortingMode="server"
                getRowId={(row) => row.metadata.sequenceNumber}
                onRowSelectionModelChange={eventTypeSelected}
                rows={props.items}
            />
        </Box>
    );
};
