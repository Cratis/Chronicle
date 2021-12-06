// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useRef, useEffect } from 'react';
import * as echarts from 'echarts';
import { default as styles } from './EventLogs.module.scss';
import { Histogram } from 'API/events/store/log/Histogram';

type EChartsOption = echarts.EChartsOption;

// https://echarts.apache.org/examples/en/
function getChartOption(dates: any, counts: any): EChartsOption {
    return {
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
            data: dates,
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
                data: counts,
                smooth: true,
                large: true
            } as any
        ]
    };
}

export interface EventHistogramProps {
    eventLog: string;
}

export const EventHistogram = (props: EventHistogramProps) => {
    const chartContainer = useRef<HTMLDivElement>(null);
    const getChart = () => echarts.getInstanceByDom(chartContainer.current!);
    const [entries, refreshEntries] = Histogram.use({ eventLogId: props.eventLog });
    const dates = entries.data.map(_ => {
        return echarts.format.formatTime('yyyy-MM-dd', _.date);
    });

    const counts = entries.data.map(_ => {
        return _.count;
    });

    if (chartContainer.current) {
        const chart = getChart();
        chart!.setOption(getChartOption(dates, counts));
    }

    useEffect(() => {
        if (chartContainer.current) {
            const chart = echarts.init(chartContainer.current);

            chart.resize();
        }

        const listener = () => getChart()!.resize();
        window.addEventListener('resize', listener);
        return () => window.removeEventListener('resize', listener);
    }, []);

    return (
        <div className={styles.eventSamplesContainer} ref={chartContainer} />
    );
};
