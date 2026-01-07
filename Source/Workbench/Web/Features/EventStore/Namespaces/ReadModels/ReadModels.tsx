// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState, useMemo, useEffect, useCallback, useRef } from 'react';
import { useParams, useNavigate, Routes, Route } from 'react-router-dom';
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
import { FluxCapacitor } from 'Icons';
import { Json } from 'Features';
import { useDialog } from '@cratis/arc.react/dialogs';
import { TimeMachineDialog } from 'Components';

interface ReadModelsRouteParams {
    readonly readModel?: string;
    readonly occurrence?: string;
    readonly instance?: string;
}

type ReadModelsParams = EventStoreAndNamespaceParams & ReadModelsRouteParams;

const useReadModelsParams = (): Readonly<Partial<ReadModelsParams>> => {
    return useParams<ReadModelsParams>();
};

const ReadModelsContent = () => {
    const params = useReadModelsParams();
    const navigate = useNavigate();
    const [allReadModels] = AllReadModelDefinitions.use({ eventStore: params.eventStore! });
    const filterPanelRef = useRef<OverlayPanel>(null);

    const [selectedReadModel, setSelectedReadModel] = useState<ReadModelDefinition | null>(null);
    const [selectedOccurrence, setSelectedOccurrence] = useState<string | null>(null);
    const [selectedInstance, setSelectedInstance] = useState<Json | null>(null);
    const [TimeMachineDialogWrapper, showTimeMachineDialog] = useDialog(TimeMachineDialog);
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
        if (params.readModel && allReadModels.data.length > 0 && !selectedReadModel) {
            const readModel = allReadModels.data.find(rm => rm.identifier === params.readModel);
            if (readModel) {
                setSelectedReadModel(readModel);
            }
        }

        if (params.occurrence && !selectedOccurrence) {
            setSelectedOccurrence(params.occurrence);
        }

        if (params.instance && !selectedInstance) {
            setSelectedInstance({ id: params.instance });
        }
    }, [params, allReadModels.data]);

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

    useEffect(() => {
        let newPath = `/event-store/${params.eventStore}/${params.namespace}/read-models`;

        if (selectedReadModel) {
            newPath += `/${selectedReadModel.identifier}`;

            if (selectedOccurrence) {
                newPath += `/${selectedOccurrence}`;

                if (selectedInstance && typeof selectedInstance === 'object' && selectedInstance !== null && 'id' in selectedInstance) {
                    newPath += `/${selectedInstance.id}`;
                }
            }
        }

        const currentPath = window.location.pathname;
        if (newPath !== currentPath) {
            navigate(newPath, { replace: true });
        }
    }, [selectedReadModel, selectedOccurrence, selectedInstance, navigate, params.eventStore, params.namespace]);

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
                        },
                        {
                            label: strings.eventStore.namespaces.readModels.actions.timeMachine,
                            icon: <FluxCapacitor size={20} />,
                            command: async () => {
                                await showTimeMachineDialog();
                            },
                            disabled: !selectedReadModel || !selectedOccurrence || !selectedInstance
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
                        instances={instances.data}
                        page={page}
                        pageSize={pageSize}
                        totalItems={instances.paging.totalItems}
                        isPerforming={instances.isPerforming}
                        setPage={setPage}
                        setPageSize={setPageSize}
                        selectedInstance={selectedInstance}
                        setSelectedInstance={setSelectedInstance}
                    />
                </Allotment>
            </div>

            {(selectedReadModel && selectedOccurrence && selectedInstance) && (
                <TimeMachineDialogWrapper
                    readModel={selectedReadModel}
                    readModelKey={typeof selectedInstance === 'object' && selectedInstance !== null && 'id' in selectedInstance ? selectedInstance.id as string : ''} />
            )}

        </Page>
    );
};

export const ReadModels = () => {
    return (
        <Routes>
            <Route path="/" element={<ReadModelsContent />} />
            <Route path=":readModel" element={<ReadModelsContent />} />
            <Route path=":readModel/:occurrence" element={<ReadModelsContent />} />
            <Route path=":readModel/:occurrence/:instance" element={<ReadModelsContent />} />
        </Routes>
    );
};
