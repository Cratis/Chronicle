// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Card } from 'primereact/card';
import { Chart } from 'primereact/chart';
import { SelectButton } from 'primereact/selectbutton';
import { useState } from 'react';

export const LagOverviewWidget = ({ className }: { className?: string }) => {
    const [timeRange, setTimeRange] = useState('1h');
    const timeOptions = [
        { label: '1h', value: '1h' },
        { label: '2h', value: '2h' },
        { label: '6h', value: '6h' },
        { label: '10s', value: '10s' }
    ];

    const labels = ['15:00', '12:10 AM', '4:00 AM', '5:50 PM', '6:00', '7:00 PM', '7:30', '1:00 AM'];

    const chartData = {
        labels,
        datasets: [
            {
                label: 'OrderObserver',
                data: [180, 200, 150, 120, 210, 230, 200, 180],
                borderColor: '#22d3ee',
                backgroundColor: 'rgba(34, 211, 238, 0.1)',
                tension: 0.4,
                fill: false,
                pointRadius: 3
            },
            {
                label: 'InvoiceReadModel',
                data: [100, 120, 140, 160, 180, 200, 150, 130],
                borderColor: '#f472b6',
                backgroundColor: 'rgba(244, 114, 182, 0.1)',
                tension: 0.4,
                fill: false,
                pointRadius: 3
            },
            {
                label: 'NotificationObserver',
                data: [80, 90, 100, 110, 90, 85, 95, 100],
                borderColor: '#fbbf24',
                backgroundColor: 'rgba(251, 191, 36, 0.1)',
                tension: 0.4,
                fill: false,
                pointRadius: 3
            }
        ]
    };

    const chartOptions = {
        responsive: true,
        maintainAspectRatio: false,
        interaction: {
            mode: 'index' as const,
            intersect: false
        },
        plugins: {
            legend: {
                display: true,
                position: 'bottom' as const,
                labels: {
                    color: '#9ca3af',
                    usePointStyle: true,
                    pointStyle: 'circle',
                    padding: 20
                }
            }
        },
        scales: {
            x: {
                grid: { color: 'rgba(75, 85, 99, 0.3)' },
                ticks: { color: '#9ca3af' }
            },
            y: {
                grid: { color: 'rgba(75, 85, 99, 0.3)' },
                ticks: { color: '#9ca3af' },
                beginAtZero: true
            }
        }
    };

    return (
        <Card
            className={`shadow-lg h-full ${className ?? ''}`}
            pt={{
                root: { className: 'border border-gray-700/60' },
                body: { className: 'p-4 h-full flex flex-col' },
                content: { className: 'p-0 flex-1 min-h-0' }
            }}>
            <div className="flex items-center justify-between mb-4">
                <div>
                    <h3 className="text-sm font-semibold text-white">Lag Overview</h3>
                </div>
                <SelectButton
                    value={timeRange}
                    onChange={e => setTimeRange(e.value)}
                    options={timeOptions}
                    className="text-xs"
                    pt={{
                        root: { className: 'border-gray-700' },
                        button: { className: 'text-xs px-2 py-1' }
                    }}
                />
            </div>
            <div className="flex-1 min-h-0" style={{ height: '220px' }}>
                <Chart type="line" data={chartData} options={chartOptions} className="h-full w-full" />
            </div>
        </Card>
    );
};
