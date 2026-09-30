// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useMemo } from 'react';
import { MdTimeline } from 'react-icons/md';
import type { ChartData, ChartOptions } from 'chart.js';
import { Chart } from 'Components/Chart';
import { ObserverHealth } from './health';
import { themeColor, withAlpha } from './themeColor';
import { useSampledHistory } from './useSampledHistory';
import { WidgetShell } from './WidgetShell';
import strings from 'Strings';

export interface IObserversOverTimeWidget {

    /**
     * The current observer figures, or undefined while loading.
     */
    observers: ObserverHealth | undefined;

    /**
     * Clears the history when it changes - the event store and namespace being shown.
     */
    scope: string;

    /**
     * Additional class names.
     */
    className?: string;
}

const texts = strings.eventStore.dashboard;

/**
 * How many observers are running, and how far behind they are, over the time the page has been open.
 */
export const ObserversOverTimeWidget = ({ observers, scope, className }: IObserversOverTimeWidget) => {
    const samples = useSampledHistory(observers, 5_000, 120, scope);

    const data = useMemo<ChartData>(() => {
        const active = themeColor('--cratis-green-500', '#22c55e');
        const behind = themeColor('--cratis-orange-500', '#f59e0b');
        return {
            labels: samples.map(_ => _.at.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' })),
            datasets: [
                {
                    label: texts.active,
                    data: samples.map(_ => _.value.active),
                    borderColor: withAlpha(active, 1),
                    backgroundColor: withAlpha(active, 0.12),
                    fill: true,
                    stepped: true,
                    borderWidth: 2,
                    pointRadius: 0,
                    yAxisID: 'observers'
                },
                {
                    label: texts.eventsBehind,
                    data: samples.map(_ => _.value.lag),
                    borderColor: withAlpha(behind, 1),
                    backgroundColor: withAlpha(behind, 0.12),
                    tension: 0.3,
                    borderWidth: 2,
                    pointRadius: 0,
                    yAxisID: 'lag'
                }
            ]
        };
    }, [samples]);

    const options = useMemo<ChartOptions>(() => {
        const muted = themeColor('--cratis-text-color-secondary', '#9ca3af');
        const grid = withAlpha(themeColor('--cratis-surface-border', '#374151'), 0.6);
        return {
            responsive: true,
            maintainAspectRatio: false,
            animation: false,
            interaction: { mode: 'index', intersect: false },
            plugins: { legend: { display: true, position: 'bottom', labels: { color: muted, boxWidth: 10, boxHeight: 10 } } },
            scales: {
                x: { grid: { display: false }, ticks: { color: muted, maxTicksLimit: 6, maxRotation: 0 } },
                observers: { position: 'left', beginAtZero: true, grid: { color: grid }, ticks: { color: muted, precision: 0 } },
                lag: { position: 'right', beginAtZero: true, grid: { display: false }, ticks: { color: muted, precision: 0 } }
            }
        };
    }, []);

    return (
        <WidgetShell
            title={texts.observersOverTime}
            icon={<MdTimeline />}
            tone='info'
            subtitle={observers === undefined
                ? texts.loading
                : texts.observersOverTimeSummary
                    .replace('{active}', observers.active.toLocaleString())
                    .replace('{total}', observers.total.toLocaleString())
                    .replace('{behind}', observers.lag.toLocaleString())}
            className={className}>
            <div className='dashboard-chart'>
                <Chart type='line' data={data} options={options} />
            </div>
        </WidgetShell>
    );
};
