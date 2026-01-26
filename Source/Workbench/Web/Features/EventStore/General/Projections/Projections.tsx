// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AllEventTypesWithSchemas } from 'Api/EventTypes';
import { AllReadModelDefinitions, CreateReadModel } from 'Api/ReadModelTypes';
import { Page } from 'Components/Common/Page';
import { JsonSchema } from 'Components/JsonSchema';
import { ProjectionEditor, setCreateReadModelCallback } from 'Components/ProjectionEditor';
import { ReadModelCreation } from 'Components/ReadModelCreation';
import { Menubar } from 'primereact/menubar';
import { Dialog } from 'primereact/dialog';
import { useState, useEffect, useMemo } from 'react';
import { useParams } from 'react-router-dom';
import { EventStoreAndNamespaceParams } from 'Shared/EventStoreAndNamespaceParams';
import strings from 'Strings';
import * as faIcons from 'react-icons/fa6';
import { DataTable } from 'primereact/datatable';
import { Column } from 'primereact/column';
import { Allotment } from 'allotment';
import { AllProjectionsWithDsl, PreviewProjection, ProjectionDefinitionSyntaxError, SaveProjection } from 'Api/Projections';
import type { ReadModelSchema } from 'Api/ReadModels';
import { ReadModelInstance } from 'Api/ReadModels';
import { FluxCapacitor } from 'Icons';
import { useDialog } from '@cratis/arc.react/dialogs';
import { TimeMachineDialog, ReadModelInstances } from 'Components';
import { Json } from 'Features';

