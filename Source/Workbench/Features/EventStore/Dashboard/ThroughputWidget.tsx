// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useMemo, useState } from 'react';
import { MdShowChart } from 'react-icons/md';
import type { ChartData, ChartOptions } from 'chart.js';
import { Chart } from 'Components/Chart';
import { readThroughput } from './readers';
import { ThroughputRange } from './throughput';
import { themeColor, withAlpha } from './themeColor';
import { usePolling } from './usePolling';
import { WidgetShell } from './WidgetShell';
import strings from 'Strings';

export interface IThroughputWidget {

    /**
     * The event store to show throughput for.
     */
    eventStore: string;

    /**
     * The namespaces to add up.
     */
    namespaces: string[];

    /**
     * Additional class names.
     */
    className?: string;
}

const texts = strings.eventStore.dashboard;

const ranges: { range: ThroughputRange, label: string }[] = [
    { range: 'hour', label: texts.lastHour },
    { range: 'day', label: texts.lastDay },
    { range: 'week', label: texts.lastWeek }
];

const formatBucket = (from: Date, range: ThroughputRange): string => {
    switch (range) {
        case 'hour': return from.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
        case 'day': return from.toLocaleTimeString([], { hour: '2-digit' });
        case 'week': return from.toLocaleDateString([], { weekday: 'short', day: 'numeric' });
    }
};

/**
 * Events appended over time, read from the event log histogram so it shows what actually happened rather than only
 * what happened while the page was open.
 */
export const ThroughputWidget = ({ eventStore, namespaces, className }: IThroughputWidget) => {
    const [range, setRange] = useState<ThroughputRange>('hour');
    const namespacesKey = namespaces.join('|');
    const throughput = usePolling(
        () => readThroughput(eventStore, namespaces, range),
        range === 'hour' ? 5_000 : 30_000,
        [eventStore, namespacesKey, range]);

    const points = useMemo(() => throughput.value ?? [], [throughput.value]);
    const total = points.reduce((sum, _) => sum + _.count, 0);
    const latest = points.length > 0 ? points[points.length - 1].count : 0;

    const data = useMemo<ChartData>(() => {
        const color = themeColor('--cratis-primary-color', '#60a5fa');
        return {
            labels: points.map(_ => formatBucket(_.from, range)),
            datasets: [{
                label: texts.events,
                data: points.map(_ => _.count),
                borderColor: withAlpha(color, 1),
                backgroundColor: withAlpha(color, 0.18),
                fill: true,
                tension: 0.35,
                borderWidth: 2,
                pointRadius: 0,
                pointHoverRadius: 4
            }]
        };
    }, [points, range]);

    const options = useMemo<ChartOptions>(() => {
        const muted = themeColor('--cratis-text-color-secondary', '#9ca3af');
        const grid = withAlpha(themeColor('--cratis-surface-border', '#374151'), 0.6);
        return {
            responsive: true,
            maintainAspectRatio: false,
            animation: false,
            interaction: { mode: 'index', intersect: false },
            plugins: { legend: { display: false } },
            scales: {
                x: { grid: { display: false }, ticks: { color: muted, maxTicksLimit: 8, maxRotation: 0 } },
                y: { beginAtZero: true, grid: { color: grid }, ticks: { color: muted, precision: 0 } }
            }
        };
    }, []);

    const subtitle = throughput.value === undefined
        ? texts.loading
        : total === 0
            ? texts.throughputIdle
            : texts.throughputSummary.replace('{total}', total.toLocaleString()).replace('{latest}', latest.toLocaleString());

    return (
        <WidgetShell
            title={texts.eventThroughput}
            icon={<MdShowChart />}
            tone='info'
            subtitle={throughput.failed ? texts.unavailable : subtitle}
            className={className}
            action={
                <div className='dashboard-ranges' role='group' aria-label={texts.range}>
                    {ranges.map(_ => (
                        <button key={_.range} type='button' aria-pressed={range === _.range} onClick={() => setRange(_.range)}>
                            {_.label}
                        </button>
                    ))}
                </div>
            }>
            <div className='dashboard-chart'>
                <Chart type='line' data={data} options={options} />
            </div>
        </WidgetShell>
    );
};
