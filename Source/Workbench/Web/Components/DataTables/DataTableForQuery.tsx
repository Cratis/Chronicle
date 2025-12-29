// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DataTable, DataTableFilterMeta, DataTableSelectionSingleChangeEvent } from 'primereact/datatable';
import { Paginator } from 'primereact/paginator';
import { Constructor } from '@cratis/fundamentals';
import { IQueryFor, Paging } from '@cratis/arc/queries';
import { useQueryWithPaging } from '@cratis/arc.react/queries';
import { ReactNode, useState, useRef, useEffect } from 'react';

/* eslint-disable @typescript-eslint/no-explicit-any */

/**
 * Props for the DataTableForQuery component
 */
export interface DataTableForQueryProps<TQuery extends IQueryFor<TDataType>, TDataType, TArguments> {
    /**
     * Children to render
     */
    children?: ReactNode;

    /**
     * The type of query to use
     */
    query: Constructor<TQuery>;

    /**
     * Optional Arguments to pass to the query
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
export const DataTableForQuery = <TQuery extends IQueryFor<TDataType, TArguments>, TDataType, TArguments extends object>(props: DataTableForQueryProps<TQuery, TDataType, TArguments>) => {
    const [filters, setFilters] = useState<DataTableFilterMeta>(props.defaultFilters ?? {});
    const [result, , , setPage] = useQueryWithPaging(props.query, paging, props.queryArguments);
    const containerRef = useRef<HTMLDivElement>(null);
    const [tableHeight, setTableHeight] = useState<number>(600);
    const timeoutRef = useRef<NodeJS.Timeout>();

    useEffect(() => {
        if (!containerRef.current) return;

        const resizeObserver = new ResizeObserver((entries) => {
            // Debug: log ResizeObserver entries for diagnosing sizing
            try {
                for (const entry of entries) {
                    const cr = entry.contentRect;
                    console.log('DataTableForQuery ResizeObserver entry', { width: cr.width, height: cr.height });
                }
            } catch (err) {
                console.error('DataTableForQuery ResizeObserver debug failed', err);
            }

            if (timeoutRef.current) {
                clearTimeout(timeoutRef.current);
            }

            timeoutRef.current = setTimeout(() => {
                for (const entry of entries) {
                    const containerHeight = entry.contentRect.height;
                    try {
                        console.log('DataTableForQuery calculating heights', { containerHeight, paginatorTotal: result.paging.totalItems });
                    } catch (err) {
                        console.error('DataTableForQuery calculate debug failed', err);
                    }
                    if (containerHeight > 0) {
                        const paginatorHeight = result.paging.totalItems > 0 ? 56 : 0;
                        const calculatedHeight = containerHeight - paginatorHeight - 2;
                        const newHeight = Math.max(calculatedHeight, 200);
                        
                        setTableHeight(prevHeight => {
                            if (Math.abs(newHeight - prevHeight) > 5) {
                                return newHeight;
                            }
                            return prevHeight;
                        });
                    }
                }
            }, 10);
        });

        resizeObserver.observe(containerRef.current);

        // Debug: initial container measurements
        try {
            const el = containerRef.current;
            if (el) {
                const rect = el.getBoundingClientRect();
                const cs = window.getComputedStyle(el);
                console.log('DataTableForQuery initial container', { rect: { width: rect.width, height: rect.height }, display: cs.display, height: cs.height, minHeight: cs.minHeight, overflow: cs.overflow });
                console.log('DataTableForQuery initial tableHeight state', tableHeight);
            }
        } catch (err) {
            console.error('DataTableForQuery initial debug failed', err);
        }

        return () => {
            if (timeoutRef.current) {
                clearTimeout(timeoutRef.current);
            }
            resizeObserver.disconnect();
        };
    }, [result.paging.totalItems]);

    return (
        <div 
            ref={containerRef}
            style={{ 
                display: 'flex',
                flexDirection: 'column',
                height: '100%',
                border: '1px solid var(--surface-border)', 
                borderRadius: 'var(--border-radius)',
                overflow: 'hidden'
            }}>
            <div style={{ flex: 1, minHeight: 0, overflow: 'auto' }}>
                <DataTable
                    value={result.data as any}
                    lazy
                    rows={paging.pageSize}
                    totalRecords={result.paging.totalItems}
                    selectionMode='single'
                    selection={props.selection}
                    dataKey={props.dataKey}
                    filters={filters}
                    filterDisplay='menu'
                    onFilter={(e) => setFilters(e.filters)}
                    globalFilterFields={props.globalFilterFields}
                    emptyMessage={props.emptyMessage}
                    style={{ minWidth: '100%' }}
                >
                    {props.children}
                </DataTable>
            </div>

            {result.paging.totalItems > 0 && (
                <div style={{ borderTop: '1px solid var(--surface-border)', flexShrink: 0 }}>
                    <Paginator
                        first={result.paging.page * paging.pageSize}
                        rows={paging.pageSize}
                        totalRecords={result.paging.totalItems}
                        onPageChange={(e) => setPage(e.page)}
                    />
                </div>
            )}
        </div>
    );
};
