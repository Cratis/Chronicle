// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useRef, useEffect } from 'react';
import * as echarts from 'echarts';
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

type EChartsOption = echarts.EChartsOption;

const dataCount = 42;
const data = generateData(dataCount);

const option: EChartsOption = {
    tooltip: {
        trigger: 'axis',
        axisPointer: {
            type: 'shadow'
        }
    },
    grid: {
        bottom: 90
    },
    dataZoom: [
        {
            type: 'inside'
        },
        {
            type: 'slider'
        }
    ],
    xAxis: {
        data: data.categoryData,
        silent: false,
        splitLine: {
            show: false
        },
        splitArea: {
            show: false
        }
    },
    yAxis: {
        splitArea: {
            show: false
        }
    },
    series: [
        {
            type: 'line',
            data: data.valueData,
            smooth: true,
            large: true
        } as any
    ]
};

function generateData(count: number) {
    let baseValue = Math.random() * 2;
    let time = +new Date(2011, 0, 1);
    let smallBaseValue: number;

    function next(idx: number) {
        smallBaseValue =
            idx % 30 === 0
                ? Math.random() * 10
                : smallBaseValue + Math.random() * 5 - 2;
        baseValue += Math.random() * 5 - 2;
        return Math.max(0, Math.round(baseValue + smallBaseValue) + 30);
    }

    const categoryData: any[] = [];
    const valueData: any[] = [];

    for (let i = 0; i < count; i++) {
        categoryData.push(
            echarts.format.formatTime('yyyy-MM-dd\nhh:mm:ss', time, false)
        );
        valueData.push(next(i).toFixed(2));
        time += 1000;
    }

    return {
        categoryData: categoryData,
        valueData: valueData
    };
}

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
    const chartContainer = useRef<HTMLDivElement>(null);
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

    const getChart = () => echarts.getInstanceByDom(chartContainer.current!);

    useEffect(() => {
        if (chartContainer.current) {
            const chart = echarts.init(chartContainer.current);
            chart.setOption(option);
            chart.resize();
        }
    }, []);

    useEffect(() => {
        const listener = () => {
            getChart().resize();
        };
        window.addEventListener('resize', listener);
        return () => window.removeEventListener('resize', listener);
    }, []);


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
            key: 'reload',
            name: 'Reload',
            iconProps: { iconName: 'Refresh' },
        },

        {
            key: 'resetZoom',
            text: 'Reset Zoom',
            iconProps: { iconName: 'ZoomToFit' },
            onClick: () => {
                getChart().dispatchAction({
                    type: 'dataZoom',
                    start: 0,
                    end: 100
                });
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
            <div className={styles.eventSamplesContainer} ref={chartContainer} />
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
