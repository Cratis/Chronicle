// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.



import {
    CommandBar,
    DetailsList,
    IColumn,
    ICommandBarItemProps,
    Panel,
    SelectionMode
} from '@fluentui/react'
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
        commandBarItems[commandBarItems.length-1].split = true;
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

    const events: any[] = [
        {
            sequence: 1,
            name: 'DebitAccountOpened',
            occurred: new Date(2021, 9, 27, 12, 0).toString()
        },
        {
            sequence: 2,
            name: 'DepositToDebitAccountPerformed',
            occurred: new Date(2021, 9, 27, 12, 0).toString()
        }
    ];

    const eventSelected = (item: any) => {
        openPanel();
    };

    return (
        <div className={styles.container}>
            <div className={styles.commandBar}>
                <CommandBar items={commandBarItems} />
            </div>
            {isTimelineOpen && <EventHistogram eventLog={eventLog} />}
            {isFilterOpen && <FilterBuilder />}
            <div className={styles.eventList}>
                <DetailsList
                    columns={eventListColumns}
                    items={events} selectionMode={SelectionMode.single}
                    onItemInvoked={eventSelected}
                />
            </div>

            <Panel
                isLightDismiss
                isOpen={isDetailsPanelOpen}
                onDismiss={dismissPanel}
                headerText="Event Details"
            />
        </div>
    );
}
