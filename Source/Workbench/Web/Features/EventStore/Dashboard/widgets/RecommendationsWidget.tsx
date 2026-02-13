// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Button } from 'primereact/button';
import { Card } from 'primereact/card';
import { MdCheckCircle, MdChevronRight, MdError, MdWarning } from 'react-icons/md';

interface Recommendation {
    title: string;
    detail: string;
    severity: 'success' | 'warning' | 'error';
}

export const RecommendationsWidget = ({ className }: { className?: string }) => {
    const items: Recommendation[] = [
        {
            title: 'Stream order-839293 has 2.1M events',
            detail: 'Consider snapshotting if this keeps growing fast.',
            severity: 'warning'
        },
        {
            title: 'Projection InvoiceReadModel is 45 min behind',
            detail: 'Consider replaying or sealing this projection.',
            severity: 'warning'
        },
        {
            title: '12 failed partitions need attention',
            detail: 'Check the single partitions tab for more details.',
            severity: 'success'
        }
    ];

    const getIcon = (severity: string) => {
        switch (severity) {
            case 'error':
                return <MdError className="text-red-400" size={20} />;
            case 'warning':
                return <MdWarning className="text-amber-400" size={20} />;
            default:
                return <MdCheckCircle className="text-green-400" size={20} />;
        }
    };

    const getBgColor = (severity: string) => {
        switch (severity) {
            case 'error':
                return 'bg-red-900/20 border-red-800/40';
            case 'warning':
                return 'bg-amber-900/20 border-amber-800/40';
            default:
                return 'bg-green-900/20 border-green-800/40';
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
                <h3 className="text-sm font-semibold text-white">Recommendations</h3>
                <Button icon={<MdChevronRight />} rounded text severity="secondary" size="small" />
            </div>
            <div className="flex flex-col gap-3">
                {items.map(item => (
                    <div
                        key={item.title}
                        className={`rounded-lg border p-3 ${getBgColor(item.severity)}`}>
                        <div className="flex items-start gap-3">
                            <div className="mt-0.5 shrink-0">{getIcon(item.severity)}</div>
                            <div className="flex flex-col gap-1">
                                <span className="text-sm font-medium text-white">{item.title}</span>
                                <span className="text-xs text-gray-400">{item.detail}</span>
                            </div>
                        </div>
                    </div>
                ))}
            </div>
        </Card>
    );
};
