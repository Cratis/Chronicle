// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CapturedEvent, CapturedEvents } from 'Features/Captures';
import { DataTable } from 'Components/DataTable';
import { Column } from '@cratis/components/DataTables';
import { useEffect } from 'react';
import strings from 'Strings';

export interface CapturedEventsViewProps {
    eventStore: string;
    captureName: string;

    /** Incremented by the owner to trigger a refresh of the captured events. */
    refreshTrigger: number;
}

/**
 * Shows the events a capture has ingested - the events tagged with the capture's name, most recent first.
 */
export const CapturedEventsView = ({ eventStore, captureName, refreshTrigger }: CapturedEventsViewProps) => {
    const [result, perform] = CapturedEvents.use({ eventStore, captureName, namespace: 'Default', maxEvents: 200 });

    useEffect(() => {
        if (refreshTrigger > 0) {
            perform({ eventStore, captureName, namespace: 'Default', maxEvents: 200 });
        }
    }, [refreshTrigger]);

    return (
        <div className="h-full" style={{ overflow: 'auto' }}>
            <DataTable<CapturedEvent>
                value={result.data}
                emptyMessage={strings.eventStore.general.captures.dataView.empty}
                dataKey="id"
                className="rounded-lg overflow-hidden"
            >
                <Column field="context.sequenceNumber" header={strings.eventStore.general.captures.dataView.columns.sequenceNumber} />
                <Column field="context.eventType.id" header={strings.eventStore.general.captures.dataView.columns.eventType} />
                <Column field="context.eventSourceId" header={strings.eventStore.general.captures.dataView.columns.eventSourceId} />
                <Column<CapturedEvent>
                    field="context.occurred"
                    header={strings.eventStore.general.captures.dataView.columns.occurred}
                    body={(event) => new Date(event.context.occurred.value).toLocaleString()}
                />
                <Column<CapturedEvent>
                    field="content"
                    header={strings.eventStore.general.captures.dataView.columns.content}
                    body={(event) => <code style={{ fontSize: '0.85rem' }}>{event.content}</code>}
                />
            </DataTable>
        </div>
    );
};
