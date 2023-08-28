// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Box, Divider, Grid, Paper, Stack, Typography } from '@mui/material';
import { EventDetails } from './eventStore/EventDetails';
import { EventList } from './eventStore/EventList';
import { AllEventTypesWithSchemas } from './API/events/store/types/AllEventTypesWithSchemas';
import { AppendedEventWithJsonAsContent as AppendedEvent } from 'API/events/store/sequence/AppendedEventWithJsonAsContent';
import { EventTypeWithSchemas } from 'API/events/store/types/EventTypeWithSchemas';
import { useRef, useState } from 'react';

export const Home = () => {

    const microserviceId = '12c737d2-e816-46f4-96fd-67fc1bf71086';
    const tenantId = '00000000-0000-0000-0000-000000000000';
    const eventSequenceId = 'cf3612a4-48fe-462a-af3e-2bd9ad6f6825';

    const [eventTypes] = AllEventTypesWithSchemas.use({
        microserviceId
    });

    const [selectedEvent, setSelectedEvent] = useState<AppendedEvent | undefined>(undefined);
    const [selectedEventType, setSelectedEventType] = useState<EventTypeWithSchemas | undefined>(undefined);
    const [schema, setSchema] = useState();

    const eventSelected = (item: AppendedEvent) => {
        if (item !== selectedEvent) {
            const eventType = eventTypes.data.find(_ => _.eventType.identifier == item.metadata.type.id);
            setSelectedEvent(item);
            setSelectedEventType(eventType);
            setSchema(eventTypes.data.find(_ => _.eventType.identifier == item.metadata.type.id)?.schemas.find(_ => _.generation == item.metadata.type.generation));
        }
    };

    const refreshEventsCallback = useRef<() => void>();
    function registerRefreshEventsCallback(callback: () => void) {
        refreshEventsCallback.current = callback;
    }

    return (
        <>
            <Paper elevation={0} sx={{ height: '100%', padding: '24px' }}>
                <Stack direction='column' style={{ height: '100%' }}>
                    <Typography variant='h4'>System events</Typography>
                    <Divider sx={{ mt: 1, mb: 3 }} />

                    <Box sx={{ height: '100%', flex: 1 }}>
                        <Grid container spacing={2} sx={{ height: '100%' }}>
                            <Grid item xs={8}>
                                <EventList
                                    registerRefreshEvents={registerRefreshEventsCallback}
                                    eventSequenceId={eventSequenceId}
                                    tenantId={tenantId}
                                    microserviceId={microserviceId}
                                    eventTypes={eventTypes.data.map(_ => _.eventType)} onEventSelected={eventSelected}
                                    onEventsRedacted={() => { }}
                                    sequenceNumber={eventSequenceId}
                                    canRedactEvents={false} />
                            </Grid>

                            <Grid item xs={4} sx={{ height: '100%' }}>
                                {selectedEvent &&
                                    <Box sx={{ height: '100%' }}>
                                        <Typography variant='h6'>{selectedEventType?.eventType.name}</Typography>
                                        <Typography>Occurred {selectedEvent?.context.occurred.toLocaleString()}</Typography>

                                        <EventDetails event={selectedEvent} type={selectedEventType!.eventType} schema={schema} />
                                    </Box>
                                }
                            </Grid>
                        </Grid>
                    </Box>
                </Stack>
            </Paper>
        </>
    );
};
