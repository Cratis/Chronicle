// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React, { useEffect, useState } from 'react';
import { InputText } from 'primereact/inputtext';
import pluralize from 'pluralize';
import { SchemaEditor } from '../SchemaEditor/SchemaEditor';
import { JsonSchema } from 'Components/JsonSchema';
import strings from 'Strings';

interface ReadModelTypeEditorProps {
    onChanged: (displayName: string, identifier: string, containerName: string, schema: JsonSchema) => void;
    initialDisplayName?: string;
    initialIdentifier?: string;
    initialContainerName?: string;
    initialSchema?: JsonSchema;
}

const createDefaultSchema = (name: string) => ({
    title: name,
    type: 'object',
    properties: {},
    required: []
});

const resolveIdentifier = (displayName: string, identifier?: string) => {
    const trimmedIdentifier = identifier?.trim() ?? '';
    return trimmedIdentifier || displayName;
};

const resolveContainerName = (displayName: string, containerName?: string) => {
    const trimmedContainerName = containerName?.trim() ?? '';
    if (trimmedContainerName) {
        return trimmedContainerName;
    }
    return displayName ? pluralize(displayName) : '';
};

export const ReadModelTypeEditor: React.FC<ReadModelTypeEditorProps> = ({
    onChanged,
    initialDisplayName = '',
    initialIdentifier,
    initialContainerName,
    initialSchema,
}) => {
    const [displayName, setDisplayName] = useState(initialDisplayName);
    const [identifier, setIdentifier] = useState(resolveIdentifier(initialDisplayName, initialIdentifier));
    const [containerName, setContainerName] = useState(resolveContainerName(initialDisplayName, initialContainerName));
    const [identifierEdited, setIdentifierEdited] = useState(false);
    const [containerNameEdited, setContainerNameEdited] = useState(false);
    const [schema, setSchema] = useState<JsonSchema>(initialSchema ?? createDefaultSchema(initialDisplayName));

    useEffect(() => {
        setDisplayName(initialDisplayName);
        setIdentifier(resolveIdentifier(initialDisplayName, initialIdentifier));
        setContainerName(resolveContainerName(initialDisplayName, initialContainerName));
        setSchema(initialSchema ?? createDefaultSchema(initialDisplayName));
        setIdentifierEdited(!!initialIdentifier?.trim() && initialIdentifier.trim() !== initialDisplayName.trim());
        setContainerNameEdited(!!initialContainerName?.trim() && initialContainerName.trim() !== resolveContainerName(initialDisplayName));
    }, [initialDisplayName, initialIdentifier, initialContainerName, initialSchema]);

    useEffect(() => {
        onChanged(displayName, identifier, containerName, schema);
    }, [onChanged, displayName, identifier, containerName, schema]);

    const handleSchemaChange = (newSchema: JsonSchema) => {
        setSchema({
            ...newSchema,
            title: displayName
        });
    };

    const handleDisplayNameChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const newDisplayName = e.target.value;
        setDisplayName(newDisplayName);
        if (!identifierEdited) {
            setIdentifier(newDisplayName);
        }
        if (!containerNameEdited) {
            setContainerName(newDisplayName ? pluralize(newDisplayName) : '');
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
        </div>
    );
};
