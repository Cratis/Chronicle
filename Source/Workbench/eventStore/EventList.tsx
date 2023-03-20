// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useState } from 'react';

import {
    AppendedEventWithJsonAsContent as AppendedEvent
} from 'API/events/store/sequence/AppendedEventWithJsonAsContent';
import { EventTypeInformation } from 'API/events/store/types/EventTypeInformation';
import {
    DataGrid,
    GridActionsCellItem,
    GridCallbackDetails,
    GridColDef,
    GridRowParams,
    GridRowSelectionModel,
    GridValueGetterParams
} from '@mui/x-data-grid';
import { Alert, AlertColor, Box, Snackbar } from '@mui/material';
import { RedactEvent } from 'API/events/store/sequence/RedactEvent';
import { RedactEvents } from 'API/events/store/sequence/RedactEvents';
import { useRouteParams } from './RouteParams';
import { ModalButtons, ModalResult, useModal } from '@aksio/cratis-mui';
import RedactEventModal from './RedactEventModal';
import GlobalEventTypes from '../GlobalEventTypes';
import RedactEventsModal from './RedactEventsModal';

export type EventSelected = (item: AppendedEvent) => void;

export interface EventListProps {
    items: AppendedEvent[];
    eventTypes: EventTypeInformation[];
    onEventSelected?: EventSelected;
    onEventsRedacted?: () => void;
    sequenceNumber?: string;
}

export const EventList = (props: EventListProps) => {

    const [redactEventCmd, setRedactEventCmd] = RedactEvent.use();
    const [redactEventsCmd, setRedactEventsCmd] = RedactEvents.use();
    const { microserviceId } = useRouteParams();
    const [snackBarState, setSnackBarState] = useState({
        open: false,
        message: '',
        severity: 'success' as AlertColor
    });


    const [showRedactEventModal, closeRedactEventModal] = useModal('Redact Event',
        ModalButtons.OkCancel,
        RedactEventModal,
        async (result, output) => {
            if (result === ModalResult.success && output) {
                if (output.content.reason.length === 0) {
                    openSnackBar('Reason is required', 'error');
                    return;
                }
                setRedactEventCmd({
                    microserviceId: microserviceId,
                    eventSequenceId: props.sequenceNumber,
                    tenantId: output.context.tenantId,
                    sequenceNumber: output.context.sequenceNumber,
                    reason: output.content.reason
                });
                const cmdResult = await redactEventCmd.execute();
                if (cmdResult.isSuccess) {
                    openSnackBar('Event redacted', 'success');
                    props.onEventsRedacted?.();
                }
            }
        }
    );
    const [showRedactEventsModal, closeRedactEventsModal] = useModal('Redact Events',
        ModalButtons.OkCancel,
        RedactEventsModal,
        async (result, output) => {
            if (result === ModalResult.success && output) {
                if (output.content.reason.length === 0) {
                    openSnackBar('Reason is required', 'error');
                    return;
                }
                setRedactEventsCmd({
                    microserviceId: microserviceId,
                    eventSequenceId: props.sequenceNumber,
                    tenantId: output.context.tenantId,
                    reason: output.content.reason,
                    eventSourceId: output.context.eventSourceId,
                    eventTypes: []
                });
                const cmdResult = await redactEventsCmd.execute();
                if (cmdResult.isSuccess) {
                    openSnackBar('Events redacted', 'success');
                    props.onEventsRedacted?.();
                }
            }
        }
    );
    const openSnackBar = (message: string, severity: AlertColor) => {
        setSnackBarState({
            open: true,
            message: message,
            severity: severity
        });
    };
    const handleCloseSnackBar = () => {
        setSnackBarState({
            ...snackBarState,
            open: false
        });
    };

    const redactEvent = (event: AppendedEvent) => {
        showRedactEventModal(event);
    };
    const redactAllWithSameEventSourceId = (event: AppendedEvent) => {
        showRedactEventsModal(event);
    };
    const redactAllEventTypesWithThisEventSourceId = (event: AppendedEvent) => {
        console.log('redact all event types with this event source id', event);
    };

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
                return params.row.context.occurred.toLocaleString();
            }
        },
        {
            headerName: 'Actions',
            field: 'actions',
            type: 'actions',
            width: 100,
            getActions: (params: GridRowParams) => {
                const disabled = params.row.metadata.type.id === GlobalEventTypes.redaction;
                return [
                    <GridActionsCellItem
                        key={1}
                        label='Redact this event'
                        showInMenu
                        disabled={disabled}
                        onClick={() => {
                            redactEvent(params.row as AppendedEvent);
                        }}
                    />,
                    <GridActionsCellItem
                        key={1}
                        label='Redact all with same event source ID'
                        showInMenu
                        onClick={() => {
                            redactAllWithSameEventSourceId(params.row as AppendedEvent);
                        }}
                    />
                    // <GridActionsCellItem
                    //     key={1}
                    //     label='Redact all event types with this event source ID'
                    //     showInMenu
                    //     onClick={() => {
                    //         redactAllEventTypesWithThisEventSourceId(params.row as AppendedEvent);
                    //     }}
                    // />

                ];

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
        <Box
            sx={{ height: 400 }}>
            <DataGrid
                columns={eventListColumns}
                filterMode='server'
                sortingMode='server'
                getRowId={(row) => row.metadata.sequenceNumber}
                onRowSelectionModelChange={eventTypeSelected}
                rows={props.items}
            />
            <Snackbar open={snackBarState.open} autoHideDuration={6000} onClose={handleCloseSnackBar}
                      anchorOrigin={{ vertical: 'top', horizontal: 'center' }}>
                <Alert onClose={handleCloseSnackBar} severity={snackBarState.severity} sx={{ width: '100%' }}>
                    {snackBarState.message}
                </Alert>
            </Snackbar>
        </Box>
    );
};
