// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Page } from 'Components/Common/Page';
import { CaptureEditor, type CaptureDeclarationSyntaxError } from 'Components/CaptureEditor';
import { ActionMenubar, Tooltip, type ActionMenuItem } from '@cratis/components/Common';
import { Tag } from '@cratis/components/Display';
import { useEffect, useMemo, useState } from 'react';
import { useParams } from 'react-router-dom';
import { type EventStoreAndNamespaceParams } from 'Shared';
import strings from 'Strings';
import * as faIcons from 'react-icons/fa6';
import { DataTable } from 'Components/DataTable';
import { Column } from '@cratis/components/DataTables';
import { Allotment } from 'allotment';
import { useConfirmationDialog, DialogResult, DialogButtons } from '@cratis/arc.react/dialogs';
import { ObserveCaptures, SaveCapture, StartCapture, StopCapture, DeleteCapture, ValidateCaptureDeclaration, type CaptureDetails } from 'Features/Captures';
import { CaptureStatus, type CaptureValidationMessage } from 'Features/Contracts/Captures';
import { GetExternalServices } from 'Features/ExternalServices';
import { AllEventTypes } from 'Features/EventTypes';
import { CapturedEventsView } from './CapturedEventsView';

const defaultCaptureDeclaration = `capture CaptureDefinition
  source api
    api MyApi
    poll 5m
  key id
  append ItemChanged
    when added`;

const toSyntaxErrors = (messages: CaptureValidationMessage[]): CaptureDeclarationSyntaxError[] =>
    messages.map(message => ({ line: message.line, column: message.column, message: message.message }));

type CaptureView = 'editor' | 'data';

