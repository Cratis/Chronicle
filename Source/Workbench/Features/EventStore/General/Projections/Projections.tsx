// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AllEventTypesWithSchemas } from 'Api/EventTypes';
import { AllReadModelDefinitions, ReadModelSource } from 'Api/ReadModelTypes';
import { Page } from 'Components/Common/Page';
import type { JsonSchema } from 'Components/JsonSchema';
import { ProjectionEditor, setCreateReadModelCallback, setEditReadModelCallback, setDraftReadModel as setDraftReadModelInProvider } from 'Components/ProjectionEditor';
import { ReadModelTypeEditor } from 'Components/ReadModelTypeEditor/ReadModelTypeEditor';
import { Menubar } from 'primereact/menubar';
import { Tooltip } from 'primereact/tooltip';
import { Dialog } from 'primereact/dialog';
import { Button } from 'primereact/button';
import { useState, useEffect, useMemo } from 'react';
import type { MenuItem } from 'primereact/menuitem';
import { useParams } from 'react-router-dom';
import { EventStoreAndNamespaceParams } from 'Shared/EventStoreAndNamespaceParams';
import strings from 'Strings';
import * as faIcons from 'react-icons/fa6';
import { DataTable } from 'primereact/datatable';
import { Column } from 'primereact/column';
import { Allotment } from 'allotment';
import { AllProjectionsWithDeclarations, DraftReadModel, PreviewProjection, ProjectionDeclarationSyntaxError, SaveProjection } from 'Api/Projections';
import { ReadModelInstance } from 'Api/ReadModels';
import { FluxCapacitor } from 'Icons';
import { useDialog, useConfirmationDialog, DialogResult, DialogButtons } from '@cratis/arc.react/dialogs';
import { TimeMachineDialog, ReadModelInstances } from 'Components';
import { Json } from 'Features';

const projectionDeclarationPattern = /^(\s*projection\s+[\w.]+\s*=>\s*)([\w.]+)/m;

const resolveReadModelIdentifier = (
    token: string | null,
    readModels: { identifier: string; displayName: string }[] | undefined,
    draft: DraftReadModel | null
): string | null => {
    if (!token) return null;
    if (draft && (draft.identifier === token || draft.displayName === token)) {
        return draft.identifier;
    }
    const byIdentifier = readModels?.find(rm => rm.identifier === token);
    if (byIdentifier) return byIdentifier.identifier;
    const byDisplayName = readModels?.find(rm => rm.displayName === token);
    return byDisplayName?.identifier ?? null;
};

const resolveReadModelDisplayName = (
    token: string | null,
    readModels: { identifier: string; displayName: string }[] | undefined,
    draft: DraftReadModel | null
): string | null => {
    if (!token) return null;
    if (draft && (draft.identifier === token || draft.displayName === token)) {
        return draft.displayName;
    }
    const byIdentifier = readModels?.find(rm => rm.identifier === token);
    if (byIdentifier) return byIdentifier.displayName;
    const byDisplayName = readModels?.find(rm => rm.displayName === token);
    return byDisplayName?.displayName ?? token;
};

const replaceReadModelToken = (
    declaration: string,
    resolver: (token: string | null) => string | null
): string => {
    const match = declaration.match(projectionDeclarationPattern);
    if (!match) return declaration;
    const resolved = resolver(match[2]);
    if (!resolved || resolved === match[2]) return declaration;
    return declaration.replace(projectionDeclarationPattern, `$1${resolved}`);
};

const toIdentifierDeclaration = (
    declaration: string,
    readModels: { identifier: string; displayName: string }[] | undefined,
    draft: DraftReadModel | null
): string => replaceReadModelToken(declaration, token => resolveReadModelIdentifier(token, readModels, draft));

const toDisplayNameDeclaration = (
    declaration: string,
    readModels: { identifier: string; displayName: string }[] | undefined,
    draft: DraftReadModel | null
): string => replaceReadModelToken(declaration, token => resolveReadModelDisplayName(token, readModels, draft));

