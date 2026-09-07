// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useMemo } from 'react';
import { Card } from 'Components/Card';
import { Chart } from 'Components/Chart';
import strings from 'Strings';
import { getThemeColors } from '../../../../Utilities/ThemeColors';
import css from './EventThroughputWidget.module.css';

/**
 * Props for {@link EventThroughputWidget}.
 */
export interface EventThroughputWidgetProps {
    /**
     * The rolling client-side samples of events appended since the previous sample,
     * oldest first.
     */
    samples: number[];
    /** Applied to the widget's root card. */
    className?: string;
}

/**
 * Shows the recent trend of event throughput as a line chart.
 *
 * There is no persisted throughput metric in Chronicle to chart against, so this plots a
 * bounded, session-scoped ring buffer of samples derived from polling the event log's tail
 * sequence number - see {@link DashboardTimeSeriesViewModel}.
 */
export const EventThroughputWidget = ({ samples, className }: EventThroughputWidgetProps) => {
    const themeColors = useMemo(() => getThemeColors(), []);
    const strings_ = strings.eventStore.namespaces.dashboard.eventThroughput;

    const chartData = {
        labels: samples.map((_, index) => `${samples.length - index}`),
        datasets: [
            {
                type: 'line' as const,
                label: strings_.title,
                data: samples,
                borderColor: themeColors.primaryColor,
                backgroundColor: themeColors.primaryColor,
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
            legend: { display: false }
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
            className={className ? `${css.eventThroughputWidget} ${className}` : css.eventThroughputWidget}
            header={<h3 className={css.title}>{strings_.title}</h3>}>
            {samples.length > 0
                ? <div className={css.chartContainer}>
                    <Chart type='line' data={chartData} options={chartOptions} className={css.chart} />
                </div>
                : <span className={css.empty}>{strings_.empty}</span>}
        </Card>
    );
};
