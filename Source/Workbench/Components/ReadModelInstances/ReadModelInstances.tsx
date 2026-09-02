// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState, useMemo, useCallback } from 'react';
import { Column } from '@cratis/components/DataTables';
import { Dropdown } from '@cratis/components/Dropdown';
import { DataTableCore } from '@cratis/components/DataTables';
import { TablePaginator } from '@cratis/components/DataTables';
import strings from 'Strings';
import { Json } from 'Features';
import * as faIcons from 'react-icons/fa6';
import { ReadModelInstance } from 'Features/ReadModelExplorer';
import { ObjectNavigationalBar } from '@cratis/components/ObjectNavigationalBar';
import { ProgressSpinner } from '@cratis/components/Display';
import './ReadModelInstances.css';

interface Props {
    instances: ReadModelInstance[];
    page: number;
    pageSize: number;
    totalItems: number;
    isPerforming: boolean;
    setPage: (p: number) => void;
    setPageSize: (s: number) => void;
    selectedInstance: Json | null;
    setSelectedInstance: (instance: Json | null) => void;
}

export function ReadModelInstances({ instances, page, pageSize, totalItems, isPerforming, setPage, setPageSize, selectedInstance, setSelectedInstance }: Props) {
    const [navigationPath, setNavigationPath] = useState<string[]>([]);

    const getValueAtPath = useCallback((data: Json, path: string[]): Json | null => {
        let current: Json = data;
        for (const segment of path) {
            if (current === null || current === undefined) return null;
            if (Array.isArray(current)) {
                // When traversing through an intermediate array, collect the property from
                // every element and flatten one level so deeper paths keep working.
                const collected = (current as Json[])
                    .filter(el => el !== null && typeof el === 'object' && !Array.isArray(el))
                    .flatMap(el => {
                        const val = (el as { [k: string]: Json })[segment];
                        return val !== undefined ? [val] : [];
                    });
                current = collected as unknown as Json;
            } else if (typeof current === 'object' && current !== null) {
                current = (current as { [key: string]: Json })[segment];
            } else {
                return null;
            }
        }
        return current;
    }, []);

    const deepEqual = useCallback((a: Json, b: Json) => {
        try {
            return JSON.stringify(a) === JSON.stringify(b);
        } catch {
            return false;
        }
    }, []);

    const currentData = useMemo<Json[]>(() => {
        if (!instances || instances.length === 0) return [];

        if (navigationPath.length === 0) {
            return instances.map((item: ReadModelInstance) => item.instance as Json);
        }

        const lastKey = navigationPath[navigationPath.length - 1];
        const pathToParent = navigationPath.slice(0, -1);

        const result: Json[] = [];
        instances.forEach((item: ReadModelInstance) => {
            const parentValue = pathToParent.length > 0
                ? getValueAtPath(item.instance as Json, pathToParent)
                : item.instance as Json;

            if (parentValue && typeof parentValue === 'object' && !Array.isArray(parentValue)) {
                const value = (parentValue as { [k: string]: Json })[lastKey];

                if (Array.isArray(value)) {
                    result.push(...value.map((val: Json, idx: number) => ({
                        __arrayIndex: idx,
                        __sourceInstance: item.instance as Json,
                        ...(typeof val === 'object' && val !== null ? (val as { [k: string]: Json }) : {})
                    }) as Json));
                } else if (value && typeof value === 'object') {
                    result.push({
                        __sourceInstance: item.instance as Json,
                        ...(value as { [k: string]: Json })
                    } as Json);
                }
            } else if (Array.isArray(parentValue)) {
                // parentValue is a flattened array produced by traversing through one or more
                // intermediate arrays (e.g. Root → configurations[] → hubs[]).
                // Collect `lastKey` from every element inside it.
                (parentValue as Json[]).forEach((element: Json) => {
                    if (element && typeof element === 'object' && !Array.isArray(element)) {
                        const value = (element as { [k: string]: Json })[lastKey];
                        if (Array.isArray(value)) {
                            result.push(...value.map((val: Json, idx: number) => ({
                                __arrayIndex: idx,
                                __sourceInstance: item.instance as Json,
                                ...(typeof val === 'object' && val !== null ? (val as { [k: string]: Json }) : {})
                            }) as Json));
                        } else if (value && typeof value === 'object') {
                            result.push({
                                __sourceInstance: item.instance as Json,
                                ...(value as { [k: string]: Json })
                            } as Json);
                        }
                    }
                });
            }
        });

        return result;
    }, [instances, navigationPath, getValueAtPath]);

    const objectArray = useMemo(() => {
        return currentData.filter((i): i is { [k: string]: Json } => i !== null && typeof i === 'object' && !Array.isArray(i));
    }, [currentData]);

    const navigateToArray = useCallback((key: string) => {
        setNavigationPath([...navigationPath, key]);
        setPage(0);
    }, [navigationPath, setPage]);

    const navigateToObject = useCallback((key: string) => {
        setNavigationPath([...navigationPath, key]);
        setPage(0);
    }, [navigationPath, setPage]);

    const navigateToBreadcrumb = useCallback((index: number) => {
        if (index === 0) {
            setNavigationPath([]);
        } else {
            setNavigationPath(navigationPath.slice(0, index));
        }
        setPage(0);
    }, [navigationPath, setPage]);

    const columns = useMemo(() => {
        if (currentData.length === 0) return [];

        const firstItem = currentData[0];
        if (!firstItem || typeof firstItem !== 'object' || Array.isArray(firstItem)) return [];
        const keys = Object.keys(firstItem as { [k: string]: Json }).filter(k => !k.startsWith('__'));

        return keys.map(key => (
            <Column
                key={key}
                field={key}
                header={key}
                sortable
                body={(rowData: Record<string, unknown>) => {
                    const value = rowData[key] as Json;
                    if (value === null || value === undefined) return '';

                    if (Array.isArray(value)) {
                        return (
                            <div
                                className="flex align-items-center gap-2 cursor-pointer"
                                onClick={(e) => { e.stopPropagation(); navigateToArray(key); }}
                                style={{ color: 'var(--primary-color)', display: 'flex', alignItems: 'center' }}
                            >
                                <span>{strings.eventStore.namespaces.readModels.labels.array}[{value.length}]</span>
                                <faIcons.FaArrowRight style={{ fontSize: '0.875rem', display: 'inline-flex' }} />
                            </div>
                        );
                    }

                    if (typeof value === 'object') {
                        return (
                            <div
                                className="flex align-items-center gap-2 cursor-pointer"
                                onClick={(e) => { e.stopPropagation(); navigateToObject(key); }}
                                style={{ color: 'var(--primary-color)', display: 'flex', alignItems: 'center' }}
                            >
                                <span>{strings.eventStore.namespaces.readModels.labels.object}</span>
                                <faIcons.FaArrowRight style={{ fontSize: '0.875rem', display: 'inline-flex' }} />
                            </div>
                        );
                    }

                    return String(value);
                }}
            />
        ));
    }, [currentData, navigateToArray, navigateToObject]);

    const pageSizeOptions = useMemo(() => [10, 25, 50, 100].map(size => ({ label: String(size), value: size })), []);

    return (
        <>
            <div className="p-4" style={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
                <ObjectNavigationalBar
                    navigationPath={navigationPath}
                    onNavigate={navigateToBreadcrumb}
                />

                <div
                    className="card"
                    style={{
                        display: 'flex',
                        flexDirection: 'column',
                        height: '100%',
                        border: '1px solid var(--surface-border)',
                        borderRadius: 'var(--border-radius)',
                        overflow: 'hidden'
                    }}>

                    <div style={{ flex: 1, minHeight: 0, overflowY: 'auto', overflowX: 'auto', position: 'relative' }}>
                        {isPerforming && (
                            <div className='read-model-instances__busy' data-testid='read-model-instances-busy'>
                                <ProgressSpinner aria-label={strings.general.loading} />
                            </div>
                        )}
                        <DataTableCore<{ [k: string]: Json }>
                            data={objectArray}
                            dataKey='_id'
                            emptyMessage={isPerforming ? strings.general.loading : strings.eventStore.namespaces.readModels.empty}
                            selectionMode="single"
                            selection={(selectedInstance && typeof selectedInstance === 'object' && !Array.isArray(selectedInstance)) ? selectedInstance as { [k: string]: Json } : null}
                            onSelectionChange={(event) => {
                                // The table reports the row that was activated; clicking the row that is
                                // already selected clears the selection. Store the row itself rather than a
                                // copy, so the table can match it back to the row it came from.
                                const clicked = event.value;
                                const isAlreadySelected = !!clicked && !!selectedInstance && deepEqual(selectedInstance, clicked);
                                setSelectedInstance(isAlreadySelected ? null : clicked);
                                setNavigationPath([]);
                            }}
                            style={selectedInstance ? { minWidth: '100%', width: 'max-content' } : { minWidth: '100%' }}
                        >
                            {columns}
                        </DataTableCore>
                    </div>

                    {totalItems > 0 && navigationPath.length === 0 && (
                        <div
                            style={{
                                borderTop: '1px solid var(--surface-border)',
                                flexShrink: 0,
                                display: 'flex',
                                alignItems: 'center',
                                justifyContent: 'space-between',
                                gap: '0.5rem'
                            }}>
                            <TablePaginator
                                page={page}
                                pageCount={Math.ceil(totalItems / pageSize)}
                                pageSize={pageSize}
                                totalItems={totalItems}
                                onPageChange={setPage}
                            />
                            <Dropdown<number>
                                value={pageSize}
                                options={pageSizeOptions}
                                optionLabel='label'
                                optionValue='value'
                                aria-label={strings.eventStore.namespaces.readModels.labels.rowsPerPage}
                                onChange={value => { setPageSize(value); setPage(0); }}
                            />
                        </div>
                    )}
                </div>
            </div>

        </>
    );
}
