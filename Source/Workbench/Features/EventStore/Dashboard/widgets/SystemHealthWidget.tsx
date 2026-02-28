// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Card } from 'primereact/card';
import { Knob } from 'primereact/knob';
import { Tag } from 'primereact/tag';

export const SystemHealthWidget = ({ className }: { className?: string }) => {
    const metrics = [
        { label: 'Disk Remaining', value: 61, display: '6.90', suffix: 'GB', showKnob: true },
        { label: 'Index Health', value: 94, display: 'Healthy', severity: 'success' as const, showKnob: true },
        { label: 'Failed Partitions', display: '12', severity: 'warning' as const },
        { label: 'Dead Letter Events', display: '49', severity: 'danger' as const },
        { label: 'Retry Rate', display: '8', suffix: '/hr', severity: 'info' as const }
    ];

    return (
        <Card
            className={`shadow-lg h-full ${className ?? ''}`}
            pt={{
                root: { className: 'border border-gray-700/60' },
                body: { className: 'p-4' },
                content: { className: 'p-0' }
            }}>
            <h3 className="text-sm font-semibold text-white mb-4">System Health</h3>
            <div className="flex flex-col gap-4">
                {metrics.map(metric => (
                    <div key={metric.label} className="flex items-center justify-between">
                        <span className="text-sm text-gray-300">{metric.label}</span>
                        <div className="flex items-center gap-2">
                            {metric.showKnob ? (
                                <>
                                    <Knob
                                        value={metric.value}
                                        size={36}
                                        strokeWidth={6}
                                        readOnly
                                        valueColor={metric.value! > 80 ? '#22c55e' : metric.value! > 50 ? '#3b82f6' : '#ef4444'}
                                        rangeColor="#374151"
                                        textColor="transparent"
                                    />
                                    <Tag
                                        value={`${metric.display}${metric.suffix ?? ''}`}
                                        severity={metric.severity ?? 'info'}
                                        className="text-xs"
                                    />
                                </>
                            ) : (
                                <Tag
                                    value={`${metric.display}${metric.suffix ?? ''}`}
                                    severity={metric.severity ?? 'info'}
                                    className="text-xs font-semibold"
                                />
                            )}
                        </div>
                    </div>
                ))}
            </div>
        </Card>
    );
};
