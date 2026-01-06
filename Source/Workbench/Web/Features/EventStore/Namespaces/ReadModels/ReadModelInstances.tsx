// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState, useMemo, useCallback } from 'react';
import { DataTable } from 'primereact/datatable';
import { Column } from 'primereact/column';
import { Paginator, PaginatorPageChangeEvent } from 'primereact/paginator';
import { Button } from 'primereact/button';
import strings from 'Strings';
import { Json } from 'Features';
import * as faIcons from 'react-icons/fa6';
import { ReadModelInstance } from 'Api/ReadModels';

interface Props {
    instances: ReadModelInstance[];
    page: number;
    pageSize: number;
    totalItems: number;
    isPerforming: boolean;
    setPage: (p: number) => void;
    setPageSize: (s: number) => void;
}

export default function ReadModelInstances({ instances, page, pageSize, totalItems, isPerforming, setPage, setPageSize }: Props) {
    const [navigationPath, setNavigationPath] = useState<string[]>([]);
    const [selectedObject, setSelectedObject] = useState<Json | null>(null);

    const getValueAtPath = useCallback((data: Json, path: string[]): Json | null => {
        let current: Json = data;
        for (const segment of path) {
            if (current === null || current === undefined) return null;
            if (typeof current === 'object' && !Array.isArray(current) && current !== null) {
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

        const pathToArray = navigationPath.slice(0, -1);
        const arrayKey = navigationPath[navigationPath.length - 1];

        const arrayData: Json[] = [];
        instances.forEach((item: ReadModelInstance) => {
            const parentValue = getValueAtPath(item.instance as Json, pathToArray);
            if (parentValue) {
                const maybeArray = (parentValue as { [k: string]: Json })[arrayKey];
                if (Array.isArray(maybeArray)) {
                    arrayData.push(...maybeArray.map((val: Json, idx: number) => ({
                        __arrayIndex: idx,
                        __sourceInstance: item.instance as Json,
                        ...(typeof val === 'object' && val !== null ? (val as { [k: string]: Json }) : {})
                    }) as Json));
                }
            }
        });

        return arrayData;
    }, [instances, navigationPath, getValueAtPath]);

    const objectArray = useMemo(() => {
        return currentData.filter((i): i is { [k: string]: Json } => i !== null && typeof i === 'object' && !Array.isArray(i));
    }, [currentData]);

    const navigateToArray = useCallback((key: string) => {
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

    const handleObjectClick = useCallback((value: Json) => {
        setSelectedObject(value);
        setObjectNavigationPath([]);
    }, []);

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
                                style={{ color: 'var(--primary-color)' }}
                            >
                                <span>{strings.eventStore.namespaces.readModels.labels.array}[{value.length}]</span>
                                <faIcons.FaArrowRight style={{ fontSize: '0.875rem' }} />
                            </div>
                        );
                    }

                    if (typeof value === 'object') {
                        return (
                            <div
                                className="flex align-items-center gap-2 cursor-pointer"
                                onClick={() => handleObjectClick(value)}
                                style={{ color: 'var(--primary-color)' }}
                            >
                                <span>{strings.eventStore.namespaces.readModels.labels.object}</span>
                                <faIcons.FaArrowRight style={{ fontSize: '0.875rem' }} />
                            </div>
                        );
                    }

                    return String(value);
                }}
            />
        ));
    }, [currentData, navigateToArray, handleObjectClick]);

    const breadcrumbItems = useMemo(() => {
        const items: { name: string; path: string[] }[] = [{ name: strings.eventStore.namespaces.readModels.labels.root, path: [] }];
        for (let i = 0; i < navigationPath.length; i++) {
            items.push({
                name: navigationPath[i],
                path: navigationPath.slice(0, i + 1)
            });
        }
        return items;
    }, [navigationPath]);

    const onPageChange = (event: PaginatorPageChangeEvent) => {
        setPage(event.page);
        setPageSize(event.rows);
    };

    return (
        <>
            <div className="p-4" style={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
                {navigationPath.length > 0 && (
                    <div className="px-4 py-2 mb-2 border-bottom-1 surface-border">
                        <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                            <Button
                                icon={<faIcons.FaArrowLeft />}
                                className="p-button-text p-button-sm"
                                onClick={() => navigateToBreadcrumb(navigationPath.length - 1)}
                                tooltip={strings.eventStore.namespaces.readModels.actions.navigateBack}
                                tooltipOptions={{ position: 'top' }}
                            />
                            <div style={{ fontSize: '0.9rem', color: 'var(--text-color-secondary)' }}>
                                {breadcrumbItems.map((item, index) => (
                                    <span key={index}>
                                        {index > 0 && <span className="mx-2">&gt;</span>}
                                        <span
                                            onClick={() => navigateToBreadcrumb(index)}
                                            style={{
                                                cursor: 'pointer',
                                                textDecoration: index < breadcrumbItems.length - 1 ? 'underline' : 'none'
                                            }}
                                        >
                                            {item.name}
                                        </span>
                                    </span>
                                ))}
                            </div>
                        </div>
                    </div>
                )}

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

                    <div style={{ flex: 1, minHeight: 0, overflowY: 'auto', overflowX: 'auto' }}>
                        <DataTable
                            value={objectArray}
                            loading={isPerforming}
                            emptyMessage={strings.eventStore.namespaces.readModels.empty}
                            className="p-datatable-sm"
                            selectionMode="single"
                            onRowClick={(e) => {
                                const rowData = { ...e.data };
                                // Remove internal properties before showing
                                delete rowData.__arrayIndex;
                                delete rowData.__sourceInstance;

                                if (selectedObject && deepEqual(selectedObject, rowData)) {
                                    setSelectedObject(null);
                                    setNavigationPath([]);
                                } else {
                                    setSelectedObject(rowData);
                                    setNavigationPath([]);
                                }
                            }}
                            style={selectedObject ? { minWidth: '100%', width: 'max-content' } : { minWidth: '100%' }}
                        >
                            {...columns}
                        </DataTable>
                    </div>

                    {totalItems > 0 && navigationPath.length === 0 && (
                        <div style={{ borderTop: '1px solid var(--surface-border)', flexShrink: 0 }}>
                            <Paginator
                                first={page * pageSize}
                                rows={pageSize}
                                totalRecords={totalItems}
                                rowsPerPageOptions={[10, 25, 50, 100]}
                                onPageChange={onPageChange}
                            />
                        </div>
                    )}
                </div>
            </div>

        </>
    );
}
