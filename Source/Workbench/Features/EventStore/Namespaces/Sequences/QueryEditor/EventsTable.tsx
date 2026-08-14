// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SortDirection, Sorting } from '@cratis/arc/queries';
import { Column } from 'primereact/column';
import { DataTable, DataTableSelectionSingleChangeEvent, DataTableStateEvent } from 'primereact/datatable';
import { Paginator } from 'primereact/paginator';
import strings from 'Strings';
import { AppendedEvent } from 'Api/Events';
import { QueryEvents, QueryEventsParameters } from 'Api/Events/QueryEvents';
import './EventsTable.css';

/** How many events one page of results holds. */
export const pageSize = 20;

/**
 * Props for {@link EventsTable}.
 */
export interface EventsTableProps {
    /** The arguments the events are read with. */
    queryArguments: QueryEventsParameters;
    /** The field the events are ordered by. */
    sortBy: string;
    /** Whether the order runs from the highest value down rather than from the lowest up. */
    descending: boolean;
    /** The event the user has selected, or null. */
    selection: AppendedEvent | null;
    /** Called when the selected event changes. */
    onSelectionChange: (event: AppendedEvent | null) => void;
    /** Called when the user sorts on a column. */
    onSort: (sortBy: string, descending: boolean) => void;
}

const occurred = (event: AppendedEvent) => event.context.occurred.toLocaleString();

/**
 * The events a query matches, a page at a time.
 *
 * Sorting is server-side and goes through Arc's own sorting - the query carries it on the query
 * context rather than as an argument of its own. This drives the table itself rather than going
 * through `DataTableForQuery` because that component does not pass `onSort` through, so a sortable
 * column there could only reorder the page already loaded rather than the whole matching set.
 * @param props The {@link EventsTableProps}.
 * @returns The rendered table.
 */
export const EventsTable = ({ queryArguments, sortBy, descending, selection, onSelectionChange, onSort }: EventsTableProps) => {
    const sequenceStrings = strings.eventStore.namespaces.sequences;
    const sorting = new Sorting(sortBy, descending ? SortDirection.descending : SortDirection.ascending);
    const [result, , , setPage] = QueryEvents.useWithPaging(pageSize, queryArguments, sorting);

    const sorted = (event: DataTableStateEvent) => onSort(event.sortField, event.sortOrder === -1);

    return (
        <div className='events-table'>
            <div className='events-table__rows'>
                <DataTable
                    value={result.data}
                    lazy
                    rows={pageSize}
                    totalRecords={result.paging.totalItems}
                    selectionMode='single'
                    selection={selection}
                    onSelectionChange={(event: DataTableSelectionSingleChangeEvent<AppendedEvent[]>) =>
                        onSelectionChange((event.value as AppendedEvent | null) ?? null)}
                    dataKey='context.sequenceNumber'
                    sortField={sortBy}
                    sortOrder={descending ? -1 : 1}
                    onSort={sorted}
                    emptyMessage={sequenceStrings.empty}
                    scrollable
                    scrollHeight='flex'
                    style={{ height: '100%' }}>
                    <Column field='sequenceNumber' sortable header={sequenceStrings.columns.sequenceNumber} body={_ => _.context.sequenceNumber.toString()} />
                    <Column field='eventType' sortable header={sequenceStrings.columns.eventType} body={_ => _.context.eventType.id} />
                    <Column field='eventSourceId' sortable header={sequenceStrings.columns.eventSourceId} body={_ => _.context.eventSourceId} />
                    <Column field='occurred' sortable header={sequenceStrings.columns.occurred} body={occurred} />
                </DataTable>
            </div>

            {result.paging.totalPages > 1 && (
                <Paginator
                    first={result.paging.page * pageSize}
                    rows={pageSize}
                    totalRecords={result.paging.totalItems}
                    onPageChange={event => setPage(event.page)} />
            )}
        </div>
    );
};
