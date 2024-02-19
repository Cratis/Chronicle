// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import css from './EventHistogram.module.css';
import { useMockData } from './useMockData';
import { useRef, useEffect } from 'react';
import * as echarts from 'echarts';

type EChartsOption = echarts.EChartsOption;

function getChartOption(dates: any, counts: any): EChartsOption {
    return {
        tooltip: {
            show: false,
        },
        grid: {
            left: '-10',
            right: '10',
            top: '0',
            bottom: '0',
            containLabel: true,
        },
        dataZoom: [
            {
                type: 'inside',
            },
            {
                type: 'slider',
            },
        ],
        xAxis: {
            data: dates,
            show: false,
        },
        yAxis: {
            show: false,
        },
        series: [
            {
                type: 'line',
                data: counts,
                smooth: true,
                large: true,
                showSymbol: false,
                lineStyle: {
                    opacity: 0,
                },
                itemStyle: {
                    opacity: 0,
                },
            } as any,
        ],
    };
}

export interface EventHistogramProps {
    eventLog: string;
}

export const EventHistogram = (props: EventHistogramProps) => {
    const chartContainer = useRef<HTMLDivElement>(null);
    const getChart = () => echarts.getInstanceByDom(chartContainer.current!);
    const entries = useMockData(props.eventLog);

    const dates = entries?.data?.map((_: any) => {
        return _.date;
    });

    const counts = entries?.data?.map((_: any) => {
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

    return <div className={css.eventSamplesContainer} ref={chartContainer} />;
};
