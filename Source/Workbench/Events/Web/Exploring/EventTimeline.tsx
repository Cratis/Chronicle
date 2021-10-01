// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useRef, useEffect } from 'react';
import * as echarts from 'echarts';
import { default as styles } from './EventLogs.module.scss';

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
            echarts.time.format(time, 'yyyy-MM-dd\nhh:mm:ss',  false)
        );
        valueData.push(next(i).toFixed(2));
        time += 1000;
    }

    return {
        categoryData: categoryData,
        valueData: valueData
    };
}

export const EventTimeline = () => {
    const chartContainer = useRef<HTMLDivElement>(null);
    const getChart = () => echarts.getInstanceByDom(chartContainer.current!);

    useEffect(() => {
        if (chartContainer.current) {
            const chart = echarts.init(chartContainer.current);
            chart.setOption(option);
            chart.resize();
        }

        const listener = () => getChart().resize();
        window.addEventListener('resize', listener);
        return () => window.removeEventListener('resize', listener);
    }, []);

    return (
        <div className={styles.eventSamplesContainer} ref={chartContainer} />
    );
}
