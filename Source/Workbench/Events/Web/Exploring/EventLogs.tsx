// Copyright (c) Cratis. All rights reserved.
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
import { useDataFrom } from '../useDataFrom';
import { EventLogInformation } from './EventLogInformation';
import { EventHistogram } from './EventHistogram';
import { Guid } from '@cratis/rudiments';
import { useState } from 'react';
import { FilterBuilder } from './FilterBuilder';
import { EventList } from './EventList';



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
    const [eventLogs, refreshEventLogs] = useDataFrom<ICommandBarItemProps>('/api/events/store/logs', (_: EventLogInformation) => {
        return {
            key: _.id,
            text: _.name
        } as ICommandBarItemProps;
    }, (data) => {
        if (data.length == 1) {
            setEventLog(data[0].key);
        }
    });
    const [events, refreshEvents] = useDataFrom(`/api/events/store/log/${eventLog}`);
    const [selectedEvent, setSelectedEvent] = useState<any>(undefined);

    let commandBarItems: ICommandBarItemProps[] = [];

    if (eventLogs.length > 1) {
        commandBarItems.push(

            {
                key: 'eventLogs',
                name: 'Event Log',
                subMenuProps: {
                    items: eventLogs,
                    onItemClick: (ev, item) => setEventLog(item?.key ?? Guid.empty.toString())
                }
            }
        );
    }

    commandBarItems = [...commandBarItems, ...[
        {
            key: 'timeline',
            name: 'Timeline',
            iconProps: { iconName: 'Timeline' },
            onClick: () => {
                toggleTimeline()
                if (isFilterOpen) toggleFilter();
            }
        },
        {
            key: 'filter',
            name: 'Filter',
            iconProps: { iconName: 'QueryList' },
            onClick: () => {
                toggleFilter();
                if (isTimelineOpen) toggleTimeline();
            }
        },
        {
            key: 'run',
            text: 'Run',

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
        }
    };

    const closePanel = () => {
        setSelectedEvent(undefined);
        dismissPanel();
    };

    return (
        <>
            <Stack>
                <Stack.Item disableShrink>
                    <Stack horizontal style={{ textAlign: 'center' }}>
                        <Pivot linkFormat="links">
                            <PivotItem key="5c5af4ee-282a-456c-a53d-e3dee158a3be" headerText="Untitled" onRenderItemLink={pivotItemHeaderRenderer} />
                            <PivotItem key="b7a5f0a3-82d3-4170-a1e7-36034d763008" headerText="Good old query" onRenderItemLink={pivotItemHeaderRenderer} />
                        </Pivot>
                        <IconButton iconProps={{ iconName: 'Add' }} title="Add query" />
                    </Stack>
                </Stack.Item>
                <Stack.Item disableShrink>
                    <CommandBar items={commandBarItems} />
                </Stack.Item>
                <Stack.Item disableShrink>
                    {isTimelineOpen && <EventHistogram eventLog={eventLog} />}
                    {isFilterOpen && <FilterBuilder />}
                </Stack.Item>
                <Stack.Item grow verticalFill>
                    <EventList items={events} onEventSelected={eventSelected} />
                </Stack.Item>
            </Stack>
            <Panel
                isLightDismiss
                isOpen={isDetailsPanelOpen}
                onDismiss={closePanel}
                headerText={selectedEvent?.name}>
                <TextField label="Occurred" disabled defaultValue={selectedEvent?.occurred} />
                {
                    (selectedEvent && selectedEvent.content) && Object.keys(selectedEvent.content).map(_ => <TextField key={_} label={_} disabled defaultValue={selectedEvent!.content[_]} />)
                }
            </Panel>
        </>
    );
};
