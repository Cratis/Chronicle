// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React, { type CSSProperties, type ReactNode, useMemo, useState } from 'react';
import { DataTable as PrimeDataTable } from 'primereact/datatable';
import { InputText } from 'primereact/inputtext';
import type { SortOrder, UseDataTableFilterEvent, UseDataTableRowMouseEvent, UseDataTableSortEvent } from '@primereact/headless/datatable';
import { ColumnFilterMenu } from '@cratis/components/DataTables';
import type { ColumnProps, DataTableFilterConstraint, DataTableFilterMeta, DataTableSelectionChangeEvent } from '@cratis/components/DataTables';
import { resolveFieldData } from './resolveFieldData';

/**
 * Props for {@link DataTable}.
 *
 * @typeParam TData - The row type.
 */
export interface DataTableProps<TData extends object> {
    /** The rows to render. */
    value: readonly TData[];
    /** `<Column>` markers from `@cratis/components/DataTables` describing the columns. */
    children?: ReactNode;
    /** Property name uniquely identifying a row — required for selection. */
    dataKey?: string;
    /** Rendered in place of the body when there are no rows. */
    emptyMessage: ReactNode;
    /** Enables single-row selection. */
    selectionMode?: 'single';
    /** The currently selected row. */
    selection?: TData | null;
    /** Called when the selected row changes. */
    onSelectionChange?: (event: DataTableSelectionChangeEvent<TData>) => void;
    /** Called when a row is clicked, regardless of selection. */
    onRowClick?: (row: TData, index: number) => void;
    /** Constrains the body to its own scroll area rather than growing the page. */
    scrollable?: boolean;
    /** Height of the scroll area — `'flex'` fills the available space. */
    scrollHeight?: string;
    /** Applied to the table root. */
    className?: string;
    /** Applied to the table root. */
    style?: CSSProperties;
    /** Field to group consecutive rows by — renders a subheader row per group. */
    groupField?: string;
    /** Renders the subheader for a group, given the group's first row. */
    groupHeaderTemplate?: (rowData: TData) => ReactNode;
    /** Fields the global search box matches against. Omit it to hide the box. */
    globalFilterFields?: string[];
    /** Placeholder for the global search box. */
    globalSearchPlaceholder?: string;
    /** Column filters applied on first render. */
    defaultFilters?: DataTableFilterMeta;
    /** Called whenever the column filters change. */
    onFilter?: (filters: DataTableFilterMeta) => void;
    /** Rows are paged/sorted server-side — the table renders `value` as given. */
    lazy?: boolean;
    /** Total row count across all pages, for lazy mode. */
    totalRecords?: number;
    /** The field currently sorted on. */
    sortField?: string;
    /** The direction of the current sort. */
    sortOrder?: SortOrder;
    /** Called when the user sorts by a column header. */
    onSort?: (field: string, order: SortOrder) => void;
}

/** A parsed `<Column>` child. */
type ColumnElement = React.ReactElement<ColumnProps<object>>;

const useColumns = (children: ReactNode): ColumnElement[] =>
    useMemo(
        () => React.Children.toArray(children).filter(React.isValidElement) as ColumnElement[],
        [children]);

const renderCellContent = <TData extends object>(column: ColumnProps<TData>, row: TData): ReactNode => {
    if (column.body) return column.body(row);
    if (column.field) {
        const value = resolveFieldData(row, column.field);
        return value === null || value === undefined ? '' : String(value);
    }
    return null;
};

const keyOf = <TData extends object>(row: TData | null | undefined, dataKey: string | undefined) =>
    row && dataKey ? String(resolveFieldData(row, dataKey)) : undefined;

/**
 * A declarative table over a plain array, rebuilt on PrimeReact 11's compositional
 * `DataTable` primitives.
 *
 * PrimeReact 11 removed the monolithic `<DataTable value>` / `primereact/column`
 * pair, and the Cratis `DataTableForQuery` wrappers bind to an Arc query rather
 * than an array. The Workbench renders many tables over arrays it already holds,
 * so this fills that gap while keeping the same `<Column field header body sortable />`
 * authoring model — it consumes the very same `Column` marker as `DataPage.Columns`.
 *
 * @typeParam TData - The row type.
 */
