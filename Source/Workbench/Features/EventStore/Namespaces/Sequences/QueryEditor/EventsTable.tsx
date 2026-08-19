// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SortDirection, Sorting } from '@cratis/arc/queries';
import { Column, type DataTableSelectionChangeEvent } from '@cratis/components/DataTables';
import { DataTable } from 'Components/DataTable';
import { Paginator } from 'Components/Paginator';
import strings from 'Strings';
import { AppendedEvent } from 'Features/Sequences';
import { QueryEvents, QueryEventsParameters } from 'Features/Sequences';
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
    /** Called when the user sorts by a column header. */
    onSort: (sortBy: string, descending: boolean) => void;
}

const occurred = (event: AppendedEvent) => event.context.occurred.toLocaleString();

/**
 * The events a query matches, a page at a time.
 *
 * Sorting is server-side and goes through Arc's own sorting — the query carries it on the query
 * context rather than as an argument of its own, so paging through a sorted query walks the whole
 * matching set. The table therefore runs `lazy`: it renders the page exactly as the server
 * returned it and reports header clicks back through `onSort` instead of reordering locally.
 * @param props The {@link EventsTableProps}.
 * @returns The rendered table.
 */
export const EventsTable = ({ queryArguments, sortBy, descending, selection, onSelectionChange, onSort }: EventsTableProps) => {
    const sequenceStrings = strings.eventStore.namespaces.sequences;
    const sorting = new Sorting(sortBy, descending ? SortDirection.descending : SortDirection.ascending);
    const [result, , , setPage] = QueryEvents.useWithPaging(pageSize, queryArguments, sorting);

    return (
        <div className='events-table'>
            <div className='events-table__rows'>
                <DataTable<AppendedEvent>
                    value={result.data}
                    selectionMode='single'
                    selection={selection}
                    onSelectionChange={(event: DataTableSelectionChangeEvent<AppendedEvent>) =>
                        onSelectionChange(event.value ?? null)}
                    dataKey='context.sequenceNumber'
                    emptyMessage={sequenceStrings.empty}
                    scrollable
                    scrollHeight='flex'
                    style={{ height: '100%' }}
                    lazy
                    totalRecords={result.paging.totalItems}
                    sortField={sortBy}
                    sortOrder={descending ? -1 : 1}
                    onSort={(field, order) => onSort(field, order === -1)}>
                    <Column<AppendedEvent> sortable field='sequenceNumber' header={sequenceStrings.columns.sequenceNumber} body={_ => _.context.sequenceNumber.toString()} />
                    <Column<AppendedEvent> sortable field='eventType' header={sequenceStrings.columns.eventType} body={_ => _.context.eventType.id} />
                    <Column<AppendedEvent> sortable field='eventSourceId' header={sequenceStrings.columns.eventSourceId} body={_ => _.context.eventSourceId} />
                    <Column<AppendedEvent> sortable field='occurred' header={sequenceStrings.columns.occurred} body={occurred} />
                </DataTable>
            </div>

            {result.paging.totalPages > 1 && (
                <Paginator
                    page={result.paging.page}
                    pageSize={pageSize}
                    totalRecords={result.paging.totalItems}
                    onPageChange={setPage} />
            )}
        </div>
    );
};
