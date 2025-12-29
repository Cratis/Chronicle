// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState, useMemo, useEffect, useCallback, useRef } from 'react';
import { useParams } from 'react-router-dom';
import { Dropdown } from 'primereact/dropdown';
import { Button } from 'primereact/button';
import { DataTable } from 'primereact/datatable';
import { Column } from 'primereact/column';
import { Paginator, PaginatorPageChangeEvent } from 'primereact/paginator';
import { OverlayPanel } from 'primereact/overlaypanel';
import { Allotment } from 'allotment';
import { Page } from 'Components';
import { AllReadModelDefinitions, ReadModelDefinition } from 'Api/ReadModelTypes';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { ReadModelOccurrence, ReadModelOccurrences, ReadModelInstances } from 'Api/ReadModels';
import * as faIcons from 'react-icons/fa6';
import { Menubar } from 'primereact/menubar';
import strings from 'Strings';

interface NavigationItem {
    name: string;
    path: string[];
}

export const ReadModels = () => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const [allReadModels] = AllReadModelDefinitions.use({ eventStore: params.eventStore! });
    const filterPanelRef = useRef<OverlayPanel>(null);

    const [selectedReadModel, setSelectedReadModel] = useState<ReadModelDefinition | null>(null);
    const [selectedOccurrence, setSelectedOccurrence] = useState<ReadModelOccurrence | null>(null);
    const [page, setPage] = useState(0);
    const [pageSize, setPageSize] = useState(50);
    const [navigationPath, setNavigationPath] = useState<string[]>([]);
    const [selectedObject, setSelectedObject] = useState<any>(null);
    const [objectNavigationPath, setObjectNavigationPath] = useState<string[]>([]);

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

    const [instances, performInstancesQuery, , clearInstances] = ReadModelInstances.useWithPaging(pageSize, instancesArgs);

    const executeQuery = useCallback(() => {
        if (selectedReadModel && selectedOccurrence) {
            setPage(0);
            setNavigationPath([]);
            setSelectedObject(null);
            setObjectNavigationPath([]);
            performInstancesQuery(instancesArgs);
        }
    }, [selectedReadModel, selectedOccurrence, instancesArgs, performInstancesQuery]);

    const handleReadModelChange = useCallback((readModel: ReadModelDefinition | null) => {
        setSelectedReadModel(readModel);
        setSelectedOccurrence(null);
        clearInstances();
        setNavigationPath([]);
        setSelectedObject(null);
        setObjectNavigationPath([]);
    }, [clearInstances]);

    const handleOccurrenceChange = useCallback((occurrence: ReadModelOccurrence | null) => {
        setSelectedOccurrence(occurrence);
        clearInstances();
        setNavigationPath([]);
        setSelectedObject(null);
        setObjectNavigationPath([]);

        if (occurrence) {
            filterPanelRef.current?.hide();
        }
    }, [clearInstances]);

    useEffect(() => {
        if (occurrences.data.length > 0 && !selectedOccurrence) {
            const defaultOccurrence = occurrences.data.find(occ => occ.revertModel === 'Default');
            if (defaultOccurrence) {
                setSelectedOccurrence(defaultOccurrence);
            }
        }
    }, [occurrences.data, selectedOccurrence]);

    // Debug: log DOM layout info to help diagnose height/scroll issues
    useEffect(() => {
        const runDebug = () => {
            try {
                const selectors = [
                    'div.px-6.py-4', // Page root
                    'main.panel', // Page main
                    '.allotment', // Allotment root
                    '.allotment .allotment-pane', // Allotment pane
                    'div.p-4', // inner container
                    'div.card', // card
                    '.p-datatable', // PrimeReact DataTable
                ];

                console.group('ReadModels layout debug');
                selectors.forEach(sel => {
                    const el = document.querySelector(sel) as HTMLElement | null;
                    if (!el) {
                        console.log(`${sel}: not found`);
                        return;
                    }
                    const rect = el.getBoundingClientRect();
                    const cs = window.getComputedStyle(el);
                    console.log(sel, {
                        rect: { width: rect.width, height: rect.height, top: rect.top, left: rect.left },
                        display: cs.display,
                        position: cs.position,
                        height: cs.height,
                        minHeight: cs.minHeight,
                        maxHeight: cs.maxHeight,
                        flex: cs.flex,
                        overflow: cs.overflow,
                    });
                });
                console.groupEnd();
            } catch (err) {
                console.error('ReadModels layout debug failed', err);
            }
        };

        // Delay slightly to allow layout to settle
        const t = setTimeout(runDebug, 300);
        return () => clearTimeout(t);
    }, []);

    useEffect(() => {
        if (selectedReadModel && selectedOccurrence) {
            setPage(0);
            setNavigationPath([]);
            setSelectedObject(null);
            setObjectNavigationPath([]);
            performInstancesQuery(instancesArgs);
        }
    }, [selectedOccurrence, selectedReadModel]);

    const onPageChange = (event: PaginatorPageChangeEvent) => {
        setPage(event.page);
        setPageSize(event.rows);
    };

    const occurrenceOptions = useMemo(() => {
        let options = occurrences.data.map(occ => ({
            label: occ.revertModel === 'Default'
                ? `${strings.eventStore.namespaces.readModels.labels.default} (${strings.eventStore.namespaces.readModels.labels.generation} ${occ.generation})`
                : `${new Date(occ.occurred).toLocaleString()} (${strings.eventStore.namespaces.readModels.labels.generation} ${occ.generation})`,
            value: occ
        }));

        // Ensure Default is always present
        const hasDefault = options.some(opt => opt.value.revertModel === 'Default');
        if (!hasDefault && selectedReadModel) {
            options = [{
                label: `${strings.eventStore.namespaces.readModels.labels.default} (${strings.eventStore.namespaces.readModels.labels.generation} 1)`,
                value: {
                    revertModel: 'Default',
                    generation: 1,
                    occurred: new Date()
                } as ReadModelOccurrence
            }, ...options];
        } else {
            // Sort to ensure Default is first
            options.sort((a, b) => {
                if (a.value.revertModel === 'Default') return -1;
                if (b.value.revertModel === 'Default') return 1;
                return 0;
            });
        }

        return options;
    }, [occurrences, selectedReadModel]);

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
        setObjectNavigationPath([]);
    }, []);

    const getCurrentObjectForDetails = useCallback(() => {
        if (!selectedObject) return null;

        let current = selectedObject;
        for (const key of objectNavigationPath) {
            if (current && typeof current === 'object' && key in current) {
                current = current[key];
            } else {
                return null;
            }
        }
        return current;
    }, [selectedObject, objectNavigationPath]);

    const navigateToObjectProperty = useCallback((key: string) => {
        setObjectNavigationPath([...objectNavigationPath, key]);
    }, [objectNavigationPath]);

    const navigateBackInObject = useCallback(() => {
        if (objectNavigationPath.length > 0) {
            setObjectNavigationPath(objectNavigationPath.slice(0, -1));
        }
    }, [objectNavigationPath]);

    const navigateToObjectBreadcrumb = useCallback((index: number) => {
        setObjectNavigationPath(objectNavigationPath.slice(0, index));
    }, [objectNavigationPath]);



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
                body={(rowData: Record<string, unknown>) => {
                    const value = rowData[key];
                    if (value === null || value === undefined) return '';

                    if (Array.isArray(value)) {
                        return (
                            <div
                                className="flex align-items-center gap-2 cursor-pointer"
                                onClick={() => navigateToArray(key)}
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
        const items: NavigationItem[] = [{ name: strings.eventStore.namespaces.readModels.labels.root, path: [] }];
        for (let i = 0; i < navigationPath.length; i++) {
            items.push({
                name: navigationPath[i],
                path: navigationPath.slice(0, i + 1)
            });
        }
        return items;
    }, [navigationPath]);

    const objectBreadcrumbItems = useMemo(() => {
        const items: NavigationItem[] = [{ name: strings.eventStore.namespaces.readModels.labels.root, path: [] }];
        for (let i = 0; i < objectNavigationPath.length; i++) {
            items.push({
                name: objectNavigationPath[i],
                path: objectNavigationPath.slice(0, i + 1)
            });
        }
        return items;
    }, [objectNavigationPath]);

    const currentDetailsObject = getCurrentObjectForDetails();
    const detailsProperties = useMemo(() => {
        if (!currentDetailsObject || typeof currentDetailsObject !== 'object') return [];
        return Object.keys(currentDetailsObject).filter(k => !k.startsWith('__'));
    }, [currentDetailsObject]);

    return (
        <Page title={strings.eventStore.namespaces.readModels.title}>
            <div className="px-4 py-2">
                <Menubar
                    model={[
                        {
                            label: strings.eventStore.namespaces.readModels.actions.filter,
                            icon: <faIcons.FaFilter className='mr-2' />,
                            command: (e) => filterPanelRef.current?.toggle(e.originalEvent)
                        },
                        {
                            label: strings.eventStore.namespaces.readModels.actions.query,
                            icon: <faIcons.FaArrowsRotate className='mr-2' />,
                            command: executeQuery,
                            disabled: !selectedReadModel || !selectedOccurrence
                        }
                    ]}
                />
                <OverlayPanel ref={filterPanelRef}>
                    <div className="flex flex-column gap-3">
                        <div>
                            <label htmlFor="readModel" className="block mb-2 font-semibold">{strings.eventStore.namespaces.readModels.labels.readModel}</label>
                            <Dropdown
                                id="readModel"
                                value={selectedReadModel}
                                options={allReadModels.data || []}
                                onChange={(e) => handleReadModelChange(e.value)}
                                optionLabel="name"
                                placeholder={strings.eventStore.namespaces.readModels.placeholders.selectReadModel}
                                className="w-full"
                            />
                        </div>
                        <div>
                            <label htmlFor="occurrence" className="block mb-2 font-semibold">{strings.eventStore.namespaces.readModels.labels.occurrence}</label>
                            <Dropdown
                                id="occurrence"
                                value={selectedOccurrence}
                                options={occurrenceOptions}
                                onChange={(e) => handleOccurrenceChange(e.value)}
                                placeholder={strings.eventStore.namespaces.readModels.placeholders.selectOccurrence}
                                className="w-full"
                                disabled={!selectedReadModel}
                            />
                        </div>
                    </div>
                </OverlayPanel>
            </div>

            <Allotment className="h-full" proportionalLayout={false} style={{ height: '100%' }}>
                <Allotment.Pane className="flex-grow" style={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
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

                        <div className="card" style={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
                            <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minHeight: 0 }}>
                                <DataTable
                                    value={currentData}
                                    loading={instances.isPerforming}
                                    emptyMessage={strings.eventStore.namespaces.readModels.empty}
                                    className="p-datatable-sm"
                                    selectionMode="single"
                                    onRowClick={(e) => {
                                        const rowData = { ...e.data };
                                        // Remove internal properties before showing
                                        delete rowData.__arrayIndex;
                                        delete rowData.__sourceInstance;
                                        handleObjectClick(rowData);
                                    }}
                                    scrollable
                                    scrollHeight="flex"
                                    style={{ minWidth: '100%', height: '100%' }}
                                >
                                    {...columns}
                                </DataTable>
                            </div>

                            {instances.paging.totalItems > 0 && navigationPath.length === 0 && (
                                <div style={{ flexShrink: 0 }}>
                                    <Paginator
                                        first={page * pageSize}
                                        rows={pageSize}
                                        totalRecords={instances.paging.totalItems}
                                        rowsPerPageOptions={[10, 25, 50, 100]}
                                        onPageChange={onPageChange}
                                    />
                                </div>
                            )}
                        </div>
                    </div>
                </Allotment.Pane>

                {selectedObject && (
                    <Allotment.Pane preferredSize="450px">
                        <div style={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
                            <div className="px-4 py-2 border-bottom-1 surface-border">
                                <h3 style={{ margin: 0 }}>{strings.eventStore.namespaces.readModels.labels.objectDetails}</h3>
                            </div>

                            {objectNavigationPath.length > 0 && (
                                <div className="px-4 py-2 border-bottom-1 surface-border">
                                    <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                                        <Button
                                            icon={<faIcons.FaArrowLeft />}
                                            className="p-button-text p-button-sm"
                                            onClick={navigateBackInObject}
                                            tooltip={strings.eventStore.namespaces.readModels.actions.navigateBack}
                                            tooltipOptions={{ position: 'top' }}
                                        />
                                        <div style={{ fontSize: '0.9rem', color: 'var(--text-color-secondary)' }}>
                                            {objectBreadcrumbItems.map((item, index) => (
                                                <span key={index}>
                                                    {index > 0 && <span className="mx-2">&gt;</span>}
                                                    <span
                                                        onClick={() => navigateToObjectBreadcrumb(index)}
                                                        style={{
                                                            cursor: 'pointer',
                                                            textDecoration: index < objectBreadcrumbItems.length - 1 ? 'underline' : 'none'
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

                            <div style={{ flex: 1, overflow: 'auto', padding: '1rem' }}>
                                <DataTable
                                    value={detailsProperties.map(key => ({ key, value: currentDetailsObject[key] }))}
                                    emptyMessage={strings.eventStore.namespaces.readModels.empty}
                                    className="p-datatable-sm"
                                    pt={{
                                        root: { style: { border: 'none' } },
                                        tbody: { style: { borderTop: '1px solid var(--surface-border)' } },
                                        bodyCell: { style: { height: '3rem', padding: '0 0.75rem', verticalAlign: 'middle' } }
                                    }}
                                >
                                    <Column
                                        field="key"
                                        header={strings.components.schemaEditor.columns.property}
                                        style={{ width: '40%', fontWeight: 500 }}
                                    />
                                    <Column
                                        field="value"
                                        header="Value"
                                        body={(rowData: { key: string; value: any }) => {
                                            const value = rowData.value;

                                            if (value === null || value === undefined) {
                                                return <span style={{ fontStyle: 'italic', color: 'var(--text-color-secondary)' }}>{strings.eventStore.namespaces.readModels.labels.null}</span>;
                                            }

                                            if (Array.isArray(value)) {
                                                return (
                                                    <span style={{ color: 'var(--text-color-secondary)' }}>
                                                        {strings.eventStore.namespaces.readModels.labels.array}[{value.length}]
                                                    </span>
                                                );
                                            }

                                            if (typeof value === 'object') {
                                                return (
                                                    <div
                                                        className="flex align-items-center gap-2 w-full cursor-pointer"
                                                        onClick={() => navigateToObjectProperty(rowData.key)}
                                                        style={{ color: 'var(--primary-color)' }}
                                                    >
                                                        <span>{strings.eventStore.namespaces.readModels.labels.object}</span>
                                                        <div style={{ flex: 1 }} />
                                                        <faIcons.FaArrowRight style={{ fontSize: '1rem' }} />
                                                    </div>
                                                );
                                            }

                                            return String(value);
                                        }}
                                        style={{ width: '60%' }}
                                    />
                                </DataTable>
                            </div>
                        </div>
                    </Allotment.Pane>
                )}
            </Allotment>
        </Page>
    );
};
