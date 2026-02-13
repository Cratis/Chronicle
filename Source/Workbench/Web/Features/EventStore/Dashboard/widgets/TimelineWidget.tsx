// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Card } from 'primereact/card';
import { Chart } from 'primereact/chart';
import { SelectButton } from 'primereact/selectbutton';
import { Tag } from 'primereact/tag';
import { useState } from 'react';

interface TimelineEvent {
    time: string;
    title: string;
    description: string;
    type: 'deploy' | 'config' | 'event' | 'error';
}

export const TimelineWidget = ({ className }: { className?: string }) => {
    const [view, setView] = useState('1h');
    const viewOptions = [
        { label: '1h', value: '1h' },
        { label: '2h', value: '2h' },
        { label: '6s', value: '6s' },
        { label: '10s', value: '10s' }
    ];

    const events: TimelineEvent[] = [
        { time: '11:15 AM', title: 'Config Update', description: 'AdminingmyApp.com', type: 'config' },
        { time: '12:30 PM', title: 'Deployed observer', description: 'INREACT METM...', type: 'deploy' }
    ];

    const labels = ['15:00', '10:38 AM', '11:16 AM', '1,460 Bil.', '11:18 AM', '13:50 PM', '13:00m', '12:10 PM'];

    const chartData = {
        labels,
        datasets: [
            {
                type: 'line' as const,
                label: 'Read Model Update',
                data: [null, null, 30, null, null, 45, null, null],
                borderColor: '#22d3ee',
                backgroundColor: '#22d3ee',
                pointRadius: 8,
                pointStyle: 'circle',
                showLine: false
            },
            {
                type: 'line' as const,
                label: 'Projection Replay',
                data: [null, 20, null, null, 35, null, null, 40],
                borderColor: '#a855f7',
                backgroundColor: '#a855f7',
                pointRadius: 8,
                pointStyle: 'circle',
                showLine: false
            },
            {
                type: 'line' as const,
                label: 'Dead Letter Events',
                data: [25, null, null, 28, null, null, 50, null],
                borderColor: '#f472b6',
                backgroundColor: '#f472b6',
                pointRadius: 8,
                pointStyle: 'circle',
                showLine: false
            },
            {
                type: 'line' as const,
                label: 'Syncs n migrat state',
                data: [null, null, null, null, null, 25, null, 30],
                borderColor: '#60a5fa',
                backgroundColor: '#60a5fa',
                pointRadius: 8,
                pointStyle: 'circle',
                showLine: false
            },
            {
                type: 'line' as const,
                label: 'Deducte',
                data: [15, null, 18, null, null, null, null, 22],
                borderColor: '#fbbf24',
                backgroundColor: '#fbbf24',
                pointRadius: 8,
                pointStyle: 'circle',
                showLine: false
            }
        ]
    };

    const chartOptions = {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
            legend: {
                display: true,
                position: 'bottom' as const,
                labels: {
                    color: '#9ca3af',
                    usePointStyle: true,
                    pointStyle: 'circle',
                    padding: 15,
                    font: { size: 10 }
                }
            }
        },
        scales: {
            x: {
                grid: { color: 'rgba(75, 85, 99, 0.3)' },
                ticks: { color: '#9ca3af', font: { size: 10 } }
            },
            y: {
                display: false
            }
        }
    };

    return (
        <Card
            className={`shadow-lg h-full ${className ?? ''}`}
            pt={{
                root: { className: 'border border-gray-700/60' },
                body: { className: 'p-4 h-full flex flex-col' },
                content: { className: 'p-0 flex-1 min-h-0 flex flex-col' }
            }}>
            <div className="flex items-center justify-between mb-3">
                <h3 className="text-sm font-semibold text-white">Timeline</h3>
                <SelectButton
                    value={view}
                    onChange={e => setView(e.value)}
                    options={viewOptions}
                    className="text-xs"
                    pt={{
                        root: { className: 'border-gray-700' },
                        button: { className: 'text-xs px-2 py-1' }
                    }}
                />
            </div>

            <div className="flex-1 min-h-0" style={{ height: '160px' }}>
                <Chart type="line" data={chartData} options={chartOptions} className="h-full w-full" />
            </div>

            <div className="mt-3 flex gap-3">
                {events.map(event => (
                    <div key={event.time} className="flex items-start gap-2 rounded-lg border border-gray-700/50 bg-slate-800/50 p-2">
                        <Tag value="Event" severity="info" className="text-xs" />
                        <div className="flex flex-col">
                            <div className="flex items-center gap-2">
                                <span className="text-sm font-medium text-white">{event.title}</span>
                                <span className="text-xs text-gray-500">{event.time}</span>
                            </div>
                            <span className="text-xs text-gray-400">{event.description}</span>
                        </div>
                    </div>
                ))}
            </div>
        </Card>
    );
};
