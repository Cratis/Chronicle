// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Card } from 'primereact/card';
import { Chart } from 'primereact/chart';
import { Dropdown } from 'primereact/dropdown';
import { useState } from 'react';

interface EventTypeShare {
    name: string;
    percent: number;
    count: string;
    color: string;
}

export const EventTypeDistributionWidget = ({ className }: { className?: string }) => {
    const [timeRange] = useState('Last 24 hours');

    const shares: EventTypeShare[] = [
        { name: 'Order Placed', percent: 43.6, count: '1.2M', color: '#22d3ee' },
        { name: 'Invoice Generated', percent: 18.5, count: '810K', color: '#fb923c' },
        { name: 'Payment Processed', percent: 15.2, count: '680K', color: '#fbbf24' },
        { name: 'Order Cancelled', percent: 10.7, count: '480K', color: '#60a5fa' },
        { name: 'Others', percent: 12.0, count: '520K', color: '#f472b6' }
    ];

    const chartData = {
        labels: shares.map(s => s.name),
        datasets: [
            {
                data: shares.map(s => s.percent),
                backgroundColor: shares.map(s => s.color),
                borderWidth: 0,
                hoverOffset: 8
            }
        ]
    };

    const chartOptions = {
        cutout: '60%',
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
            legend: { display: false }
        }
    };

    return (
        <Card
            className={`shadow-lg h-full ${className ?? ''}`}
            pt={{
                root: { className: 'border border-gray-700/60' },
                body: { className: 'p-4' },
                content: { className: 'p-0' }
            }}>
            <div className="flex items-center justify-between mb-4">
                <div>
                    <h3 className="text-sm font-semibold text-white">Event Type Distribution</h3>
                    <span className="text-xs text-gray-500">Top Event Types</span>
                </div>
                <Dropdown
                    value={timeRange}
                    options={['Last 24 hours', 'Last 7 days', 'Last 30 days']}
                    className="text-xs"
                    pt={{ root: { className: 'border-gray-700 text-xs' } }}
                />
            </div>

            <div className="flex items-center gap-6">
                <div className="relative h-44 w-44 shrink-0">
                    <Chart type="doughnut" data={chartData} options={chartOptions} className="h-full w-full" />
                    <div className="absolute inset-0 flex flex-col items-center justify-center">
                        <span className="text-2xl font-bold text-white">43.6%</span>
                        <span className="text-xs text-gray-400">Top #1</span>
                    </div>
                </div>

                <div className="flex flex-col gap-2 flex-1">
                    {shares.map(share => (
                        <div key={share.name} className="flex items-center justify-between text-sm">
                            <div className="flex items-center gap-2">
                                <span className="h-2.5 w-2.5 rounded-full" style={{ backgroundColor: share.color }} />
                                <span className="text-gray-300">{share.name}</span>
                            </div>
                            <span className="font-medium text-white">{share.percent}%</span>
                        </div>
                    ))}
                </div>
            </div>
        </Card>
    );
};
