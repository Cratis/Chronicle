// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Badge } from 'primereact/badge';
import { Card } from 'primereact/card';
import { Chip } from 'primereact/chip';

export const NewEventTypesWidget = ({ className }: { className?: string }) => {
    const recent = [
        'ProductReviewed',
        'UserRetrieved',
        'nwhate roiently',
        'Dectilepic',
        'UserDeactivated',
        'CartAbandoned'
    ];

    return (
        <Card
            className={`shadow-lg h-full ${className ?? ''}`}
            pt={{
                root: { className: 'border border-gray-700/60' },
                body: { className: 'p-4' },
                content: { className: 'p-0' }
            }}>
            <div className="flex items-center gap-2 mb-4">
                <h3 className="text-sm font-semibold text-white">New Event Types</h3>
                <Badge value="3 last 24h" severity="info" className="text-xs" />
            </div>
            <div className="flex flex-wrap gap-2">
                {recent.map(name => (
                    <Chip
                        key={name}
                        label={name}
                        className="bg-slate-800/80 text-gray-200 border border-gray-700/50"
                    />
                ))}
            </div>
        </Card>
    );
};
