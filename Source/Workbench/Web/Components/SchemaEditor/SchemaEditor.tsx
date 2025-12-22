// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState, useEffect } from 'react';
import { Button } from 'primereact/button';
import { InputText } from 'primereact/inputtext';
import { Dropdown } from 'primereact/dropdown';
import { DataTable } from 'primereact/datatable';
import { Column } from 'primereact/column';
import { AllTypeFormats } from 'Api/TypeFormats';
import { useQuery } from '@cratis/arc.react/queries';
import strings from 'Strings';

export interface SchemaEditorProps {
    schema: string;
    onSave: (schema: string) => void;
}

interface SchemaProperty {
    name: string;
    type: string;
    format?: string;
}

export const SchemaEditor = ({ schema, onSave }: SchemaEditorProps) => {
    const [isEditing, setIsEditing] = useState(false);
    const [properties, setProperties] = useState<SchemaProperty[]>([]);
    const [typeFormats, setTypeFormats] = useState<{label: string, value: string}[]>([]);
    
    const typeFormatsQuery = useQuery(AllTypeFormats);

    useEffect(() => {
        // Load type formats
        if (typeFormatsQuery.data) {
            const formats = typeFormatsQuery.data.map(tf => ({
                label: tf.typeName,
                value: tf.format
            }));
            // Add string type
            formats.unshift({ label: 'String', value: 'string' });
            setTypeFormats(formats);
        }
    }, [typeFormatsQuery.data]);

    useEffect(() => {
        // Parse schema into properties
        if (schema) {
            try {
                const parsedSchema = JSON.parse(schema);
                const props: SchemaProperty[] = [];
                
                if (parsedSchema.properties) {
                    for (const [name, prop] of Object.entries(parsedSchema.properties)) {
                        const property = prop as any;
                        props.push({
                            name,
                            type: property.format || property.type || 'string',
                            format: property.format
                        });
                    }
                }
                setProperties(props);
            } catch (e) {
                console.error('Failed to parse schema:', e);
            }
        }
    }, [schema]);

    const addProperty = () => {
        setProperties([...properties, { name: '', type: 'string' }]);
    };

    const removeProperty = (index: number) => {
        setProperties(properties.filter((_, i) => i !== index));
    };

    const updateProperty = (index: number, field: keyof SchemaProperty, value: string) => {
        const updated = [...properties];
        updated[index][field] = value;
        setProperties(updated);
    };

    const handleSave = () => {
        // Convert properties to JSON schema
        const schemaObj: any = {
            type: 'object',
            properties: {}
        };

        properties.forEach(prop => {
            if (prop.name) {
                if (prop.type === 'string') {
                    schemaObj.properties[prop.name] = { type: 'string' };
                } else {
                    schemaObj.properties[prop.name] = {
                        type: 'string',
                        format: prop.type
                    };
                }
            }
        });

        const schemaJson = JSON.stringify(schemaObj, null, 2);
        onSave(schemaJson);
        setIsEditing(false);
    };

    const nameEditor = (rowData: SchemaProperty, options: any) => {
        if (!isEditing) return rowData.name;
        return (
            <InputText
                value={rowData.name}
                onChange={(e) => updateProperty(options.rowIndex, 'name', e.target.value)}
            />
        );
    };

    const typeEditor = (rowData: SchemaProperty, options: any) => {
        if (!isEditing) {
            const format = typeFormats.find(tf => tf.value === rowData.type);
            return format?.label || rowData.type;
        }
        return (
            <Dropdown
                value={rowData.type}
                options={typeFormats}
                onChange={(e) => updateProperty(options.rowIndex, 'type', e.value)}
            />
        );
    };

    const actionsTemplate = (rowData: SchemaProperty, options: any) => {
        if (!isEditing) return null;
        return (
            <Button
                icon="pi pi-trash"
                className="p-button-rounded p-button-danger p-button-text"
                onClick={() => removeProperty(options.rowIndex)}
            />
        );
    };

    return (
        <div className="schema-editor">
            <div className="toolbar mb-3">
                {!isEditing ? (
                    <Button label={strings.eventStore.general.readModels.actions.edit} icon="pi pi-pencil" onClick={() => setIsEditing(true)} />
                ) : (
                    <>
                        <Button label={strings.eventStore.general.readModels.actions.addProperty} icon="pi pi-plus" onClick={addProperty} className="mr-2" />
                        <Button label={strings.eventStore.general.readModels.actions.save} icon="pi pi-check" onClick={handleSave} className="mr-2" />
                        <Button label={strings.general.buttons.cancel} icon="pi pi-times" severity="secondary" onClick={() => setIsEditing(false)} />
                    </>
                )}
            </div>

            <DataTable value={properties} showGridlines>
                <Column 
                    field="name" 
                    header={strings.eventStore.general.readModels.schema.propertyName}
                    body={nameEditor}
                />
                <Column 
                    field="type" 
                    header={strings.eventStore.general.readModels.schema.type}
                    body={typeEditor}
                />
                {isEditing && <Column body={actionsTemplate} style={{ width: '4rem' }} />}
            </DataTable>
        </div>
    );
};
