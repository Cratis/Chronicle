// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DataTable, DataTableFilterMeta, DataTableSelectionSingleChangeEvent } from 'primereact/datatable';
import { Constructor } from '@cratis/fundamentals';
import { IObservableQueryFor } from '@cratis/applications/queries';
import { useObservableQuery } from '@cratis/applications.react/queries';
import { useState } from 'react';

/**
 * Props for the DataTableForQuery component
 */
export interface DataTableForObservableQueryProps<TQuery extends IObservableQueryFor<TDataType>, TDataType, TArguments> {
    /**
     * Children to render
     */
    children?: JSX.Element | JSX.Element[];

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
    dataKey: string;

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

/**
 * Represents a DataTable for a query.
 * @param props Props for the component
 * @returns Function to render the DataTableForQuery component
 */
export const DataTableForObservableQuery = <TQuery extends IObservableQueryFor<TDataType, TArguments>, TDataType, TArguments extends {}>(props: DataTableForObservableQueryProps<TQuery, TDataType, TArguments>) => {
    const [filters, setFilters] = useState<DataTableFilterMeta>(props.defaultFilters ?? {});
    const [result] = useObservableQuery<TDataType, TQuery>(props.query, props.queryArguments);

    return (
        <DataTable
            value={result.data as any}
            rows={100}
            paginator
            alwaysShowPaginator={false}
            scrollable
            scrollHeight={'flex'}
            selectionMode='single'
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
