// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as echarts from 'echarts';

import { default as styles } from './EventLog.module.scss';
import { useRef, useEffect, useState } from 'react';

type EChartsOption = echarts.EChartsOption;

const dataCount = 42;
const data = generateData(dataCount);

const option: EChartsOption = {
    title: {
        text: echarts.format.addCommas(dataCount) + ' Data',
        left: 10
    },
    responsive: true,
    maintainAspectRation: false,
    toolbox: {
        feature: {
            dataZoom: {
                yAxisIndex: false
            },
            saveAsImage: {
                pixelRatio: 2
            }
        }
    },
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
        },
    },
    series: [
        {
            type: 'line',
            data: data.valueData,
            smooth: true,
            // Set `large` for large data amount
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

// https://echarts.apache.org/examples/en/
export const EventLog = () => {
    const chartContainer = useRef<HTMLDivElement>(null);

    useEffect(() => {
        if (chartContainer.current) {
            const chart = echarts.init(chartContainer.current);
            chart.setOption(option);
            chart.resize();
        }
    }, []);

    useEffect(() => {
        const listener = () => {
            if (chartContainer.current) {
                const chart = echarts.getInstanceByDom(chartContainer.current);
                chart.resize();
            }
        };
        window.addEventListener('resize', listener);
        return () => window.removeEventListener('resize', listener);
    }, []);

    return (
        <div className={styles.eventSamplesContainer} ref={chartContainer}>
        </div>
    );
}
