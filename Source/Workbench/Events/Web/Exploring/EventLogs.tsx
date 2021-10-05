// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.



import {
    CommandBar,
    DetailsList,
    IDetailsListStyles,
    IColumn,
    ICommandBarItemProps,
    Panel,
    SelectionMode,
    Stack
} from '@fluentui/react';

import {
    Pagination
} from '@fluentui/react-experiments';
import { useBoolean } from '@fluentui/react-hooks';

import { default as styles } from './EventLogs.module.scss';
import { useDataFrom } from '../useDataFrom';
import { EventLogInformation } from './EventLogInformation';
import { EventHistogram } from './EventHistogram';
import { Guid } from '@cratis/rudiments';
import { useState, useRef } from 'react';
import { FilterBuilder } from './FilterBuilder';


const eventListColumns: IColumn[] = [

    {
        key: 'sequence',
        name: 'Sequence',
        fieldName: 'sequence',
        minWidth: 100,
        maxWidth: 100
    },
    {
        key: 'name',
        name: 'Name',
        fieldName: 'name',
        minWidth: 200
    },
    {
        key: 'occurred',
        name: 'Occurred',
        fieldName: 'occurred',
        minWidth: 300
    }
];

const gridStyles: Partial<IDetailsListStyles> = {
    root: {
        overflowX: 'scroll',
        selectors: {
            '& [role=grid]': {
                display: 'flex',
                flexDirection: 'column',
                alignItems: 'start'
            },
        },
    },
    headerWrapper: {
        flex: '0 0 auto',
    },
    contentWrapper: {
        flex: '1 1 auto',
        overflowX: 'hidden',
        overflowY: 'auto',
        height: '300px'
    },
};

export const EventLogs = () => {
    const [isDetailsPanelOpen, { setTrue: openPanel, setFalse: dismissPanel }] = useBoolean(false);
    const [isTimelineOpen, { toggle: toggleTimeline }] = useBoolean(false);
    const [isFilterOpen, { toggle: toggleFilter }] = useBoolean(false);
    const [eventLog, setEventLog] = useState(Guid.empty.toString())
    const [eventLogs, refreshEventLogs] = useDataFrom<ICommandBarItemProps>('/api/events/store/logs', (_: EventLogInformation) => {
        return {
            key: _.id,
            text: _.name
        } as ICommandBarItemProps
    }, (data) => {
        if (data.length == 1) {
            setEventLog(data[0].key);
        }
    });
    const [events, refreshEvents] = useDataFrom(`/api/events/store/log/${eventLog}`);
    const [selectedEvent, setSelectedEvent] = useState(undefined);

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
            key: 'reload',
            name: 'Reload',
            iconProps: { iconName: 'Refresh' }
        },
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

    if (isFilterOpen) {
        commandBarItems.push(
            {
                key: 'run',
                text: 'Run',

                iconProps: { iconName: 'Play' }
            }
        );

    }

    const eventSelected = (item: any) => {
        if (item !== selectedEvent) {
            openPanel();
            setSelectedEvent(selectedEvent);
        }
    };

    const closePanel = () => {
        setSelectedEvent(undefined);
        dismissPanel();
    };

    return (
        <>
            <Stack className={styles.container}>
                <Stack.Item disableShrink>
                    <CommandBar items={commandBarItems} />
                </Stack.Item>
                <Stack.Item disableShrink>
                    {isTimelineOpen && <EventHistogram eventLog={eventLog} />}
                    {isFilterOpen && <FilterBuilder />}
                </Stack.Item>
                <Stack.Item grow>
                    <DetailsList
                        styles={gridStyles}
                        columns={eventListColumns}
                        items={events} selectionMode={SelectionMode.single}
                        onItemInvoked={eventSelected}
                    />
                </Stack.Item>
                <Stack.Item>
                    {/* https://codepen.io/micahgodbolt/pen/jXNLvB */}
                    <Pagination
                        format="buttons"
                        pageCount={4}
                        itemsPerPage={20} />
                </Stack.Item>
            </Stack>
            <Panel
                isLightDismiss
                isOpen={isDetailsPanelOpen}
                onDismiss={closePanel}
                headerText="Event Details"
            />
        </>
    );
};
