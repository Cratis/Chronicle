// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import {
    ICommandBarItemProps,
    IDropdownOption,
    Panel,
    TextField,
} from '@fluentui/react';

import { useBoolean } from '@fluentui/react-hooks';

import { EventHistogram } from './EventHistogram';
import { useState } from 'react';
import { FilterBuilder } from './FilterBuilder';
import { EventList } from './EventList';

import { EventSequenceInformation } from 'API/events/store/sequences/EventSequenceInformation';
import { AllEventSequences } from 'API/events/store/sequences/AllEventSequences';

import { FindFor, FindForArguments } from 'API/events/store/sequence/FindFor';
import { AppendedEventWithJsonAsContent as AppendedEvent } from 'API/events/store/sequence/AppendedEventWithJsonAsContent';
import { AllEventTypes } from 'API/events/store/types/AllEventTypes';
import { EventTypeInformation } from 'API/events/store/types/EventTypeInformation';
import { TenantInfo } from 'API/configuration/tenants/TenantInfo';
import { AllTenants } from 'API/configuration/tenants/AllTenants';
import { useEffect } from 'react';
import { useRouteParams } from './RouteParams';
import { Button, FormControl, InputLabel, MenuItem, Select, Stack, Toolbar } from '@mui/material';
import * as icons from '@mui/icons-material';

export const EventSequences = () => {
    const { microserviceId } = useRouteParams();
    const [eventSequences] = AllEventSequences.use();
    const [tenants] = AllTenants.use();

    const [selectedEventSequence, setSelectedEventSequence] = useState<EventSequenceInformation>();
    const [selectedTenant, setSelectedTenant] = useState<TenantInfo>();

    const [isDetailsPanelOpen, { setTrue: openPanel, setFalse: dismissPanel }] = useBoolean(false);
    const [isTimelineOpen, { toggle: toggleTimeline }] = useBoolean(false);
    const [isFilterOpen, { toggle: toggleFilter }] = useBoolean(false);

    const getFindForArguments = () => {
        return {
            eventSequenceId: selectedEventSequence?.id || undefined!,
            microserviceId: microserviceId,
            tenantId: selectedTenant?.id || undefined
        } as FindForArguments;
    };

    const [events, refreshEvents] = FindFor.use(getFindForArguments());

    const [selectedEvent, setSelectedEvent] = useState<AppendedEvent | undefined>(undefined);
    const [selectedEventType, setSelectedEventType] = useState<EventTypeInformation | undefined>(undefined);

    const [eventTypes, refreshEventTypes] = AllEventTypes.use({
        microserviceId: microserviceId,
    });

    const eventSequenceOptions = eventSequences.data.map(_ => {
        return {
            key: _.id,
            text: _.name
        } as IDropdownOption;
    });

    const tenantOptions = tenants.data.map(_ => {
        return {
            key: _.id,
            text: _.name
        } as IDropdownOption;
    });

    let commandBarItems: ICommandBarItemProps[] = [];

    useEffect(() => {
        if (eventSequences.data.length > 0) {
            setSelectedEventSequence(eventSequences.data[0]);
        }
    }, [eventSequences.data]);

    useEffect(() => {
        if (tenants.data.length > 0) {
            setSelectedTenant(tenants.data[0]);
        }
    }, [tenants.data]);

    useEffect(() => {
        if (selectedEventSequence && selectedTenant) {
            refreshEvents(getFindForArguments());
        }
    }, [selectedEventSequence, selectedTenant]);

    const eventSelected = (item: any) => {
        if (item !== selectedEvent) {
            openPanel();
            setSelectedEvent(item);

            setSelectedEventType(eventTypes.data.find(_ => _.identifier == item.metadata.type.id));
        }
    };

    const closePanel = () => {
        setSelectedEvent(undefined);
        setSelectedEventType(undefined);
        dismissPanel();
    };

    return (
        <>
            <Stack direction="column" style={{ height: '100%' }}>
                {/* <Stack.Item>
                    <Stack horizontal style={{ textAlign: 'center' }}>
                        <Pivot linkFormat="links">
                            <PivotItem key="5c5af4ee-282a-456c-a53d-e3dee158a3be" headerText="Untitled" onRenderItemLink={pivotItemHeaderRenderer} />
                            <PivotItem key="b7a5f0a3-82d3-4170-a1e7-36034d763008" headerText="Good old query" onRenderItemLink={pivotItemHeaderRenderer} />
                        </Pivot>
                        <IconButton iconProps={{ iconName: 'Add' }} title="Add query" />
                    </Stack>
                </Stack.Item> */}
                {/* <CommandBar items={commandBarItems} /> */}
                <Toolbar>
                    <FormControl size="small" sx={{ m: 1, minWidth: 120 }}>
                        <InputLabel>Sequence</InputLabel>
                        <Select
                            label="Sequence"
                            autoWidth
                            value={selectedEventSequence?.id || ''}
                            onChange={e => setSelectedEventSequence(eventSequences.data.find(_ => _.id == e.target.value))}>

                            {eventSequences.data.map(sequence => {
                                return (
                                    <MenuItem key={sequence.id} value={sequence.id}>{sequence.name}</MenuItem>
                                );
                            })}
                        </Select>
                    </FormControl>

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


                    <Button startIcon={<icons.Timeline />} onClick={() => {
                        toggleTimeline();
                        if (isFilterOpen) toggleFilter();
                    }}>Timeline</Button>
                    <Button startIcon={<icons.FilterAlt />} onClick={() => {
                        toggleFilter();
                        if (isTimelineOpen) toggleTimeline();
                    }}>Filter</Button>
                    <Button startIcon={<icons.PlayArrow />} onClick={() => refreshEvents(getFindForArguments())}>Run</Button>

                    {isTimelineOpen &&
                        <Button startIcon={<icons.ZoomOutMap />} onClick={() => {
                        }}>Reset Zoom</Button>}



                </Toolbar>
                <div>
                    {isTimelineOpen && <EventHistogram eventLog={selectedEventSequence!.id} />}
                    {isFilterOpen && <FilterBuilder />}
                </div>
                <div>

                    <EventList items={events.data} eventTypes={eventTypes.data} onEventSelected={eventSelected} />
                </div>
            </Stack>
            <Panel
                isLightDismiss
                isOpen={isDetailsPanelOpen}
                onDismiss={closePanel}
                headerText={selectedEventType?.name}>
                <TextField label="Occurred" disabled defaultValue={new Date(selectedEvent?.context.occurred || new Date().toISOString()).toLocaleString()} />
                {
                    (selectedEvent && selectedEvent.content) && Object.keys(selectedEvent.content).map(_ => <TextField key={_} label={_} disabled defaultValue={selectedEvent!.content[_]} />)
                }
            </Panel>
        </>
    );
};
