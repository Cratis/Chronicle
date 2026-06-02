// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useRef, useEffect } from 'react';
import * as echarts from 'echarts';
import { ForSequence } from 'Api/EventSequences/ForSequence';

type EChartsOption = echarts.EChartsOption;

/* eslint-disable @typescript-eslint/no-explicit-any */

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
    eventStore: string;
    namespace: string;
    eventSequenceId: string;
    resolution?: string;
}

export const EventHistogram = (props: EventHistogramProps) => {
    const chartContainer = useRef<HTMLDivElement>(null);
    const getChart = () => echarts.getInstanceByDom(chartContainer.current!);

    const [histogram] = ForSequence.use({
        eventStore: props.eventStore,
        namespace: props.namespace,
        eventSequenceId: props.eventSequenceId,
        resolution: props.resolution ?? 'Hour',
    });

    const dates = histogram.data?.map(_ => _.occurred?.toISOString?.() ?? String(_.occurred)) ?? [];
    const counts = histogram.data?.map(_ => _.count) ?? [];

    if (chartContainer.current) {
        const chart = getChart();
        chart?.setOption(getChartOption(dates, counts));
    }

    useEffect(() => {
        if (chartContainer.current) {
            const chart = echarts.init(chartContainer.current);
            chart.resize();
        }

        const listener = () => getChart()?.resize();
        window.addEventListener('resize', listener);
        return () => window.removeEventListener('resize', listener);
    }, []);

    return <div className="w-full h-20" ref={chartContainer} />;
};
