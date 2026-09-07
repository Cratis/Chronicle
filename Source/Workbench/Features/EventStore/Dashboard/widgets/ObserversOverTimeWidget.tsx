// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useMemo } from 'react';
import { Card } from 'Components/Card';
import { Chart } from 'Components/Chart';
import strings from 'Strings';
import { getThemeColors } from '../../../../Utilities/ThemeColors';
import { type ObserverSample } from '../ObserverSample';
import css from './ObserversOverTimeWidget.module.css';

/**
 * Props for {@link ObserversOverTimeWidget}.
 */
export interface ObserversOverTimeWidgetProps {
    /** The rolling client-side samples of active vs. total observer counts, oldest first. */
    samples: ObserverSample[];
    /** Applied to the widget's root card. */
    className?: string;
}

/**
 * Shows the recent trend of active and total observer counts as a line chart.
 *
 * There is no persisted observer-state history in Chronicle to chart against, so this plots
 * a bounded, session-scoped ring buffer sampled from the live observer state breakdown - see
 * {@link DashboardTimeSeriesViewModel}.
 */
export const ObserversOverTimeWidget = ({ samples, className }: ObserversOverTimeWidgetProps) => {
    const themeColors = useMemo(() => getThemeColors(), []);
    const strings_ = strings.eventStore.namespaces.dashboard.observersOverTime;

    const chartData = {
        labels: samples.map((_, index) => `${samples.length - index}`),
        datasets: [
            {
                type: 'line' as const,
                label: strings_.total,
                data: samples.map(sample => sample.total),
                borderColor: themeColors.primaryColor,
                backgroundColor: themeColors.primaryColor,
                borderWidth: 2,
                pointRadius: 0,
                tension: 0.3
            },
            {
                type: 'line' as const,
                label: strings_.active,
                data: samples.map(sample => sample.active),
                borderColor: themeColors.green500,
                backgroundColor: themeColors.green500,
                borderWidth: 2,
                pointRadius: 0,
                tension: 0.3
            }
        ]
    };

    const chartOptions = {
        responsive: true,
        maintainAspectRatio: false,
        interaction: { mode: 'nearest' as const, intersect: false },
        plugins: {
            legend: {
                display: true,
                position: 'bottom' as const,
                labels: { color: themeColors.textColorSecondary, usePointStyle: true }
            }
        },
        scales: {
            x: { display: false },
            y: {
                beginAtZero: true,
                grid: { color: themeColors.surfaceBorder },
                ticks: { color: themeColors.textColorSecondary, precision: 0 }
            }
        }
    };

    return (
        <Card
            className={className ? `${css.observersOverTimeWidget} ${className}` : css.observersOverTimeWidget}
            header={<h3 className={css.title}>{strings_.title}</h3>}>
            {samples.length > 0
                ? <div className={css.chartContainer}>
                    <Chart type='line' data={chartData} options={chartOptions} className={css.chart} />
                </div>
                : <span className={css.empty}>{strings_.empty}</span>}
        </Card>
    );
};
