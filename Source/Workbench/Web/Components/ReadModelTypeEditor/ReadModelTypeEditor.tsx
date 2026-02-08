// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React, { useEffect, useState } from 'react';
import { InputText } from 'primereact/inputtext';
import { SchemaEditor } from '../SchemaEditor/SchemaEditor';
import { JsonSchema } from 'Components/JsonSchema';

interface ReadModelTypeEditorProps {
    onChanged: (name: string, schema: JsonSchema) => void;
    initialName?: string;
    initialSchema?: JsonSchema;
}

const createDefaultSchema = (name: string) => ({
    title: name,
    type: 'object',
    properties: {},
    required: []
});

export const ReadModelTypeEditor: React.FC<ReadModelTypeEditorProps> = ({
    onChanged,
    initialName = '',
    initialSchema,
}) => {
    const [readModelName, setReadModelName] = useState(initialName);
    const [schema, setSchema] = useState<JsonSchema>(initialSchema ?? createDefaultSchema(initialName));

    useEffect(() => {
        setReadModelName(initialName);
        setSchema(initialSchema ?? createDefaultSchema(initialName));
    }, [initialName, initialSchema]);

    useEffect(() => {
        onChanged(readModelName, schema);
    }, [onChanged, readModelName, schema]);

    const handleSchemaChange = (newSchema: JsonSchema) => {
        setSchema({
            ...newSchema,
            title: readModelName
        });
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

        </div>
    );
};