export const DataTable = <TData extends object>({
    value,
    children,
    dataKey,
    emptyMessage,
    selectionMode,
    selection,
    onSelectionChange,
    onRowClick,
    scrollable,
    scrollHeight,
    className,
    style,
    groupField,
    groupHeaderTemplate,
    globalFilterFields,
    globalSearchPlaceholder = 'Search…',
    defaultFilters,
    onFilter,
    lazy,
    totalRecords,
    sortField,
    sortOrder,
    onSort
}: DataTableProps<TData>) => {
    const columns = useColumns(children);
    const data = useMemo(() => value as TData[], [value]);
    const [filters, setFilters] = useState<DataTableFilterMeta>(defaultFilters ?? {});
    const [globalFilter, setGlobalFilter] = useState('');
    const showGlobalSearch = !!globalFilterFields && globalFilterFields.length > 0;

    const handleFilter = (event: UseDataTableFilterEvent) => {
        // PrimeReact's headless filter meta is looser than the typed Cratis
        // constraint the public API exposes; narrow at this one boundary.
        const next = event.filters as DataTableFilterMeta;
        setFilters(next);
        onFilter?.(next);
    };

    // Components 4's `ColumnFilterMenu` is controlled — it renders the draft editor and
    // reports the result, leaving the applied state to the table that owns it.
    const applyFilters = (next: DataTableFilterMeta) => {
        setFilters(next);
        onFilter?.(next);
    };

    const constraintFor = (field: string) => {
        const entry = filters[field];
        return entry && 'constraints' in entry ? entry.constraints[0] : entry;
    };

    const applyConstraint = (field: string, constraint: DataTableFilterConstraint) =>
        applyFilters({ ...filters, [field]: constraint });

    const clearConstraint = (field: string) => {
        const next = { ...filters };
        delete next[field];
        applyFilters(next);
    };

    const selectionKeys = useMemo(() => {
        const key = keyOf(selection, dataKey);
        return key === undefined ? {} : { [key]: true };
    }, [selection, dataKey]);

    const handleSelectionChange = (event: { value: Record<string, boolean>; originalEvent?: React.SyntheticEvent }) => {
        if (!onSelectionChange) return;
        const selectedKey = Object.keys(event.value).find(key => event.value[key]);
        const row = selectedKey === undefined
            ? null
            : data.find(candidate => keyOf(candidate, dataKey) === selectedKey) ?? null;
        onSelectionChange({ value: row, originalEvent: event.originalEvent });
    };

    return (
        <PrimeDataTable.Root
            data={data}
            dataKey={dataKey}
            removableSort
            selectionMode={selectionMode ?? null}
            selectionKeys={selectionKeys}
            onSelectionChange={onSelectionChange ? handleSelectionChange : undefined}
            onRowClick={onRowClick
                ? (event: UseDataTableRowMouseEvent) => onRowClick(event.data as TData, event.index)
                : undefined}
            scrollable={scrollable}
            scrollHeight={scrollHeight}
            className={className}
            style={style}
            groupField={groupField}
            filters={filters}
            onFilter={handleFilter}
            globalFilter={globalFilter || null}
            globalFilterFields={globalFilterFields}
            lazy={lazy}
            totalRecords={totalRecords}
            sortField={sortField}
            sortOrder={sortOrder}
            onSortChange={onSort ? (event: UseDataTableSortEvent) => onSort(event.field, event.order) : undefined}>
            {showGlobalSearch && (
                <div className='cratis-datatable-search'>
                    <InputText
                        value={globalFilter}
                        placeholder={globalSearchPlaceholder}
                        className='w-full'
                        onChange={(event: React.ChangeEvent<HTMLInputElement>) => setGlobalFilter(event.target.value)} />
                </div>
            )}
            <PrimeDataTable.TableContainer>
                <PrimeDataTable.Table>
                    <PrimeDataTable.THead>
                        <PrimeDataTable.THeadRow>
                            {columns.map((column, index) => (
                                <PrimeDataTable.THeadCell
                                    key={index}
                                    style={column.props.headerStyle ?? column.props.style}
                                    className={column.props.headerClassName}>
                                    <div className='cratis-datatable-header-cell'>
                                        {column.props.sortable && column.props.field
                                            ? (
                                                <PrimeDataTable.Sort field={column.props.field}>
                                                    <PrimeDataTable.THeadTitle>{column.props.header}</PrimeDataTable.THeadTitle>
                                                    <PrimeDataTable.SortIndicator match='asc'> ▲</PrimeDataTable.SortIndicator>
                                                    <PrimeDataTable.SortIndicator match='desc'> ▼</PrimeDataTable.SortIndicator>
                                                </PrimeDataTable.Sort>
                                            )
                                            // The title part rather than a bare span, so the theme's column-title weight reaches it.
                                            : <PrimeDataTable.THeadTitle>{column.props.header}</PrimeDataTable.THeadTitle>}
                                        {column.props.filter && (column.props.filterField ?? column.props.field) && (
                                            <ColumnFilterMenu
                                                field={(column.props.filterField ?? column.props.field) as string}
                                                dataType={column.props.dataType}
                                                placeholder={column.props.filterPlaceholder}
                                                showMatchModes={column.props.showFilterMatchModes}
                                                constraint={constraintFor((column.props.filterField ?? column.props.field) as string)}
                                                onApply={constraint => applyConstraint((column.props.filterField ?? column.props.field) as string, constraint)}
                                                onClear={() => clearConstraint((column.props.filterField ?? column.props.field) as string)} />
                                        )}
                                    </div>
                                </PrimeDataTable.THeadCell>
                            ))}
                        </PrimeDataTable.THeadRow>
                    </PrimeDataTable.THead>
                    <PrimeDataTable.TBody>
                        {({ item, index, groupMeta }) => (
                            <>
                                {groupHeaderTemplate && groupMeta?.isGroupStart && (
                                    <PrimeDataTable.RowGroupHeader>
                                        <PrimeDataTable.Cell colSpan={Math.max(columns.length, 1)}>
                                            {groupHeaderTemplate(item as TData)}
                                        </PrimeDataTable.Cell>
                                    </PrimeDataTable.RowGroupHeader>
                                )}
                                <PrimeDataTable.Row index={index}>
                                    {columns.map((column, columnIndex) => (
                                        <PrimeDataTable.Cell
                                            key={columnIndex}
                                            style={{ ...column.props.style, ...column.props.bodyStyle }}
                                            className={column.props.bodyClassName ?? column.props.className}>
                                            {renderCellContent(column.props as ColumnProps<TData>, item as TData)}
                                        </PrimeDataTable.Cell>
                                    ))}
                                </PrimeDataTable.Row>
                            </>
                        )}
                    </PrimeDataTable.TBody>
                    <PrimeDataTable.EmptyTBody>
                        <PrimeDataTable.Row>
                            <PrimeDataTable.Cell colSpan={Math.max(columns.length, 1)}>
                                {emptyMessage}
                            </PrimeDataTable.Cell>
                        </PrimeDataTable.Row>
                    </PrimeDataTable.EmptyTBody>
                </PrimeDataTable.Table>
            </PrimeDataTable.TableContainer>
        </PrimeDataTable.Root>
    );
};