export const Projections = () => {

    const [declarationValue, setDeclarationValue] = useState('');
    const [originalDeclarationValue, setOriginalDeclarationValue] = useState('');
    const [selectedProjection, setSelectedProjection] = useState<unknown>(null);
    const params = useParams<EventStoreAndNamespaceParams>();
    const [isCreateReadModelDialogOpen, setIsCreateReadModelDialogOpen] = useState(false);
    const [newReadModelDisplayName, setNewReadModelDisplayName] = useState('');
    const [initialReadModelSchema, setInitialReadModelSchema] = useState<JsonSchema | undefined>(undefined);
    const [selectedInstance, setSelectedInstance] = useState<Json | null>(null);
    const [page, setPage] = useState(0);
    const [pageSize, setPageSize] = useState(50);
    const [hasValidationErrors, setHasValidationErrors] = useState(false);
    const [draftReadModel, setDraftReadModel] = useState<DraftReadModel | null>(null);
    const [pendingReadModel, setPendingReadModel] = useState<{ displayName: string; identifier: string; containerName: string; schema: JsonSchema } | null>(null);

    const [readModels, refreshReadModels] = AllReadModelDefinitions.use({ eventStore: params.eventStore! });
    const [eventTypes] = AllEventTypesWithSchemas.use({ eventStore: params.eventStore! });
    const eventSchemas = eventTypes.data?.map(eventType => JSON.parse(eventType.schema) as JsonSchema);
    const [readModelInstances, setReadModelInstances] = useState<ReadModelInstance[]>([]);
    const [syntaxErrors, setSyntaxErrors] = useState<ProjectionDeclarationSyntaxError[]>([]);

    const [projections, refreshProjections] = AllProjectionsWithDeclarations.use({ eventStore: params.eventStore! });
    const [previewProjection, setPreviewProjectionValues, clearPreviewProjectionValues] = PreviewProjection.use();
    const [saveProjection, setSaveProjectionValues, clearSaveProjectionValues] = SaveProjection.use();
    const [TimeMachineDialogWrapper, showTimeMachineDialog] = useDialog(TimeMachineDialog);
    const [showConfirmation] = useConfirmationDialog();

    const selectedReadModel = useMemo(() => {
        if (!selectedProjection || !readModels.data) return null;
        const projection = selectedProjection as { containerName: string };
        const found = readModels.data.find(rm => rm.containerName === projection.containerName) || null;
        return found;
    }, [selectedProjection, readModels.data, draftReadModel, declarationValue]);

    const readModelTokenFromDeclaration = useMemo(() => {
        const match = declarationValue.match(/projection\s+[\w.]+\s*=>\s*([\w.]+)/i);
        return match ? match[1] : null;
    }, [declarationValue]);

    const readModelIdentifierFromDeclaration = useMemo(
        () => resolveReadModelIdentifier(readModelTokenFromDeclaration, readModels.data, draftReadModel),
        [readModelTokenFromDeclaration, readModels.data, draftReadModel]
    );

    const readModelFromDeclaration = useMemo(() => {
        if (!readModelIdentifierFromDeclaration || !readModels.data) return null;
        return readModels.data.find(rm => rm.identifier === readModelIdentifierFromDeclaration) || null;
    }, [readModelIdentifierFromDeclaration, readModels.data]);

    const hasUnsavedChanges = useMemo(() => {
        return declarationValue !== originalDeclarationValue;
    }, [declarationValue, originalDeclarationValue]);

    const saveDisabledReason = useMemo(() => {
        if (!declarationValue.trim()) {
            return strings.eventStore.general.projections.saveDisabledReasons.emptyContent;
        }
        if (!hasUnsavedChanges) {
            return strings.eventStore.general.projections.saveDisabledReasons.noChanges;
        }
        if (hasValidationErrors) {
            return strings.eventStore.general.projections.saveDisabledReasons.validationErrors;
        }
        if (readModelFromDeclaration && readModelFromDeclaration.source === ReadModelSource.code) {
            return strings.eventStore.general.projections.saveDisabledReasons.readModelOwnedByCode;
        }
        if (!draftReadModel && selectedReadModel && selectedReadModel.source === ReadModelSource.code) {
            return strings.eventStore.general.projections.saveDisabledReasons.readModelOwnedByCode;
        }
        if (readModelFromDeclaration && !selectedProjection) {
            const existingProjection = projections.data?.find(p =>
                p.containerName === readModelFromDeclaration.containerName
            );
            if (existingProjection) {
                return strings.eventStore.general.projections.saveDisabledReasons.projectionAlreadyExists;
            }
        }
        return null;
    }, [declarationValue, hasUnsavedChanges, hasValidationErrors, readModelFromDeclaration, selectedProjection, projections.data, draftReadModel, selectedReadModel]);

    const previewDisabledReason = useMemo(() => {
        if (!declarationValue.trim()) {
            return strings.eventStore.general.projections.previewDisabledReasons.emptyContent;
        }
        if (hasValidationErrors) {
            return strings.eventStore.general.projections.previewDisabledReasons.validationErrors;
        }
        return null;
    }, [declarationValue, hasValidationErrors]);

    useEffect(() => {
        setCreateReadModelCallback((readModelName: string) => {
            setNewReadModelDisplayName(readModelName);
            setInitialReadModelSchema(undefined);
            setIsCreateReadModelDialogOpen(true);
        });
        setEditReadModelCallback((readModelName: string, currentSchema: JsonSchema) => {
            setNewReadModelDisplayName(readModelName);
            setInitialReadModelSchema(currentSchema);
            setIsCreateReadModelDialogOpen(true);
        });
    }, []);

    // Sync draft read model with the code action provider
    useEffect(() => {
        if (draftReadModel) {
            setDraftReadModelInProvider({
                identifier: draftReadModel.identifier,
                displayName: draftReadModel.displayName,
                containerName: draftReadModel.containerName,
                schema: JSON.parse(draftReadModel.schema) as JsonSchema
            });
        } else {
            setDraftReadModelInProvider(null);
        }
    }, [draftReadModel]);

    // Update selected projection when projections data refreshes after save
    useEffect(() => {
        if (selectedProjection && projections.data) {
            const projection = selectedProjection as { declaration?: string };
            const updatedProjection = projections.data.find((p: { declaration?: string }) =>
                p.declaration === projection.declaration
            );
            if (updatedProjection && updatedProjection !== selectedProjection) {
                setSelectedProjection(updatedProjection);
            }
        }
    }, [projections.data]);

    useEffect(() => {
        if (!selectedProjection || !readModels.data) {
            return;
        }
        const projection = selectedProjection as { declaration?: string };
        if (!projection.declaration || declarationValue !== projection.declaration) {
            return;
        }
        const displayDeclaration = toDisplayNameDeclaration(projection.declaration, readModels.data, draftReadModel);
        if (displayDeclaration !== declarationValue) {
            setDeclarationValue(displayDeclaration);
            setOriginalDeclarationValue(displayDeclaration);
        }
    }, [selectedProjection, readModels.data, draftReadModel, declarationValue]);

    const handleReadModelChanged = (displayName: string, identifier: string, containerName: string, schema: JsonSchema) => {
        const trimmedDisplayName = displayName.trim();
        const trimmedIdentifier = identifier.trim();
        const trimmedContainerName = containerName.trim();
        if (!trimmedDisplayName) {
            setPendingReadModel(null);
            return;
        }
        setPendingReadModel({
            displayName: trimmedDisplayName,
            identifier: trimmedIdentifier,
            containerName: trimmedContainerName,
            schema
        });
    };

    const handleSaveReadModel = () => {
        if (!pendingReadModel) {
            return;
        }
        const draft = new DraftReadModel();
        draft.displayName = pendingReadModel.displayName;
        draft.identifier = pendingReadModel.identifier;
        draft.containerName = pendingReadModel.containerName;
        draft.schema = JSON.stringify(pendingReadModel.schema);
        setDraftReadModel(draft);
        setIsCreateReadModelDialogOpen(false);
        setNewReadModelDisplayName('');
        setInitialReadModelSchema(undefined);
        setPendingReadModel(null);
    };

    const handleCancelReadModel = () => {
        setIsCreateReadModelDialogOpen(false);
        setNewReadModelDisplayName('');
        setInitialReadModelSchema(undefined);
        setPendingReadModel(null);
    };

    const draftForDialog = draftReadModel &&
        (draftReadModel.displayName === newReadModelDisplayName || draftReadModel.identifier === newReadModelDisplayName)
        ? draftReadModel
        : null;

    return (
        <Page title='Projections'>
            <Allotment className="h-full" proportionalLayout={false}>
                <Allotment.Pane preferredSize="350px">
                    <div className="px-4 py-4">
                        <DataTable
                            value={projections.data}
                            selectionMode="single"
                            selection={selectedProjection as never}
                            emptyMessage={strings.eventStore.general.projections.empty}
                            onSelectionChange={async (e) => {
                                if (hasUnsavedChanges) {
                                    const result = await showConfirmation(
                                        strings.eventStore.general.projections.dialogs.unsavedChanges.title,
                                        strings.eventStore.general.projections.dialogs.unsavedChanges.message,
                                        DialogButtons.YesNo
                                    );
                                    if (result !== DialogResult.Yes) {
                                        return;
                                    }
                                }
                                setSelectedProjection(e.value);
                                const newDeclaration = toDisplayNameDeclaration(e.value?.declaration ?? '', readModels.data, draftReadModel);
                                setDeclarationValue(newDeclaration);
                                setOriginalDeclarationValue(newDeclaration);
                                setReadModelInstances([]);
                                setSelectedInstance(null);
                                setPage(0);
                            }}
                            pt={{
                                root: { className: 'rounded-lg overflow-hidden' }
                            }}>

                            <Column field="identifier" header="Name" />
                            <Column field="containerName" header="Container Name" />
                        </DataTable>
                    </div>
                </Allotment.Pane>
                <Allotment.Pane className="h-full">
                    <div className="px-4 py-4" style={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
                        <Tooltip target="[data-pr-tooltip]" />
                        <Menubar
                            model={[
                                {
                                    label: strings.eventStore.general.projections.actions.new,
                                    icon: <faIcons.FaPlus className='mr-2' />,
                                    command: async () => {
                                        if (hasUnsavedChanges) {
                                            const result = await showConfirmation(
                                                strings.eventStore.general.projections.dialogs.unsavedChanges.title,
                                                strings.eventStore.general.projections.dialogs.unsavedChanges.message,
                                                DialogButtons.YesNo
                                            );
                                            if (result !== DialogResult.Yes) {
                                                return;
                                            }
                                        }
                                        setSelectedProjection(null);
                                        setDeclarationValue('');
                                        setOriginalDeclarationValue('');
                                        setReadModelInstances([]);
                                        setSyntaxErrors([]);
                                        setSelectedInstance(null);
                                        setPage(0);
                                    }
                                },
                                {
                                    label: strings.eventStore.general.projections.actions.save,
                                    icon: <faIcons.FaFloppyDisk className='mr-2' />,
                                    disabled: !!saveDisabledReason,
                                    command: saveDisabledReason ? undefined : async () => {
                                        clearSaveProjectionValues();
                                        setSaveProjectionValues({
                                            eventStore: params.eventStore!,
                                            namespace: params.namespace!,
                                            declaration: toIdentifierDeclaration(declarationValue, readModels.data, draftReadModel),
                                            draftReadModel: draftReadModel ?? undefined
                                        });
                                        const result = await saveProjection.execute();
                                        const errors = (result.response ?? []) as ProjectionDeclarationSyntaxError[];
                                        if (result.isSuccess && errors.length === 0) {
                                            await refreshProjections({ eventStore: params.eventStore! });
                                            if (draftReadModel) {
                                                refreshReadModels({ eventStore: params.eventStore! }); // Only refresh read models if we created a new one
                                            }
                                            setSyntaxErrors([]);
                                            setDraftReadModel(null); // Clear draft after successful save
                                            clearPreviewProjectionValues();
                                            clearSaveProjectionValues();
                                            setOriginalDeclarationValue(declarationValue); // Reset original after successful save
                                        } else {
                                            // Display server-side validation errors in the editor
                                            setSyntaxErrors(errors);
                                        }
                                    },
                                    template: saveDisabledReason ? (item: MenuItem) => (
                                        <div
                                            className="p-menuitem-link p-disabled"
                                            data-pr-tooltip={saveDisabledReason}
                                            data-pr-position="bottom"
                                            style={{ cursor: 'not-allowed', opacity: 0.6 }}
                                        >
                                            {item.icon}
                                            <span className="p-menuitem-text">{item.label}</span>
                                        </div>
                                    ) : undefined
                                },
                                {
                                    label: strings.eventStore.general.projections.actions.preview,
                                    icon: <faIcons.FaEye className='mr-2' />,
                                    disabled: !!previewDisabledReason,
                                    command: previewDisabledReason ? undefined : async () => {
                                        clearPreviewProjectionValues();
                                        setPreviewProjectionValues({
                                            eventStore: params.eventStore!,
                                            namespace: params.namespace!,
                                            declaration: toIdentifierDeclaration(declarationValue, readModels.data, draftReadModel),
                                            draftReadModel: draftReadModel ?? undefined
                                        });
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
                                    },
                                    template: previewDisabledReason ? (item: MenuItem) => (
                                        <div
                                            className="p-menuitem-link p-disabled"
                                            data-pr-tooltip={previewDisabledReason}
                                            data-pr-position="bottom"
                                            style={{ cursor: 'not-allowed', opacity: 0.6 }}
                                        >
                                            {item.icon}
                                            <span className="p-menuitem-text">{item.label}</span>
                                        </div>
                                    ) : undefined
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
                                value={declarationValue}
                                originalValue={originalDeclarationValue}
                                onChange={setDeclarationValue}
                                onValidationChange={setHasValidationErrors}
                                readModels={readModels.data}
                                eventSchemas={eventSchemas}
                                errors={syntaxErrors}
                                theme="vs-dark"
                                eventStore={params.eventStore}
                                namespace={params.namespace}
                                draftReadModel={draftReadModel}
                                normalizeDeclarationForRequests={(declaration) =>
                                    toIdentifierDeclaration(declaration, readModels.data, draftReadModel)}
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
                header={(initialReadModelSchema ? 'Edit' : 'Create') + ' Read Model: ' + newReadModelDisplayName}
                visible={isCreateReadModelDialogOpen}
                style={{ width: '800px', height: '80vh' }}
                modal
                resizable={true}
                footer={(
                    <>
                        <Button
                            label={strings.general.buttons.ok}
                            icon="pi pi-check"
                            onClick={handleSaveReadModel}
                            disabled={!pendingReadModel?.displayName || !pendingReadModel?.identifier || !pendingReadModel?.containerName}
                            autoFocus
                        />
                        <Button
                            label={strings.general.buttons.cancel}
                            icon="pi pi-times"
                            onClick={handleCancelReadModel}
                            outlined
                        />
                    </>
                )}
                onHide={handleCancelReadModel}>
                <ReadModelTypeEditor
                    initialDisplayName={newReadModelDisplayName}
                    initialIdentifier={draftForDialog?.identifier || newReadModelDisplayName}
                    initialContainerName={draftForDialog?.containerName || ''}
                    initialSchema={initialReadModelSchema}
                    onChanged={handleReadModelChanged}
                />
            </Dialog>

            <>
                {selectedReadModel && selectedInstance && (
                    <TimeMachineDialogWrapper
                        readModel={selectedReadModel}
                        readModelKey={typeof selectedInstance === 'object' && 'id' in selectedInstance ? selectedInstance.id as string : ''}
                    />
                )}
            </>
        </Page>
    );
};
