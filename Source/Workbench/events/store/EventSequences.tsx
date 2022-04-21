// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import {
    CommandBar,
    ICommandBarItemProps,
    IconButton,
    IDropdownOption,
    Panel,
    Stack,
    IPivotItemProps,
    Pivot,
    PivotItem,
    TextField,
    Dropdown,
    IDropdownStyles
} from '@fluentui/react';

import { useBoolean } from '@fluentui/react-hooks';

import { EventHistogram } from './EventHistogram';
import { useState } from 'react';
import { FilterBuilder } from './FilterBuilder';
import { EventList } from './EventList';

import { EventSequenceInformation } from 'API/events/store/sequences/EventSequenceInformation';
import { AllEventSequences } from 'API/events/store/sequences/AllEventSequences';

import { FindFor, FindForArguments } from 'API/events/store/sequence/FindFor';
import { AppendedEvent } from 'API/events/store/sequence/AppendedEvent';
import { AllEventTypes } from 'API/events/store/types/AllEventTypes';
import { EventType } from 'API/events/store/types/EventType';
import { Microservice } from 'API/configuration/Microservice';
import { Microservices } from 'API/configuration/Microservices';
import { Tenant } from 'API/configuration/Tenant';
import { Tenants } from 'API/configuration/Tenants';
import { useEffect } from 'react';

function pivotItemHeaderRenderer(
    link?: IPivotItemProps,
    defaultRenderer?: (link?: IPivotItemProps) => JSX.Element | null,
): JSX.Element | null {
    if (!link || !defaultRenderer) {
        return null;
    }

    return (
        <span style={{ flex: '0 1 100%' }}>
            <IconButton iconProps={{ iconName: 'StatusCircleErrorX' }} title="Close query" onClick={() => alert('hello world')} />
            {defaultRenderer({ ...link, itemIcon: undefined })}
        </span>
    );
}

const commandBarDropdownStyles: Partial<IDropdownStyles> = { dropdown: { width: 200, marginLeft: 8, marginTop: 8 } };

