// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DataTable, DataTableFilterMeta, DataTableSelectionSingleChangeEvent } from 'primereact/datatable';
import { Constructor } from '@cratis/fundamentals';
import { IObservableQueryFor, Paging } from '@cratis/applications/queries';
import { useObservableQueryWithPaging } from '@cratis/applications.react/queries';
import { ReactNode, useState } from 'react';

/* eslint-disable @typescript-eslint/no-explicit-any */

/**
 * Props for the DataTableForQuery component
 */
export interface DataTableForObservableQueryProps<TQuery extends IObservableQueryFor<TDataType>, TDataType, TArguments> {
    /**
     * Children to render
     */
    children?: ReactNode;

    /**
     * The type of query to use
     */
    query: Constructor<TQuery>;

    /**
     * Optional arguments to pass to the query
     */
    queryArguments?: TArguments;

    /**
     * The message to show when there is no data
     */
    emptyMessage: string;

    /**
     * The key to use for the data
     */
    dataKey?: string | undefined;

    /**
     * The current selection.
     */
    selection?: any[number] | undefined | null;

    /**
     * Callback for when the selection changes
     */
    onSelectionChange?(event: DataTableSelectionSingleChangeEvent<any>): void;

    /**
     * Fields to use for global filtering
     */
    globalFilterFields?: string[] | undefined;

    /**
     * Default filters to use
     */
    defaultFilters?: DataTableFilterMeta;
}

const paging = new Paging(0, 20);

/**
 * Represents a DataTable for a query.
 * @param props Props for the component
 * @returns Function to render the DataTableForQuery component
 */
export const DataTableForObservableQuery = <TQuery extends IObservableQueryFor<TDataType, TArguments>, TDataType, TArguments extends object>(props: DataTableForObservableQueryProps<TQuery, TDataType, TArguments>) => {
    const [filters, setFilters] = useState<DataTableFilterMeta>(props.defaultFilters ?? {});
    const [result, , setPage] = useObservableQueryWithPaging(props.query, paging, props.queryArguments);

    return (
        <DataTable
            value={result.data as any}
            lazy
            rows={paging.pageSize}
            totalRecords={result.paging.totalItems}
            paginator
            alwaysShowPaginator={false}
            first={result.paging.page * paging.pageSize}
            onPage={(e) => setPage(e.page ?? 0)}
            scrollable
            scrollHeight={'flex'}
            selectionMode='single'
            selection={props.selection}
            onSelectionChange={props.onSelectionChange}
            dataKey={props.dataKey}
            filters={filters}
            filterDisplay='menu'
            onFilter={(e) => setFilters(e.filters)}
            globalFilterFields={props.globalFilterFields}
            emptyMessage={props.emptyMessage}>
            {props.children}
        </DataTable>
    );
};
