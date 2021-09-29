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
import { useDataFrom } from './useDataFrom';
import { EventLogInformation } from './EventLogInformation';
import { EventType } from './EventType';
import { EventChartSelector } from './EventChartSelector';


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

// https://echarts.apache.org/examples/en/
export const EventLogs = () => {

    const [eventLogs, refreshEventLogs] = useDataFrom<ICommandBarItemProps>('/api/events/store/logs', (_: EventLogInformation) => {
        return {
            key: _.id,
            text: _.name
        } as ICommandBarItemProps
    });
    const [eventTypes, refreshEventTypes] = useDataFrom<ICommandBarItemProps>('/api/events/types', (_: EventType) => {
        return {
            key: _.id,
            text: _.name
        }
    });

    const [isOpen, { setTrue: openPanel, setFalse: dismissPanel }] = useBoolean(false);



    const commandBarItems: ICommandBarItemProps[] = [
        {
            key: 'eventLogs',
            name: 'Event Log',
            subMenuProps: {
                items: eventLogs
            }
        },
        {
            key: 'eventTypes',
            name: 'Event Types',
            subMenuProps: {
                items: eventTypes
            }
        },
        {
            key: 'filter',
            name: 'Filter',
            iconProps: { iconName: 'QueryList' }
        },
        {
            key: 'reload',
            name: 'Reload',
            iconProps: { iconName: 'Refresh' }
        },

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
    ];


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
            <EventChartSelector />
            <div className={styles.eventList}>
                <DetailsList
                    columns={eventListColumns}
                    items={events} selectionMode={SelectionMode.single}
                    onItemInvoked={eventSelected}
                />
            </div>

            <Panel
                isLightDismiss
                isOpen={isOpen}
                onDismiss={dismissPanel}
                headerText="Event Details"
            />
        </div>
    );
}
