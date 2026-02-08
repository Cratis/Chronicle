// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React, { useState, useEffect } from 'react';
import pluralize from 'pluralize';
import { InputText } from 'primereact/inputtext';
import { Button } from 'primereact/button';
import { SchemaEditor } from './SchemaEditor/SchemaEditor';
import { JsonSchema } from 'Components/JsonSchema';
import strings from 'Strings';

interface ReadModelCreationProps {
    onSave: (displayName: string, identifier: string, containerName: string, schema: JsonSchema) => void;
    onCancel: () => void;
    initialDisplayName?: string;
    initialIdentifier?: string;
    initialContainerName?: string;
    initialSchema?: JsonSchema;
    editMode?: boolean;
    saveDisabled?: boolean;
    cancelDisabled?: boolean;
}

export const ReadModelCreation: React.FC<ReadModelCreationProps> = ({
    onSave,
    onCancel,
    initialDisplayName = '',
    initialIdentifier = '',
    initialContainerName = '',
    initialSchema,
    saveDisabled = false,
    cancelDisabled = false,
}) => {
    const [displayName, setDisplayName] = useState(initialDisplayName);
    const [identifier, setIdentifier] = useState(initialIdentifier || initialDisplayName);
    const [containerName, setContainerName] = useState(initialContainerName || pluralize(initialDisplayName));
    const [containerNameEdited, setContainerNameEdited] = useState(false);
    const [identifierEdited, setIdentifierEdited] = useState(false);
    const [schema, setSchema] = useState<JsonSchema>(initialSchema ?? {
        title: initialDisplayName,
        type: 'object',
        properties: {},
        required: []
    });

    // Reset state when initial values change (e.g., when dialog reopens)
    useEffect(() => {
        setDisplayName(initialDisplayName);
        setIdentifier(initialIdentifier || initialDisplayName);
        setContainerName(initialContainerName || pluralize(initialDisplayName));
        setContainerNameEdited(false);
        setIdentifierEdited(false);
        setSchema(initialSchema ?? {
            title: initialDisplayName,
            type: 'object',
            properties: {},
            required: []
        });
    }, [initialDisplayName, initialIdentifier, initialContainerName, initialSchema]);

    const handleSave = () => {
        if (!displayName.trim() || !identifier.trim() || !containerName.trim()) {
            return;
        }

        const updatedSchema = {
            ...schema,
            title: displayName
        };

        onSave(displayName, identifier, containerName, updatedSchema);
    };

    const handleSchemaChange = (newSchema: JsonSchema) => {
        setSchema(newSchema);
    };

    const handleDisplayNameChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const newDisplayName = e.target.value;
        setDisplayName(newDisplayName);
        if (!identifierEdited) {
            setIdentifier(newDisplayName);
        }
        if (!containerNameEdited) {
            setContainerName(pluralize(newDisplayName));
        }
        setSchema(prev => ({
            ...prev,
            title: newDisplayName
        }));
    };

    const handleIdentifierChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        setIdentifier(e.target.value);
        setIdentifierEdited(true);
    };

    const handleContainerNameChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        setContainerName(e.target.value);
        setContainerNameEdited(true);
    };

    const isSaveDisabled =
        !!saveDisabled ||
        !displayName.trim() ||
        !identifier.trim() ||
        !containerName.trim();

    return (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '16px', padding: '16px', height: '100%' }}>
            <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
                <label htmlFor="displayName" style={{ fontWeight: 'bold' }}>
                    {strings.eventStore.general.readModels.dialogs.addReadModel.displayNameLabel}
                </label>
                <InputText
                    id="displayName"
                    value={displayName}
                    onChange={handleDisplayNameChange}
                    placeholder={strings.eventStore.general.readModels.dialogs.addReadModel.displayNamePlaceholder}
                    style={{ width: '100%' }}
                    autoFocus
                />
            </div>

            <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
                <label htmlFor="identifier" style={{ fontWeight: 'bold' }}>
                    {strings.eventStore.general.readModels.dialogs.addReadModel.identifierLabel}
                </label>
                <InputText
                    id="identifier"
                    value={identifier}
                    onChange={handleIdentifierChange}
                    placeholder={strings.eventStore.general.readModels.dialogs.addReadModel.identifierPlaceholder}
                    style={{ width: '100%' }}
                />
            </div>

            <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
                <label htmlFor="containerName" style={{ fontWeight: 'bold' }}>
                    {strings.eventStore.general.readModels.dialogs.addReadModel.containerNameLabel}
                </label>
                <InputText
                    id="containerName"
                    value={containerName}
                    onChange={handleContainerNameChange}
                    placeholder={strings.eventStore.general.readModels.dialogs.addReadModel.containerNamePlaceholder}
                    style={{ width: '100%' }}
                />
            </div>

            <div style={{ flex: 1, minHeight: '250px', overflow: 'auto' }}>
                <SchemaEditor
                    schema={schema}
                    onChange={handleSchemaChange}
                    editMode={true}
                    saveDisabled={true}
                    cancelDisabled={true}
                />
            </div>

            <div style={{ display: 'flex', gap: '10px', justifyContent: 'flex-start', paddingTop: '8px', borderTop: '1px solid var(--surface-border)' }}>
                <Button
                    label={strings.eventStore.general.readModels.actions.save}
                    icon="pi pi-check"
                    onClick={handleSave}
                    disabled={isSaveDisabled}
                />
                <Button
                    label={strings.general.buttons.cancel}
                    icon="pi pi-times"
                    onClick={onCancel}
                    outlined
                    disabled={cancelDisabled}
                />
            </div>
        </div>
    );
};