export const Captures = () => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const eventStore = params.eventStore!;

    const [capturesResult] = ObserveCaptures.use({ eventStore });
    const [externalServicesResult] = GetExternalServices.use({ eventStore });
    const [eventTypesResult] = AllEventTypes.use({ eventStore });
    const [saveCommand] = SaveCapture.use();
    const [startCommand] = StartCapture.use();
    const [stopCommand] = StopCapture.use();
    const [deleteCommand] = DeleteCapture.use();
    const [validateCommand] = ValidateCaptureDeclaration.use();
    const [showConfirmation] = useConfirmationDialog();

    const [selectedCaptureId, setSelectedCaptureId] = useState<string | null>(null);
    const [isCreatingNew, setIsCreatingNew] = useState(false);
    const [declarationValue, setDeclarationValue] = useState('');
    const [originalDeclarationValue, setOriginalDeclarationValue] = useState('');
    const [hasSyntaxErrors, setHasSyntaxErrors] = useState(false);
    const [serverMessages, setServerMessages] = useState<CaptureDeclarationSyntaxError[]>([]);
    const [view, setView] = useState<CaptureView>('editor');
    const [dataRefreshTrigger, setDataRefreshTrigger] = useState(0);

    const captures = capturesResult.data ?? [];
    const selectedCapture = captures.find(capture => capture.id === selectedCaptureId) ?? null;
    const isStarted = selectedCapture?.status === CaptureStatus.started;
    const externalServiceNames = useMemo(
        () => (externalServicesResult.data ?? []).map(service => service.name),
        [externalServicesResult.data]);
    const eventTypeNames = useMemo(
        () => (eventTypesResult.data ?? []).map(eventType => eventType.type.id),
        [eventTypesResult.data]);

    useEffect(() => {
        setSelectedCaptureId(null);
        setIsCreatingNew(false);
        setDeclarationValue('');
        setOriginalDeclarationValue('');
        setHasSyntaxErrors(false);
        setServerMessages([]);
        setView('editor');
    }, [eventStore]);

    useEffect(() => {
        if (!declarationValue.trim()) {
            setServerMessages([]);
            return;
        }

        const timeout = setTimeout(async () => {
            validateCommand.eventStore = eventStore;
            validateCommand.declaration = declarationValue;
            const result = await validateCommand.execute();
            if (result.isSuccess) {
                const messages = (result.response ?? []) as unknown as CaptureValidationMessage[];
                setServerMessages(toSyntaxErrors(messages));
            }
        }, 500);

        return () => clearTimeout(timeout);
    }, [declarationValue, eventStore]);

    const hasUnsavedChanges = useMemo(() => declarationValue !== originalDeclarationValue, [declarationValue, originalDeclarationValue]);

    const saveDisabledReason = useMemo(() => {
        if (isStarted) {
            return strings.eventStore.general.captures.saveDisabledReasons.started;
        }
        if (!declarationValue.trim()) {
            return strings.eventStore.general.captures.saveDisabledReasons.emptyContent;
        }
        if (!hasUnsavedChanges) {
            return strings.eventStore.general.captures.saveDisabledReasons.noChanges;
        }
        if (hasSyntaxErrors) {
            return strings.eventStore.general.captures.saveDisabledReasons.validationErrors;
        }
        return null;
    }, [isStarted, declarationValue, hasUnsavedChanges, hasSyntaxErrors]);

    const selectCapture = (capture: CaptureDetails | null) => {
        setSelectedCaptureId(capture?.id ?? null);
        setIsCreatingNew(false);
        setDeclarationValue(capture?.declaration ?? '');
        setOriginalDeclarationValue(capture?.declaration ?? '');
        setServerMessages([]);
        setView(capture?.status === CaptureStatus.started ? 'data' : 'editor');
    };

    const handleNew = () => {
        setSelectedCaptureId(null);
        setIsCreatingNew(true);
        setDeclarationValue(defaultCaptureDeclaration);
        setOriginalDeclarationValue('');
        setServerMessages([]);
        setView('editor');
    };

    const handleSave = async () => {
        saveCommand.eventStore = eventStore;
        saveCommand.id = selectedCaptureId ?? '';
        saveCommand.declaration = declarationValue;
        const result = await saveCommand.execute();
        if (!result.isSuccess) return;

        const response = result.response;
        if (response?.messages?.length) {
            setServerMessages(toSyntaxErrors(response.messages));
        }

        if (response?.capture) {
            setSelectedCaptureId(response.capture.id);
            setIsCreatingNew(false);
            setOriginalDeclarationValue(declarationValue);
        }
    };

    const handleStart = async () => {
        if (!selectedCapture) return;
        startCommand.eventStore = eventStore;
        startCommand.captureId = selectedCapture.id;
        const result = await startCommand.execute();
        if (!result.isSuccess) return;

        const messages = result.response?.messages ?? [];
        if (messages.length > 0) {
            setServerMessages(toSyntaxErrors(messages));
            setView('editor');
        } else {
            setView('data');
        }
    };

    const handleStop = async () => {
        if (!selectedCapture) return;
        stopCommand.eventStore = eventStore;
        stopCommand.captureId = selectedCapture.id;
        await stopCommand.execute();
    };

    const handleDelete = async () => {
        if (!selectedCapture) return;
        const result = await showConfirmation(
            strings.eventStore.general.captures.dialogs.deleteCapture.title,
            strings.eventStore.general.captures.dialogs.deleteCapture.message.replace('{name}', selectedCapture.name),
            DialogButtons.YesNo
        );

        if (result === DialogResult.Yes) {
            deleteCommand.eventStore = eventStore;
            deleteCommand.captureId = selectedCapture.id;
            await deleteCommand.execute();
            selectCapture(null);
        }
    };

    const menuItems: ActionMenuItem[] = [
        {
            label: strings.eventStore.general.captures.actions.new,
            icon: <faIcons.FaPlus className='mr-2' />,
            command: handleNew,
        },
        {
            label: strings.eventStore.general.captures.actions.save,
            icon: <faIcons.FaFloppyDisk className='mr-2' />,
            disabled: !!saveDisabledReason,
            command: saveDisabledReason ? undefined : handleSave,
            template: saveDisabledReason ? (item: ActionMenuItem) => (
                <Tooltip content={saveDisabledReason} position="bottom">
                    <div
                        className="p-menuitem-link p-disabled"
                        style={{ cursor: 'not-allowed', opacity: 0.6 }}
                    >
                        {item.icon}
                        <span className="p-menuitem-text">{item.label}</span>
                    </div>
                </Tooltip>
            ) : undefined,
        },
        isStarted
            ? {
                label: strings.eventStore.general.captures.actions.stop,
                icon: <faIcons.FaStop className='mr-2' />,
                command: handleStop,
            }
            : {
                label: strings.eventStore.general.captures.actions.start,
                icon: <faIcons.FaPlay className='mr-2' />,
                disabled: !selectedCapture || hasUnsavedChanges || hasSyntaxErrors,
                command: handleStart,
            },
        {
            label: strings.eventStore.general.captures.actions.delete,
            icon: <faIcons.FaTrash className='mr-2' />,
            disabled: !selectedCapture,
            command: handleDelete,
        },
        {
            label: view === 'editor'
                ? strings.eventStore.general.captures.actions.showData
                : strings.eventStore.general.captures.actions.showEditor,
            icon: view === 'editor' ? <faIcons.FaTable className='mr-2' /> : <faIcons.FaCode className='mr-2' />,
            disabled: !selectedCapture,
            command: () => setView(view === 'editor' ? 'data' : 'editor'),
        },
    ];

    if (view === 'data' && selectedCapture) {
        menuItems.push({
            label: strings.eventStore.general.captures.actions.refresh,
            icon: <faIcons.FaArrowsRotate className='mr-2' />,
            command: () => setDataRefreshTrigger(previous => previous + 1),
        });
    }

    const statusBody = (capture: CaptureDetails) => (
        <Tag
            value={capture.status === CaptureStatus.started
                ? strings.eventStore.general.captures.status.started
                : strings.eventStore.general.captures.status.stopped}
            severity={capture.status === CaptureStatus.started ? 'success' : 'secondary'}
        />
    );

    const showEmptyState = !selectedCapture && !isCreatingNew;

    return (
        <Page title={strings.eventStore.general.captures.title} key={eventStore}>
            <Allotment className="h-full" proportionalLayout={false}>
                <Allotment.Pane preferredSize="320px">
                    <div className="px-4 py-4 h-full">
                        <DataTable
                            value={captures}
                            dataKey="id"
                            selectionMode="single"
                            selection={selectedCapture}
                            emptyMessage={strings.eventStore.general.captures.empty}
                            onSelectionChange={(event) => selectCapture(event.value)}
                            className="rounded-lg overflow-hidden"
                        >
                            <Column field="name" header={strings.eventStore.general.captures.columns.name} />
                            <Column field="status" header={strings.eventStore.general.captures.columns.status} body={statusBody} />
                        </DataTable>
                    </div>
                </Allotment.Pane>
                <Allotment.Pane className="h-full">
                    <div className="px-4 py-4" style={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
                        <ActionMenubar model={menuItems} />

                        <div className="pt-4" style={{ flex: 1, minHeight: 0 }}>
                            {showEmptyState ? (
                                <div style={{
                                    height: '100%',
                                    display: 'flex',
                                    alignItems: 'center',
                                    justifyContent: 'center',
                                    color: 'var(--text-color-secondary)',
                                    fontSize: '1rem',
                                }}>
                                    {strings.eventStore.general.captures.emptyEditor}
                                </div>
                            ) : view === 'data' && selectedCapture ? (
                                <CapturedEventsView
                                    eventStore={eventStore}
                                    captureName={selectedCapture.name}
                                    refreshTrigger={dataRefreshTrigger}
                                />
                            ) : (
                                <CaptureEditor
                                    value={declarationValue}
                                    originalValue={originalDeclarationValue}
                                    onChange={setDeclarationValue}
                                    onValidationChange={setHasSyntaxErrors}
                                    errors={serverMessages}
                                    readOnly={isStarted}
                                    externalServiceNames={externalServiceNames}
                                    eventTypeNames={eventTypeNames}
                                    theme="vs-dark"
                                />
                            )}
                        </div>
                    </div>
                </Allotment.Pane>
            </Allotment>
        </Page>
    );
};
