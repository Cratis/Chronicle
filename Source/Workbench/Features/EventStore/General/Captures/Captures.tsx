// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Page } from 'Components/Common/Page';
import { CaptureEditor } from 'Components/CaptureEditor';
import { Menubar } from 'primereact/menubar';
import { Tooltip } from 'primereact/tooltip';
import { useEffect, useMemo, useState } from 'react';
import type { MenuItem } from 'primereact/menuitem';
import { useParams } from 'react-router-dom';
import { type EventStoreAndNamespaceParams } from 'Shared';
import strings from 'Strings';
import * as faIcons from 'react-icons/fa6';
import { DataTable } from 'primereact/datatable';
import { Column } from 'primereact/column';
import { Allotment } from 'allotment';

interface CaptureDefinition {
    id: string;
    name: string;
    declaration: string;
}

const defaultCaptureDeclaration = `capture CaptureDefinition
  source api
    url https://example.com/items
  key id
  map
  append ItemChanged
    when added`;

const getCaptureName = (declaration: string) => {
    const match = declaration.match(/^\s*capture\s+([\w.]+)/m);
    return match ? match[1] : 'CaptureDefinition';
};

const createCaptureId = () => globalThis.crypto.randomUUID();

export const Captures = () => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const [captures, setCaptures] = useState<CaptureDefinition[]>([]);
    const [selectedCapture, setSelectedCapture] = useState<CaptureDefinition | null>(null);
    const [declarationValue, setDeclarationValue] = useState('');
    const [originalDeclarationValue, setOriginalDeclarationValue] = useState('');
    const [hasValidationErrors, setHasValidationErrors] = useState(false);

    const storageKey = useMemo(() => `chronicle.workbench.captures.${params.eventStore ?? 'default'}`, [params.eventStore]);

    useEffect(() => {
        const storedCaptures = globalThis.localStorage.getItem(storageKey);
        if (storedCaptures) {
            try {
                const parsed = JSON.parse(storedCaptures) as CaptureDefinition[];
                setCaptures(Array.isArray(parsed) ? parsed : []);
            } catch {
                setCaptures([]);
            }
        } else {
            setCaptures([]);
        }

        setSelectedCapture(null);
        setDeclarationValue('');
        setOriginalDeclarationValue('');
        setHasValidationErrors(false);
    }, [storageKey]);

    const hasUnsavedChanges = useMemo(() => declarationValue !== originalDeclarationValue, [declarationValue, originalDeclarationValue]);

    const saveDisabledReason = useMemo(() => {
        if (!declarationValue.trim()) {
            return strings.eventStore.general.captures.saveDisabledReasons.emptyContent;
        }
        if (!hasUnsavedChanges) {
            return strings.eventStore.general.captures.saveDisabledReasons.noChanges;
        }
        if (hasValidationErrors) {
            return strings.eventStore.general.captures.saveDisabledReasons.validationErrors;
        }
        return null;
    }, [declarationValue, hasUnsavedChanges, hasValidationErrors]);

    const persistCaptures = (nextCaptures: CaptureDefinition[]) => {
        setCaptures(nextCaptures);
        globalThis.localStorage.setItem(storageKey, JSON.stringify(nextCaptures));
    };

    const handleNew = () => {
        setSelectedCapture(null);
        setDeclarationValue(defaultCaptureDeclaration);
        setOriginalDeclarationValue('');
    };

    const handleSave = () => {
        const name = getCaptureName(declarationValue);
        const existingCapture = selectedCapture
            ? captures.find(capture => capture.id === selectedCapture.id)
            : null;

        const nextCapture: CaptureDefinition = existingCapture
            ? { ...existingCapture, name, declaration: declarationValue }
            : { id: createCaptureId(), name, declaration: declarationValue };

        const nextCaptures = existingCapture
            ? captures.map(capture => capture.id === nextCapture.id ? nextCapture : capture)
            : [...captures, nextCapture];

        persistCaptures(nextCaptures);
        setSelectedCapture(nextCapture);
        setOriginalDeclarationValue(declarationValue);
    };

    return (
        <Page title={strings.eventStore.general.captures.title} key={storageKey}>
            <Allotment className="h-full" proportionalLayout={false}>
                <Allotment.Pane preferredSize="320px">
                    <div className="px-4 py-4 h-full">
                        <DataTable
                            value={captures}
                            selectionMode="single"
                            selection={selectedCapture}
                            emptyMessage={strings.eventStore.general.captures.empty}
                            onSelectionChange={(e) => {
                                const capture = e.value as CaptureDefinition | null;
                                setSelectedCapture(capture);
                                setDeclarationValue(capture?.declaration ?? '');
                                setOriginalDeclarationValue(capture?.declaration ?? '');
                            }}
                            pt={{
                                root: { className: 'rounded-lg overflow-hidden' },
                            }}
                        >
                            <Column field="name" header={strings.eventStore.general.captures.columns.name} />
                        </DataTable>
                    </div>
                </Allotment.Pane>
                <Allotment.Pane className="h-full">
                    <div className="px-4 py-4" style={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
                        <Tooltip target="[data-pr-tooltip]" />
                        <Menubar
                            model={[
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
                                    ) : undefined,
                                },
                            ]}
                        />

                        <div className="pt-4" style={{ flex: 1, minHeight: 0 }}>
                            {!selectedCapture && !declarationValue ? (
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
                            ) : (
                                <CaptureEditor
                                    value={declarationValue}
                                    originalValue={originalDeclarationValue}
                                    onChange={setDeclarationValue}
                                    onValidationChange={setHasValidationErrors}
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
