// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React, { useState, useEffect } from 'react';
import { InputText } from 'primereact/inputtext';
import { Button } from 'primereact/button';
import { SchemaEditor } from './SchemaEditor/SchemaEditor';
import { JsonSchema } from 'Components/JsonSchema';

interface ReadModelCreationProps {
    onSave: (name: string, schema: JsonSchema) => void;
    onCancel: () => void;
    initialName?: string;
    initialSchema?: JsonSchema;
    editMode?: boolean;
    saveDisabled?: boolean;
    cancelDisabled?: boolean;
}

export const ReadModelCreation: React.FC<ReadModelCreationProps> = ({
    onSave,
    onCancel,
    initialName = '',
    initialSchema,
}) => {
    const [readModelName, setReadModelName] = useState(initialName);
    const [schema, setSchema] = useState<JsonSchema>(initialSchema ?? {
        title: initialName,
        type: 'object',
        properties: {},
        required: []
    });

    // Reset state when initial values change (e.g., when dialog reopens)
    useEffect(() => {
        setReadModelName(initialName);
        setSchema(initialSchema ?? {
            title: initialName,
            type: 'object',
            properties: {},
            required: []
        });
    }, [initialName, initialSchema]);

    const handleSave = () => {
        if (!readModelName.trim()) {
            return;
        }

        const updatedSchema = {
            ...schema,
            title: readModelName
        };

        onSave(readModelName, updatedSchema);
    };

    const handleSchemaChange = (newSchema: JsonSchema) => {
        setSchema(newSchema);
    };

    const handleNameChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const newName = e.target.value;
        setReadModelName(newName);
        setSchema(prev => ({
            ...prev,
            title: newName
        }));
    };

    return (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '16px', padding: '16px', height: '100%' }}>
            <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
                <label htmlFor="readModelName" style={{ fontWeight: 'bold' }}>
                    Read Model Name
                </label>
                <InputText
                    id="readModelName"
                    value={readModelName}
                    onChange={handleNameChange}
                    placeholder="Enter read model name (e.g., Order, Customer)"
                    style={{ width: '100%' }}
                    autoFocus
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

            <div style={{ display: 'flex', gap: '10px', justifyContent: 'flex-end', paddingTop: '8px', borderTop: '1px solid var(--surface-border)' }}>
                <Button
                    label="Cancel"
                    icon="pi pi-times"
                    onClick={onCancel}
                    className="p-button-text"
                />
                <Button
                    label="Save"
                    icon="pi pi-check"
                    onClick={handleSave}
                    disabled={!readModelName.trim()}
                />
            </div>
        </div>
    );
};
