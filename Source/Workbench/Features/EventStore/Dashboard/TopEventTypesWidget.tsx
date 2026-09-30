// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useMemo } from 'react';
import { MdDataObject } from 'react-icons/md';
import type { ChartData, ChartOptions } from 'chart.js';
import { Chart } from 'Components/Chart';
import { EventTypeCount } from 'Features/Statistics';
import { themeColor, withAlpha } from './themeColor';
import { WidgetShell } from './WidgetShell';
import strings from 'Strings';

export interface ITopEventTypesWidget {

    /**
     * The per event type counts, ordered by the server from the most frequent, or undefined while loading.
     */
    perEventType: EventTypeCount[] | undefined;

    /**
     * Whether the counts could not be read.
     */
    failed?: boolean;

    /**
     * Additional class names.
     */
    className?: string;
}

const texts = strings.eventStore.dashboard;

/**
 * How many event types the widget names before folding the rest into "others".
 */
const shown = 6;

const palette = [
    ['--cratis-primary-color', '#60a5fa'],
    ['--cratis-green-500', '#22c55e'],
    ['--cratis-orange-500', '#f59e0b'],
    ['--cratis-purple-500', '#a855f7'],
    ['--cratis-cyan-500', '#06b6d4'],
    ['--cratis-pink-500', '#ec4899']
] as const;

/**
 * The event types that make up most of what is held.
 */
export const TopEventTypesWidget = ({ perEventType, failed, className }: ITopEventTypesWidget) => {
    const rows = useMemo(() => {
        const all = [...(perEventType ?? [])].map(_ => ({ name: _.eventType, count: Number(_.count) }));
        all.sort((left, right) => right.count - left.count);

        const top = all.slice(0, shown);
        const others = all.slice(shown).reduce((sum, _) => sum + _.count, 0);
        return others > 0 ? [...top, { name: texts.otherEventTypes, count: others }] : top;
    }, [perEventType]);

    const total = rows.reduce((sum, _) => sum + _.count, 0);
    const colors = useMemo(() => rows.map((_, index) => index < palette.length
        ? withAlpha(themeColor(palette[index][0], palette[index][1]), 1)
        : withAlpha(themeColor('--cratis-text-color-secondary', '#9ca3af'), 0.6)), [rows]);

    const data = useMemo<ChartData>(() => ({
        labels: rows.map(_ => _.name),
        datasets: [{ data: rows.map(_ => _.count), backgroundColor: colors, borderWidth: 0, hoverOffset: 6 }]
    }), [rows, colors]);

    const options = useMemo<ChartOptions>(() => ({
        responsive: true,
        maintainAspectRatio: false,
        animation: false,
        cutout: '68%',
        plugins: { legend: { display: false } }
    } as ChartOptions), []);

    const largest = rows.length > 0 ? Math.max(...rows.map(_ => _.count)) : 0;

    return (
        <WidgetShell
            title={texts.topEventTypes}
            icon={<MdDataObject />}
            tone='info'
            subtitle={failed ? texts.unavailable : perEventType === undefined ? texts.loading : texts.topEventTypesSubtitle.replace('{total}', total.toLocaleString())}
            className={className}>
            {(failed || perEventType === undefined) ? null : rows.length === 0
                ? <span className='dashboard-widget__empty'>{texts.noEvents}</span>
                : (
                    <div className='flex items-center gap-5'>
                        <div className='relative h-36 w-36 shrink-0'>
                            <Chart type='doughnut' data={data} options={options} />
                            <div className='pointer-events-none absolute inset-0 flex flex-col items-center justify-center'>
                                <span className='text-xl font-semibold'>{total === 0 ? '–' : `${Math.round((rows[0].count / total) * 100)}%`}</span>
                                <span className='text-xs' style={{ color: 'var(--dashboard-muted)' }}>{texts.topShare}</span>
                            </div>
                        </div>
                        <div className='dashboard-bars min-w-0 flex-1'>
                            {rows.map((row, index) => (
                                <div key={row.name}>
                                    <div className='dashboard-bar__label'>
                                        <span className='dashboard-bar__name flex items-center gap-2' title={row.name}>
                                            <span className='dashboard-dot' style={{ background: colors[index] }} aria-hidden='true' />
                                            {row.name}
                                        </span>
                                        <span className='dashboard-bar__count'>{row.count.toLocaleString()}</span>
                                    </div>
                                    <div className='dashboard-meter'>
                                        <div className='dashboard-meter__fill' style={{ width: `${largest === 0 ? 0 : (row.count / largest) * 100}%`, background: colors[index] }} />
                                    </div>
                                </div>
                            ))}
                        </div>
                    </div>
                )}
        </WidgetShell>
    );
};
