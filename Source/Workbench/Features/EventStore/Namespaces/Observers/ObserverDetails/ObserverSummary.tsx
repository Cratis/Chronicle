// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Column } from 'primereact/column';
import { DataTable } from 'primereact/datatable';
import { ObserverInformation } from 'Api/Observation';
import { buildObserverSummaryRows } from './buildObserverSummaryRows';
import './ObserverSummary.css';

/**
 * Props for {@link ObserverSummary}.
 */
export interface ObserverSummaryProps {
    /**
     * The observer to summarize.
     */
    observer: ObserverInformation;
}

/**
 * Renders a flat key/value summary of the observer.
 *
 * @param props - The {@link ObserverSummaryProps}.
 */
export const ObserverSummary = ({ observer }: ObserverSummaryProps) => {
    const summaryRows = buildObserverSummaryRows(observer);

    return (
        <div className='observer-summary'>
            <DataTable
                value={summaryRows}
                dataKey='label'
                showHeaders={false}
                scrollable
                style={{ height: 'auto' }}>
                <Column field='label' style={{ width: '40%', color: 'var(--text-color-secondary)' }} />
                <Column field='value' />
            </DataTable>
        </div>
    );
};
