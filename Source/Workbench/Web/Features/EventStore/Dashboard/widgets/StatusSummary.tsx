// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Card } from 'primereact/card';
import { Tag } from 'primereact/tag';
import { ProgressBar } from 'primereact/progressbar';
import { MdBolt, MdCheckCircle, MdEventAvailable, MdPeople, MdStorage, MdTimer, MdWaves } from 'react-icons/md';

interface Metric {
    label: string;
    value: string;
    hint?: string;
    change?: string;
    icon: JSX.Element;
    iconBg: string;
    showBar?: boolean;
}

export const StatusSummary = () => {
    const metrics: Metric[] = [
        {
            label: 'Health',
            value: 'Healthy',
            icon: <MdCheckCircle className="text-green-400" size={26} />,
            iconBg: 'bg-green-900/40',
            showBar: true
        },
        {
            label: 'Events/sec',
            value: '73',
            change: '+1.2% / 5m',
            icon: <MdBolt className="text-cyan-400" size={26} />,
            iconBg: 'bg-cyan-900/40'
        },
        {
            label: 'Total Events',
            value: '49.2M',
            icon: <MdEventAvailable className="text-blue-400" size={26} />,
            iconBg: 'bg-blue-900/40'
        },
        {
            label: 'Streams',
            value: '74.8K',
            icon: <MdWaves className="text-indigo-400" size={26} />,
            iconBg: 'bg-indigo-900/40'
        },
        {
            label: 'Storage Used',
            value: '92.4 GB',
            hint: '+117 MB/day',
            icon: <MdStorage className="text-purple-400" size={26} />,
            iconBg: 'bg-purple-900/40'
        },
        {
            label: 'Last Append',
            value: '8s ago',
            icon: <MdTimer className="text-amber-400" size={26} />,
            iconBg: 'bg-amber-900/40'
        },
        {
            label: 'Active',
            value: '16',
            icon: <MdPeople className="text-teal-400" size={26} />,
            iconBg: 'bg-teal-900/40'
        }
    ];

    return (
        <div className="grid grid-cols-2 gap-3 md:grid-cols-4 xl:grid-cols-7">
            {metrics.map(metric => (
                <Card
                    key={metric.label}
                    className="shadow-lg"
                    pt={{
                        root: { className: 'border border-gray-700/60 bg-surface-900' },
                        body: { className: 'p-3' },
                        content: { className: 'p-0' }
                    }}>
                    <div className="flex items-center gap-3">
                        <div className={`flex h-11 w-11 shrink-0 items-center justify-center rounded-lg ${metric.iconBg}`}>
                            {metric.icon}
                        </div>
                        <div className="flex flex-col min-w-0">
                            <span className="text-xs text-gray-400 truncate">{metric.label}</span>
                            <div className="flex items-center gap-2">
                                <span className="text-lg font-bold text-white">{metric.value}</span>
                                {metric.showBar && (
                                    <ProgressBar
                                        value={100}
                                        showValue={false}
                                        className="h-1.5 w-10"
                                        pt={{ value: { className: 'bg-green-500' } }}
                                    />
                                )}
                            </div>
                            {metric.change && <Tag value={metric.change} severity="success" className="mt-0.5 text-xs w-fit" />}
                            {metric.hint && <span className="text-xs text-gray-500 truncate">{metric.hint}</span>}
                        </div>
                    </div>
                </Card>
            ))}
        </div>
    );
};
