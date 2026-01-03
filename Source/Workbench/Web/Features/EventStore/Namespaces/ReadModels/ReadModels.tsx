// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState, useMemo, useEffect, useCallback, useRef } from 'react';
import { useParams } from 'react-router-dom';
import { Dropdown } from 'primereact/dropdown';
import { OverlayPanel } from 'primereact/overlaypanel';
import ReadModelInstances from './ReadModelInstances';
import { Allotment } from 'allotment';
import { Page } from 'Components';
import { AllReadModelDefinitions, ReadModelDefinition } from 'Api/ReadModelTypes';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { ReadModelOccurrences, ReadModelInstances as ReadModelInstancesApi } from 'Api/ReadModels';
import * as faIcons from 'react-icons/fa6';
import { Menubar } from 'primereact/menubar';
import strings from 'Strings';

export const ReadModels = () => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const [allReadModels] = AllReadModelDefinitions.use({ eventStore: params.eventStore! });
    const filterPanelRef = useRef<OverlayPanel>(null);

    const [selectedReadModel, setSelectedReadModel] = useState<ReadModelDefinition | null>(null);
    const [selectedOccurrence, setSelectedOccurrence] = useState<string | null>(null);
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
        occurrence: selectedOccurrence && selectedOccurrence !== 'Default' ? selectedOccurrence : undefined
    }), [params.eventStore, params.namespace, selectedReadModel, selectedOccurrence]);

    const [instances, performInstancesQuery] = ReadModelInstancesApi.useWithPaging(pageSize, instancesArgs);

    const executeQuery = useCallback(() => {
        if (selectedReadModel && selectedOccurrence) {
            setPage(0);
            performInstancesQuery(instancesArgs);
        }
    }, [selectedReadModel, selectedOccurrence, instancesArgs, performInstancesQuery]);

    const handleReadModelChange = useCallback((readModel: ReadModelDefinition | null) => {
        setSelectedReadModel(readModel);
        setSelectedOccurrence(null);
    }, []);

    const handleOccurrenceChange = useCallback((occurrence: string | null) => {
        setSelectedOccurrence(occurrence);

        if (occurrence) {
            filterPanelRef.current?.hide();
        }
    }, []);

    const occurrenceOptions = useMemo(() => {
        let options = occurrences.data.map(occ => ({
            label: occ.revertModel === 'Default'
                ? `${strings.eventStore.namespaces.readModels.labels.default} (${strings.eventStore.namespaces.readModels.labels.generation} ${occ.generation})`
                : `${new Date(occ.occurred).toLocaleString()} (${strings.eventStore.namespaces.readModels.labels.generation} ${occ.generation})`,
            value: occ.revertModel
        }));

        // Ensure Default is always present
        const hasDefault = options.some(opt => opt.value === 'Default');
        if (!hasDefault && selectedReadModel) {
            options = [{
                label: `${strings.eventStore.namespaces.readModels.labels.default} (${strings.eventStore.namespaces.readModels.labels.generation} 1)`,
                value: 'Default'
            }, ...options];
        }

        // Sort to ensure Default is first
        options.sort((a, b) => {
            if (a.value === 'Default') return -1;
            if (b.value === 'Default') return 1;
            return 0;
        });

        return options;
    }, [occurrences.data, selectedReadModel]);

    useEffect(() => {
        if (occurrenceOptions.length > 0 && !selectedOccurrence) {
            const defaultOccurrence = occurrenceOptions.find(occ => occ.value === 'Default')?.value;
            if (defaultOccurrence) {
                setSelectedOccurrence(defaultOccurrence as string);
            }
        }
    }, [occurrenceOptions, selectedOccurrence, selectedReadModel]);

    useEffect(() => {
        if (selectedReadModel && selectedOccurrence) {
            setPage(0);
            performInstancesQuery(instancesArgs);
        }
    }, [selectedOccurrence, selectedReadModel]);

    return (
        <Page title={strings.eventStore.namespaces.readModels.title}>
            <div className="px-4 py-4">
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

            <div className="h-full" style={{ height: '100%' }}>
                <Allotment className="h-full" proportionalLayout={false}>
                    <ReadModelInstances
                        instances={instances}
                        page={page}
                        pageSize={pageSize}
                        setPage={setPage}
                        setPageSize={setPageSize}
                    />
                </Allotment>
            </div>
        </Page>
    );
};
