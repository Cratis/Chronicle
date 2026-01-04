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
import { useState, useEffect } from 'react';
import { useParams } from 'react-router-dom';
import { EventStoreAndNamespaceParams } from 'Shared/EventStoreAndNamespaceParams';
import strings from 'Strings';
import * as faIcons from 'react-icons/fa6';
import { DataTable } from 'primereact/datatable';
import { Column } from 'primereact/column';
import { Allotment } from 'allotment';
import { AllProjectionsWithDsl, PreviewProjection, ProjectionDefinitionSyntaxError } from 'Api/Projections';
import type { ReadModelSchema } from 'Api/ReadModels';

export const Projections = () => {

    const [dslValue, setDslValue] = useState('');
    const [selectedProjection, setSelectedProjection] = useState<unknown>(null);
    const params = useParams<EventStoreAndNamespaceParams>();
    const [isCreateReadModelDialogOpen, setIsCreateReadModelDialogOpen] = useState(false);
    const [newReadModelName, setNewReadModelName] = useState('');

    const [readModels, refreshReadModels] = AllReadModelDefinitions.use({ eventStore: params.eventStore! });
    const [eventTypes] = AllEventTypesWithSchemas.use({ eventStore: params.eventStore! });
    const readModelSchemas = readModels.data?.map(readModel => JSON.parse(readModel.schema) as JsonSchema);
    const eventSchemas = eventTypes.data?.map(eventType => JSON.parse(eventType.schema) as JsonSchema);
    const [readModelInstances, setReadModelInstances] = useState<unknown[]>([]);
    const [readModelSchema, setReadModelSchema] = useState<JsonSchema | null>(null);
    const [syntaxErrors, setSyntaxErrors] = useState<ProjectionDefinitionSyntaxError[]>([]);

    const [projections] = AllProjectionsWithDsl.use({ eventStore: params.eventStore! });
    const [previewProjection] = PreviewProjection.use();
    const [createReadModel] = CreateReadModel.use();

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
                            onSelectionChange={(e) => { setSelectedProjection(e.value); setDslValue(e.value?.dsl ?? ''); }}
                            pt={{
                                root: { className: 'rounded-lg overflow-hidden' }
                            }}>

                            <Column field="readModel" header="Read Model" />
                        </DataTable>
                    </div>
                </Allotment.Pane>
                <Allotment.Pane className="h-full">

                    <div className="px-4 py-4">
                        <Menubar
                            model={[
                                {
                                    label: strings.eventStore.general.projections.actions.new,
                                    icon: <faIcons.FaPlus className='mr-2' />
                                },
                                {
                                    label: strings.eventStore.general.projections.actions.save,
                                    icon: <faIcons.FaFloppyDisk className='mr-2' />
                                },
                                {
                                    label: strings.eventStore.general.projections.actions.preview,
                                    icon: <faIcons.FaEye className='mr-2' />,
                                    command: async () => {
                                        previewProjection.eventStore = params.eventStore!;
                                        previewProjection.namespace = params.namespace!;
                                        previewProjection.dsl = dslValue;
                                        const result = await previewProjection.execute();

                                        setReadModelInstances(result.response?.readModelEntries ?? []);
                                        setReadModelSchema(result.response?.schema ?? null);
                                        setSyntaxErrors(result.response?.syntaxErrors ?? []);
                                    }
                                }
                            ]}
                        />

                        <div className="py-4">
                            <ProjectionEditor
                                value={dslValue}
                                onChange={setDslValue}
                                readModelSchemas={readModelSchemas}
                                eventSchemas={eventSchemas}
                                errors={syntaxErrors}
                                height="500px"
                                theme="vs-dark"
                                eventStore={params.eventStore}
                                namespace={params.namespace}
                            />
                        </div>

                        <div style={{ flex: 1, overflow: 'auto', padding: '1rem' }}>
                            <DataTable
                                value={readModelInstances as never[]}
                                emptyMessage={strings.eventStore.general.projections.empty}
                                className="p-datatable-sm"
                                pt={{
                                    root: { className: 'rounded-lg overflow-hidden', style: { border: 'none' } },
                                    tbody: { style: { borderTop: '1px solid var(--surface-border)' } }
                                }}>
                                {readModelSchema?.properties && Object.keys(readModelSchema.properties).map((_, index) => (
                                    <Column key={index} field={_} header={_} />
                                ))}
                            </DataTable>
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
        </Page>
    );
};