export const Projections = () => {

    const [dslValue, setDslValue] = useState('');
    const [selectedProjection, setSelectedProjection] = useState<unknown>(null);
    const params = useParams<EventStoreAndNamespaceParams>();
    const [isCreateReadModelDialogOpen, setIsCreateReadModelDialogOpen] = useState(false);
    const [newReadModelName, setNewReadModelName] = useState('');
    const [selectedInstance, setSelectedInstance] = useState<Json | null>(null);
    const [page, setPage] = useState(0);
    const [pageSize, setPageSize] = useState(50);

    const [readModels, refreshReadModels] = AllReadModelDefinitions.use({ eventStore: params.eventStore! });
    const [eventTypes] = AllEventTypesWithSchemas.use({ eventStore: params.eventStore! });
    const readModelSchemas = readModels.data?.map(readModel => JSON.parse(readModel.schema) as JsonSchema);
    const eventSchemas = eventTypes.data?.map(eventType => JSON.parse(eventType.schema) as JsonSchema);
    const [readModelInstances, setReadModelInstances] = useState<ReadModelInstance[]>([]);
    const [syntaxErrors, setSyntaxErrors] = useState<ProjectionDefinitionSyntaxError[]>([]);

    const [projections, refreshProjections] = AllProjectionsWithDsl.use({ eventStore: params.eventStore! });
    const [previewProjection] = PreviewProjection.use();
    const [saveProjection] = SaveProjection.use();
    const [createReadModel] = CreateReadModel.use();
    const [TimeMachineDialogWrapper, showTimeMachineDialog] = useDialog(TimeMachineDialog);

    const selectedReadModel = useMemo(() => {
        if (!selectedProjection || !readModels.data) return null;
        const projection = selectedProjection as { readModel: string };
        const found = readModels.data.find(rm => rm.identifier.endsWith(`.${projection.readModel}`) || rm.identifier === projection.readModel) || null;
        return found;
    }, [selectedProjection, readModels.data]);

    useEffect(() => {
        setCreateReadModelCallback((readModelName: string) => {
            setNewReadModelName(readModelName);
            setIsCreateReadModelDialogOpen(true);
        });
    }, []);

    const handleSaveReadModel = async (name: string, schema: ReadModelSchema) => {
        if (params.eventStore) {
            createReadModel.eventStore = params.eventStore;
            createReadModel.name = name;
            createReadModel.schema = JSON.stringify(schema);
            const result = await createReadModel.execute();
            if (result.isSuccess) {
                setIsCreateReadModelDialogOpen(false);
                setNewReadModelName('');
                refreshReadModels();
            }
        }
    };

    const handleCancelReadModel = () => {
        setIsCreateReadModelDialogOpen(false);
        setNewReadModelName('');
    };

    return (
        <Page title='Projections'>
            <Allotment className="h-full" proportionalLayout={false}>
                <Allotment.Pane preferredSize="270px">
                    <div className="px-4 py-4">
                        <DataTable
                            value={projections.data}
                            selectionMode="single"
                            selection={selectedProjection as never}
                            onSelectionChange={(e) => {
                                setSelectedProjection(e.value);
                                setDslValue(e.value?.dsl ?? '');
                                setReadModelInstances([]);
                                setSelectedInstance(null);
                                setPage(0);
                            }}
                            pt={{
                                root: { className: 'rounded-lg overflow-hidden' }
                            }}>

                            <Column field="readModel" header="Read Model" />
                        </DataTable>
                    </div>
                </Allotment.Pane>
                <Allotment.Pane className="h-full">
                    <div className="px-4 py-4" style={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
                        <Menubar
                            model={[
                                {
                                    label: strings.eventStore.general.projections.actions.new,
                                    icon: <faIcons.FaPlus className='mr-2' />,
                                    command: () => {
                                        setSelectedProjection(null);
                                        setDslValue('');
                                        setReadModelInstances([]);
                                        setSyntaxErrors([]);
                                        setSelectedInstance(null);
                                        setPage(0);
                                    }
                                },
                                {
                                    label: strings.eventStore.general.projections.actions.save,
                                    icon: <faIcons.FaFloppyDisk className='mr-2' />,
                                    command: async () => {
                                        saveProjection.eventStore = params.eventStore!;
                                        saveProjection.namespace = params.namespace!;
                                        saveProjection.dsl = dslValue;
                                        const result = await saveProjection.execute();
                                        if (result.isSuccess) {
                                            refreshProjections();
                                        }
                                    }
                                },
                                {
                                    label: strings.eventStore.general.projections.actions.preview,
                                    icon: <faIcons.FaEye className='mr-2' />,
                                    command: async () => {
                                        previewProjection.eventStore = params.eventStore!;
                                        previewProjection.namespace = params.namespace!;
                                        previewProjection.dsl = dslValue;
                                        const result = await previewProjection.execute();

                                        const instances = (result.response?.readModelEntries ?? []).map((entry: unknown) => {
                                            const instance = new ReadModelInstance();
                                            instance.instance = entry as Record<string, unknown>;
                                            return instance;
                                        });
                                        setReadModelInstances(instances);
                                        setSyntaxErrors(result.response?.syntaxErrors ?? []);
                                        setSelectedInstance(null);
                                        setPage(0);
                                    }
                                },
                                {
                                    label: strings.eventStore.general.projections.actions.timeMachine,
                                    icon: <FluxCapacitor size={20} />,
                                    command: async () => {
                                        await showTimeMachineDialog();
                                    },
                                    disabled: !selectedProjection || !selectedInstance
                                }
                            ]}
                        />

                        <div className="pt-4" style={{ height: '500px', flexShrink: 0 }}>
                            <ProjectionEditor
                                value={dslValue}
                                onChange={setDslValue}
                                readModelSchemas={readModelSchemas}
                                eventSchemas={eventSchemas}
                                errors={syntaxErrors}
                                theme="vs-dark"
                                eventStore={params.eventStore}
                                namespace={params.namespace}
                            />
                        </div>

                        <div className="pt-4" style={{ flex: 1, minHeight: 0 }}>
                            <ReadModelInstances
                                instances={readModelInstances}
                                page={page}
                                pageSize={pageSize}
                                totalItems={readModelInstances.length}
                                isPerforming={false}
                                setPage={setPage}
                                setPageSize={setPageSize}
                                selectedInstance={selectedInstance}
                                setSelectedInstance={setSelectedInstance}
                            />
                        </div>
                    </div>
                </Allotment.Pane>
            </Allotment>

            <Dialog
                header={'Create Read Model: ' + newReadModelName}
                visible={isCreateReadModelDialogOpen}
                style={{ width: '800px', height: '80vh' }}
                modal
                resizable={true}
                onHide={handleCancelReadModel}>
                <ReadModelCreation
                    initialName={newReadModelName}
                    onSave={handleSaveReadModel}
                    onCancel={handleCancelReadModel}
                />
            </Dialog>

            <>
                {selectedReadModel && selectedInstance && (
                    <TimeMachineDialogWrapper
                        readModel={selectedReadModel}
                        readModelKey={typeof selectedInstance === 'object' && selectedInstance !== null && 'id' in selectedInstance ? selectedInstance.id as string : ''}
                    />
                )}
            </>
        </Page>
    );
};
