// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import {
    CommandBar,
    ICommandBarItemProps,
    IconButton,
    Panel,
    Stack,
    IPivotItemProps,
    Pivot,
    PivotItem,
    TextField
} from '@fluentui/react';

import { useBoolean } from '@fluentui/react-hooks';

import { default as styles } from './EventLogs.module.scss';
import { EventHistogram } from './EventHistogram';
import { Guid } from '@aksio/cratis-fundamentals';
import { useState } from 'react';
import { FilterBuilder } from './FilterBuilder';
import { EventList } from './EventList';

import { AllEventLogs } from 'API/events/store/logs/AllEventLogs';
import { FindFor } from 'API/events/store/log/FindFor';
import { AllEventTypes } from 'API/events/types/AllEventTypes';
import { AppendedEvent } from 'API/events/store/log/AppendedEvent';
import { EventType } from 'API/events/types/EventType';


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

export const EventLogs = () => {
    const [isDetailsPanelOpen, { setTrue: openPanel, setFalse: dismissPanel }] = useBoolean(false);
    const [isTimelineOpen, { toggle: toggleTimeline }] = useBoolean(false);
    const [isFilterOpen, { toggle: toggleFilter }] = useBoolean(false);
    const [eventLog, setEventLog] = useState(Guid.empty.toString());
    const [eventLogs, refreshEventLogs] = AllEventLogs.use();
    const [events, refreshEvents] = FindFor.use({ eventLogId: eventLog });
    const [selectedEvent, setSelectedEvent] = useState<AppendedEvent | undefined>(undefined);
    const [selectedEventType, setSelectedEventType] = useState<EventType | undefined>(undefined);
    const [eventTypes] = AllEventTypes.use();

    let commandBarItems: ICommandBarItemProps[] = [];

    if (eventLogs.data.length > 1) {
        commandBarItems.push(

            {
                key: 'eventLogs',
                name: 'Event Log',
                subMenuProps: {
                    items: eventLogs.data.map(_ => {
                        return {
                            key: _.id,
                            text: _.name
                        } as ICommandBarItemProps;
                    }),
                    onItemClick: (ev, item) => setEventLog(item?.key ?? Guid.empty.toString())
                }
            }
        );
    }

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
            key: 'run',
            text: 'Run',
            onClick: () => {
                refreshEvents({ eventLogId: eventLog });
            },
            iconProps: { iconName: 'Play' }
        }
    ]];

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
                    {isTimelineOpen && <EventHistogram eventLog={eventLog} />}
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
