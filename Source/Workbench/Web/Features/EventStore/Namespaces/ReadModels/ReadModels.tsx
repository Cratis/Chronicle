// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState, useMemo, useEffect } from 'react';
import { useParams } from 'react-router-dom';
import { Dropdown } from 'primereact/dropdown';
import { Button } from 'primereact/button';
import { DataTable } from 'primereact/datatable';
import { Column } from 'primereact/column';
import { Paginator, PaginatorPageChangeEvent } from 'primereact/paginator';
import { Page } from 'Components';
import { AllReadModelDefinitions, ReadModelDefinition } from 'Api/ReadModelTypes';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { ReadModelOccurrence, ReadModelOccurrences, ReadModelInstances } from 'Api/ReadModels';

export const ReadModels = () => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const [allReadModels] = AllReadModelDefinitions.use({ eventStore: params.eventStore! });

    const [selectedReadModel, setSelectedReadModel] = useState<ReadModelDefinition | null>(null);
    const [selectedOccurrence, setSelectedOccurrence] = useState<ReadModelOccurrence | null>(null);
    const [page, setPage] = useState(0);
    const [pageSize, setPageSize] = useState(50);

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
    const handleExecuteQuery = () => {
        setPage(0);
        performInstancesQuery(instancesArgs);
    };

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

    const columns = useMemo(() => {
        if (instances.data == null || instances.data.length === 0) return [];

        const firstInstance = instances.data[0];
        return Object.keys(firstInstance.instance).map(key => (
            <Column
                key={key}
                field={`instance.${key}`}
                header={key}
                sortable
                body={(rowData) => {
                    const value = rowData.instance[key];
                    if (value === null || value === undefined) return '';
                    if (typeof value === 'object') return JSON.stringify(value);
                    return String(value);
                }}
            />
        ));
    }, [instances]);

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
                    <div className="flex items-end">
                        <Button
                            label="Execute Query"
                            icon="pi pi-search"
                            onClick={handleExecuteQuery}
                            disabled={!selectedReadModel || !selectedOccurrence}
                        />
                    </div>
                </div>

                <div className="card">
                    <DataTable
                        value={instances.data}
                        loading={instances.isPerforming}
                        emptyMessage="No instances found. Select a read model and click Execute Query."
                        className="p-datatable-sm"
                    >
                        {...columns}
                    </DataTable>

                    {instances.paging.totalItems > 0 && (
                        <Paginator
                            first={page * pageSize}
                            rows={pageSize}
                            totalRecords={instances.paging.totalItems}
                            rowsPerPageOptions={[10, 25, 50, 100]}
                            onPageChange={onPageChange}
                        />
                    )}
                </div>
            </div>
        </Page>
    );
};
