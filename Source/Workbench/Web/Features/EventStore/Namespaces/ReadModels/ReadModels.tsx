// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState, useMemo, useEffect, useCallback } from 'react';
import { useParams } from 'react-router-dom';
import { Dropdown } from 'primereact/dropdown';
import { Button } from 'primereact/button';
import { DataTable } from 'primereact/datatable';
import { Column } from 'primereact/column';
import { Paginator, PaginatorPageChangeEvent } from 'primereact/paginator';
import { Sidebar } from 'primereact/sidebar';
import { Allotment } from 'allotment';
import { Page } from 'Components';
import { AllReadModelDefinitions, ReadModelDefinition } from 'Api/ReadModelTypes';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { ReadModelOccurrence, ReadModelOccurrences, ReadModelInstances } from 'Api/ReadModels';
import * as faIcons from 'react-icons/fa6';

interface NavigationItem {
    name: string;
    path: string[];
}

export const ReadModels = () => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const [allReadModels] = AllReadModelDefinitions.use({ eventStore: params.eventStore! });

    const [selectedReadModel, setSelectedReadModel] = useState<ReadModelDefinition | null>(null);
    const [selectedOccurrence, setSelectedOccurrence] = useState<ReadModelOccurrence | null>(null);
    const [page, setPage] = useState(0);
    const [pageSize, setPageSize] = useState(50);
    const [navigationPath, setNavigationPath] = useState<string[]>([]);
    const [selectedObject, setSelectedObject] = useState<any>(null);
    const [showObjectDetails, setShowObjectDetails] = useState(false);

    const [occurrences] = ReadModelOccurrences.use({
        eventStore: params.eventStore!,
        namespace: params.namespace!,
        readModel: selectedReadModel ? selectedReadModel.identifier : undefined!
    });

    const instancesArgs = useMemo(() => ({
        eventStore: params.eventStore!,
        namespace: params.namespace!,
        readModel: selectedReadModel ? selectedReadModel.identifier : undefined!,
        occurrence: selectedOccurrence && selectedOccurrence.revertModel !== 'Default' ? selectedOccurrence.revertModel : undefined
    }), [params.eventStore, params.namespace, selectedReadModel, selectedOccurrence]);

    const [instances, performInstancesQuery] = ReadModelInstances.useWithPaging(pageSize, instancesArgs);

    const executeQuery = useCallback(() => {
        if (selectedReadModel && selectedOccurrence) {
            setPage(0);
            setNavigationPath([]);
            setSelectedObject(null);
            setShowObjectDetails(false);
            performInstancesQuery(instancesArgs);
        }
    }, [selectedReadModel, selectedOccurrence, instancesArgs, performInstancesQuery]);

    useEffect(() => {
        if (occurrences.data.length > 0 && !selectedOccurrence) {
            const defaultOccurrence = occurrences.data.find(occ => occ.revertModel === 'Default');
            if (defaultOccurrence) {
                setSelectedOccurrence(defaultOccurrence);
            }
        }
    }, [occurrences.data, selectedOccurrence]);

    useEffect(() => {
        executeQuery();
    }, [selectedOccurrence]);

    const onPageChange = (event: PaginatorPageChangeEvent) => {
        setPage(event.page);
        setPageSize(event.rows);
    };

    const occurrenceOptions = useMemo(() => {
        return occurrences.data.map(occ => ({
            label: occ.revertModel === 'Default'
                ? `Default (Generation ${occ.generation})`
                : `${new Date(occ.occurred).toLocaleString()} (Generation ${occ.generation})`,
            value: occ
        }));
    }, [occurrences]);

    const getValueAtPath = useCallback((data: any, path: string[]): any => {
        let current = data;
        for (const segment of path) {
            if (current === null || current === undefined) return null;
            current = current[segment];
        }
        return current;
    }, []);

    const currentData = useMemo(() => {
        if (!instances.data || instances.data.length === 0) return [];

        if (navigationPath.length === 0) {
            return instances.data.map(item => item.instance);
        }

        const pathToArray = navigationPath.slice(0, -1);
        const arrayKey = navigationPath[navigationPath.length - 1];

        const arrayData: any[] = [];
        instances.data.forEach(item => {
            const parentValue = getValueAtPath(item.instance, pathToArray);
            if (parentValue && Array.isArray(parentValue[arrayKey])) {
                arrayData.push(...parentValue[arrayKey].map((val: any, idx: number) => ({
                    __arrayIndex: idx,
                    __sourceInstance: item.instance,
                    ...val
                })));
            }
        });

        return arrayData;
    }, [instances.data, navigationPath, getValueAtPath]);

    const navigateToArray = useCallback((key: string) => {
        setNavigationPath([...navigationPath, key]);
        setPage(0);
    }, [navigationPath]);

    const navigateToBreadcrumb = useCallback((index: number) => {
        if (index === 0) {
            setNavigationPath([]);
        } else {
            setNavigationPath(navigationPath.slice(0, index));
        }
        setPage(0);
    }, [navigationPath]);

    const handleObjectClick = useCallback((value: any) => {
        setSelectedObject(value);
        setShowObjectDetails(true);
    }, []);

    const renderObjectProperty = useCallback((obj: any, key: string, depth: number = 0): JSX.Element => {
        const value = obj[key];
        const indent = depth * 1.5;

        if (value === null || value === undefined) {
            return (
                <div key={key} style={{ marginLeft: `${indent}rem`, marginBottom: '0.5rem' }}>
                    <span style={{ fontWeight: 500, color: 'var(--text-color)' }}>{key}:</span>
                    <span style={{ marginLeft: '0.5rem', color: 'var(--text-color-secondary)', fontStyle: 'italic' }}>null</span>
                </div>
            );
        }

        if (Array.isArray(value)) {
            return (
                <div key={key} style={{ marginLeft: `${indent}rem`, marginBottom: '0.5rem' }}>
                    <span style={{ fontWeight: 500, color: 'var(--text-color)' }}>{key}:</span>
                    <span style={{ marginLeft: '0.5rem', color: 'var(--text-color-secondary)' }}>Array[{value.length}]</span>
                    {value.map((item, idx) => (
                        <div key={idx} style={{ marginLeft: '1.5rem', marginTop: '0.25rem' }}>
                            {typeof item === 'object' ? (
                                <div>
                                    <span style={{ color: 'var(--text-color-secondary)' }}>[{idx}]</span>
                                    {Object.keys(item).map(k => renderObjectProperty(item, k, depth + 2))}
                                </div>
                            ) : (
                                <span><span style={{ color: 'var(--text-color-secondary)' }}>[{idx}]:</span> {String(item)}</span>
                            )}
                        </div>
                    ))}
                </div>
            );
        }

        if (typeof value === 'object') {
            return (
                <div key={key} style={{ marginLeft: `${indent}rem`, marginBottom: '0.5rem' }}>
                    <span style={{ fontWeight: 500, color: 'var(--text-color)' }}>{key}:</span>
                    <span style={{ marginLeft: '0.5rem', color: 'var(--text-color-secondary)' }}>Object</span>
                    <div style={{ marginTop: '0.25rem' }}>
                        {Object.keys(value).map(k => renderObjectProperty(value, k, depth + 1))}
                    </div>
                </div>
            );
        }

        return (
            <div key={key} style={{ marginLeft: `${indent}rem`, marginBottom: '0.5rem' }}>
                <span style={{ fontWeight: 500, color: 'var(--text-color)' }}>{key}:</span>
                <span style={{ marginLeft: '0.5rem' }}>{String(value)}</span>
            </div>
        );
    }, []);

    const columns = useMemo(() => {
        if (currentData.length === 0) return [];

        const firstItem = currentData[0];
        const keys = Object.keys(firstItem).filter(k => !k.startsWith('__'));

        return keys.map(key => (
            <Column
                key={key}
                field={key}
                header={key}
                sortable
                body={(rowData: any) => {
                    const value = rowData[key];
                    if (value === null || value === undefined) return '';

                    if (Array.isArray(value)) {
                        return (
                            <div
                                className="flex align-items-center gap-2 cursor-pointer"
                                onClick={() => navigateToArray(key)}
                                style={{ color: 'var(--primary-color)' }}
                            >
                                <span>Array[{value.length}]</span>
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
                                <span>Object</span>
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
        const items: NavigationItem[] = [{ name: 'Root', path: [] }];
        for (let i = 0; i < navigationPath.length; i++) {
            items.push({
                name: navigationPath[i],
                path: navigationPath.slice(0, i + 1)
            });
        }
        return items;
    }, [navigationPath]);

    return (
        <Page title="Read Models">
            <div className="p-4">
                <div className="flex gap-4 mb-4">
                    <div className="flex-1">
                        <label htmlFor="readModel" className="block mb-2">Read Model</label>
                        <Dropdown
                            id="readModel"
                            value={selectedReadModel}
                            options={allReadModels.data || []}
                            onChange={(e) => setSelectedReadModel(e.value)}
                            optionLabel="name"
                            placeholder="Select a Read Model"
                            className="w-full"
                        />
                    </div>
                    <div className="flex-1">
                        <label htmlFor="occurrence" className="block mb-2">Occurrence</label>
                        <Dropdown
                            id="occurrence"
                            value={selectedOccurrence}
                            options={occurrenceOptions}
                            onChange={(e) => setSelectedOccurrence(e.value)}
                            placeholder="Select an Occurrence"
                            className="w-full"
                            disabled={!selectedReadModel}
                        />
                    </div>
                </div>

                {navigationPath.length > 0 && (
                    <div className="px-4 py-2 mb-2 border-bottom-1 surface-border">
                        <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                            <Button
                                icon={<faIcons.FaArrowLeft />}
                                className="p-button-text p-button-sm"
                                onClick={() => navigateToBreadcrumb(navigationPath.length - 1)}
                                tooltip="Navigate Back"
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

                <div className="card">
                    <DataTable
                        value={currentData}
                        loading={instances.isPerforming}
                        emptyMessage="No instances found. Select a read model to view data."
                        className="p-datatable-sm"
                    >
                        {...columns}
                    </DataTable>

                    {instances.paging.totalItems > 0 && navigationPath.length === 0 && (
                        <Paginator
                            first={page * pageSize}
                            rows={pageSize}
                            totalRecords={instances.paging.totalItems}
                            rowsPerPageOptions={[10, 25, 50, 100]}
                            onPageChange={onPageChange}
                        />
                    )}
                </div>

                <Sidebar
                    visible={showObjectDetails}
                    position="right"
                    onHide={() => setShowObjectDetails(false)}
                    style={{ width: '450px' }}
                    className="p-sidebar-md"
                >
                    <h3 style={{ marginTop: 0 }}>Object Details</h3>
                    {selectedObject && (
                        <div style={{ overflow: 'auto' }}>
                            {Object.keys(selectedObject).map(key => renderObjectProperty(selectedObject, key))}
                        </div>
                    )}
                </Sidebar>
            </div>
        </Page>
    );
};