export const EventSequences = () => {
    const [eventSequences] = AllEventSequences.use();
    const [microservices] = Microservices.use();
    const [tenants] = Tenants.use();

    const [selectedEventSequence, setSelectedEventSequence] = useState<EventSequenceInformation>();
    const [selectedMicroservice, setSelectedMicroservice] = useState<Microservice>();
    const [selectedTenant, setSelectedTenant] = useState<Tenant>();

    const [isDetailsPanelOpen, { setTrue: openPanel, setFalse: dismissPanel }] = useBoolean(false);
    const [isTimelineOpen, { toggle: toggleTimeline }] = useBoolean(false);
    const [isFilterOpen, { toggle: toggleFilter }] = useBoolean(false);

    const getFindForArguments = () => {
        return {
            eventSequenceId: selectedEventSequence?.id || undefined!,
            microserviceId: selectedMicroservice?.id || undefined!,
            tenantId: selectedTenant?.id || undefined
        } as FindForArguments;
    };

    const [events, refreshEvents] = FindFor.use(getFindForArguments());

    const [selectedEvent, setSelectedEvent] = useState<AppendedEvent | undefined>(undefined);
    const [selectedEventType, setSelectedEventType] = useState<EventType | undefined>(undefined);
    const [eventTypes] = AllEventTypes.use({
        microserviceId: selectedMicroservice?.id || undefined!,
    });

    const eventSequenceOptions = eventSequences.data.map(_ => {
        return {
            key: _.id,
            text: _.name
        } as IDropdownOption;
    });

    const microserviceOptions = microservices.data.map(_ => {
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
        if (microservices.data.length > 0) {
            setSelectedMicroservice(microservices.data[0]);
        }
    }, [microservices.data]);

    useEffect(() => {
        if (tenants.data.length > 0) {
            setSelectedTenant(tenants.data[0]);
        }
    }, [tenants.data]);

    useEffect(() => {
        if (selectedEventSequence && selectedMicroservice && selectedTenant) {
            refreshEvents(getFindForArguments());
        }
    }, [selectedEventSequence, selectedMicroservice, selectedTenant]);


    commandBarItems = [...commandBarItems, ...[
        // {
        //     key: 'timeline',
        //     name: 'Timeline',
        //     iconProps: { iconName: 'Timeline' },
        //     onClick: () => {
        //         toggleTimeline();
        //         if (isFilterOpen) toggleFilter();
        //     }
        // },
        // {
        //     key: 'filter',
        //     name: 'Filter',
        //     iconProps: { iconName: 'QueryList' },
        //     onClick: () => {
        //         toggleFilter();
        //         if (isTimelineOpen) toggleTimeline();
        //     }
        // },
        {
            key: 'eventSequences',
            name: 'Event Sequences',
            onRender: () => {
                return (
                    <Dropdown
                        styles={commandBarDropdownStyles}
                        options={eventSequenceOptions}
                        selectedKey={selectedEventSequence?.id}
                        onChange={(e, option) => {
                            setSelectedEventSequence(eventSequences.data.find(_ => _.id == option!.key));
                        }} />
                );
            }
        },
        {
            key: 'microservice',
            text: 'Microservice',
            onRender: () => {
                return (
                    <Dropdown
                        styles={commandBarDropdownStyles}
                        options={microserviceOptions}
                        selectedKey={selectedMicroservice?.id}
                        onChange={(e, option) => {
                            setSelectedMicroservice(microservices.data.find(_ => _.id == option!.key));
                        }} />
                );
            }
        },
        {
            key: 'tenant',
            text: 'Tenant',
            onRender: () => {
                return (
                    <Dropdown
                        styles={commandBarDropdownStyles}
                        options={tenantOptions}
                        selectedKey={selectedTenant?.id}
                        onChange={(e, option) => {
                            setSelectedTenant(tenants.data.find(_ => _.id == option!.key));
                        }} />
                );
            }
        },
        {
            key: 'run',
            text: 'Run',
            onClick: () => {
                refreshEvents(getFindForArguments());
            },
            iconProps: { iconName: 'Play' }
        }
    ] as ICommandBarItemProps[]];

    if (isTimelineOpen || isFilterOpen) {
        commandBarItems[commandBarItems.length - 1].split = true;
    }

    if (isTimelineOpen) {
        commandBarItems.push(
            {
                key: 'resetZoom',
                text: 'Reset Zoom',

                iconProps: { iconName: 'ZoomToFit' },
                onClick: () => {
                    // getChart().dispatchAction({
                    //     type: 'dataZoom',
                    //     start: 0,
                    //     end: 100
                    // });
                }
            }
        );
    }

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
            <Stack style={{ height: '100%' }}>
                {/* <Stack.Item>
                    <Stack horizontal style={{ textAlign: 'center' }}>
                        <Pivot linkFormat="links">
                            <PivotItem key="5c5af4ee-282a-456c-a53d-e3dee158a3be" headerText="Untitled" onRenderItemLink={pivotItemHeaderRenderer} />
                            <PivotItem key="b7a5f0a3-82d3-4170-a1e7-36034d763008" headerText="Good old query" onRenderItemLink={pivotItemHeaderRenderer} />
                        </Pivot>
                        <IconButton iconProps={{ iconName: 'Add' }} title="Add query" />
                    </Stack>
                </Stack.Item> */}
                <Stack.Item>
                    <CommandBar items={commandBarItems} />
                </Stack.Item>
                <Stack.Item>
                    {isTimelineOpen && <EventHistogram eventLog={selectedEventSequence!.id} />}
                    {isFilterOpen && <FilterBuilder />}
                </Stack.Item>
                <Stack.Item grow={1}>
                    <EventList items={events.data} eventTypes={eventTypes.data} onEventSelected={eventSelected} />
                </Stack.Item>
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
