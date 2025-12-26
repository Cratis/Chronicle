// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState, useEffect, useMemo } from 'react';
import { Button } from 'primereact/button';
import { InputText } from 'primereact/inputtext';
import { Dropdown } from 'primereact/dropdown';
import { DataTable } from 'primereact/datatable';
import { Column } from 'primereact/column';
import { Menubar } from 'primereact/menubar';
import { AllTypeFormats } from 'Api/TypeFormats';
import { useQuery } from '@cratis/arc.react/queries';
import * as faIcons from 'react-icons/fa6';

export interface JSONSchemaType {
    type?: string;
    format?: string;
    properties?: Record<string, JSONSchemaType>;
    items?: JSONSchemaType;
    required?: string[];
}

export interface JSONSchemaEditorProps {
    schema: JSONSchemaType;
    eventTypeName: string;
    isEditMode: boolean;
    onChange: (schema: JSONSchemaType) => void;
    onSave: () => void;
    onCancel: () => void;
    onEdit: () => void;
}

interface SchemaProperty {
    name: string;
    type: string;
    format?: string;
    items?: JSONSchemaType;
    properties?: Record<string, JSONSchemaType>;
    required?: boolean;
}

interface NavigationItem {
    name: string;
    path: string[];
}

const JSON_TYPES = [
    { label: 'String', value: 'string' },
    { label: 'Number', value: 'number' },
    { label: 'Integer', value: 'integer' },
    { label: 'Boolean', value: 'boolean' },
    { label: 'Array', value: 'array' },
    { label: 'Object', value: 'object' }
];

export const JSONSchemaEditor = ({ schema, eventTypeName, isEditMode, onChange, onSave, onCancel, onEdit }: JSONSchemaEditorProps) => {
    const [currentPath, setCurrentPath] = useState<string[]>([]);
    const [properties, setProperties] = useState<SchemaProperty[]>([]);
    const [typeFormats, setTypeFormats] = useState<{ label: string, value: string }[]>([]);
    const [currentSchema, setCurrentSchema] = useState<JSONSchemaType>(schema);

    const typeFormatsQuery = useQuery(AllTypeFormats);

    useEffect(() => {
        if (typeFormatsQuery.data) {
            const formats = typeFormatsQuery.data.map(tf => ({
                label: tf.typeName,
                value: tf.format
            }));
            setTypeFormats(formats);
        }
    }, [typeFormatsQuery.data]);

    useEffect(() => {
        setCurrentSchema(schema);
        setCurrentPath([]);
    }, [schema]);

    useEffect(() => {
        loadPropertiesForCurrentPath();
    }, [currentPath, currentSchema]);

    const loadPropertiesForCurrentPath = () => {
        let targetSchema = currentSchema;

        for (const segment of currentPath) {
            if (targetSchema.type === 'array' && segment === '$items') {
                targetSchema = targetSchema.items || {};
            } else if (targetSchema.properties && targetSchema.properties[segment]) {
                targetSchema = targetSchema.properties[segment];
            } else {
                return;
            }
        }

        const schemaProps: SchemaProperty[] = [];
        if (targetSchema.properties) {
            for (const [name, property] of Object.entries(targetSchema.properties)) {
                schemaProps.push({
                    name,
                    type: property.type || 'string',
                    format: property.format,
                    items: property.items,
                    properties: property.properties,
                    required: currentSchema.required?.includes(name) || false
                });
            }
        }

        setProperties(schemaProps);
    };

    const updateSchemaAtPath = (path: string[], updater: (schema: JSONSchemaType) => JSONSchemaType) => {
        const newSchema = JSON.parse(JSON.stringify(currentSchema));

        if (path.length === 0) {
            const updated = updater(newSchema);
            setCurrentSchema(updated);
            onChange(updated);
            return;
        }

        let targetSchema = newSchema;
        for (let i = 0; i < path.length - 1; i++) {
            const segment = path[i];
            if (targetSchema.type === 'array' && segment === '$items') {
                if (!targetSchema.items) {
                    targetSchema.items = { type: 'object', properties: {} };
                }
                targetSchema = targetSchema.items;
            } else if (targetSchema.properties && targetSchema.properties[segment]) {
                targetSchema = targetSchema.properties[segment];
            }
        }

        const lastSegment = path[path.length - 1];
        if (targetSchema.type === 'array' && lastSegment === '$items') {
            targetSchema.items = updater(targetSchema.items || {});
        } else {
            if (!targetSchema.properties) {
                targetSchema.properties = {};
            }
            targetSchema.properties[lastSegment] = updater(targetSchema.properties[lastSegment] || {});
        }

        setCurrentSchema(newSchema);
        onChange(newSchema);
    };

    const addProperty = () => {
        updateSchemaAtPath(currentPath, (schema) => {
            const newProps = { ...(schema.properties || {}) };
            let newName = 'newProperty';
            let counter = 1;
            while (newProps[newName]) {
                newName = `newProperty${counter++}`;
            }
            newProps[newName] = { type: 'string' };
            return { ...schema, properties: newProps };
        });
    };

    const removeProperty = (propertyName: string) => {
        updateSchemaAtPath(currentPath, (schema) => {
            const newProps = { ...(schema.properties || {}) };
            delete newProps[propertyName];
            return { ...schema, properties: newProps };
        });
    };

    const updateProperty = (oldName: string, field: keyof SchemaProperty, value: unknown) => {
        updateSchemaAtPath(currentPath, (schema) => {
            const newProps = { ...(schema.properties || {}) };
            const prop = { ...(newProps[oldName] || {}) };

            if (field === 'name') {
                if (value !== oldName && !newProps[value]) {
                    newProps[value] = prop;
                    delete newProps[oldName];
                }
            } else if (field === 'type') {
                prop.type = value;
                if (value === 'array') {
                    prop.items = { type: 'string' };
                    delete prop.format;
                } else if (value === 'object') {
                    prop.properties = {};
                    delete prop.format;
                    delete prop.items;
                } else {
                    delete prop.items;
                    delete prop.properties;
                }
                newProps[oldName] = prop;
            } else if (field === 'format') {
                if (value && value !== 'none') {
                    prop.format = value;
                } else {
                    delete prop.format;
                }
                newProps[oldName] = prop;
            }

            return { ...schema, properties: newProps };
        });
    };

    const updateArrayItemType = (propertyName: string, itemType: string) => {
        updateSchemaAtPath(currentPath, (schema) => {
            const newProps = { ...(schema.properties || {}) };
            const prop = { ...(newProps[propertyName] || {}) };

            if (itemType === 'object') {
                prop.items = { type: 'object', properties: {} };
            } else if (itemType === 'array') {
                prop.items = { type: 'array', items: { type: 'string' } };
            } else {
                prop.items = { type: itemType };
            }

            newProps[propertyName] = prop;
            return { ...schema, properties: newProps };
        });
    };

    const navigateToProperty = (propertyName: string) => {
        setCurrentPath([...currentPath, propertyName]);
    };

    const navigateToArrayItems = (propertyName: string) => {
        setCurrentPath([...currentPath, propertyName, '$items']);
    };

    const navigateToBreadcrumb = (index: number) => {
        setCurrentPath(currentPath.slice(0, index));
    };

    const getBreadcrumbItems = () => {
        const items: NavigationItem[] = [{ name: eventTypeName, path: [] }];

        for (let i = 0; i < currentPath.length; i++) {
            const segment = currentPath[i];
            if (segment === '$items') {
                items.push({
                    name: '[items]',
                    path: currentPath.slice(0, i + 1)
                });
            } else {
                items.push({
                    name: segment,
                    path: currentPath.slice(0, i + 1)
                });
            }
        }

        return items;
    };

    const nameEditor = (rowData: SchemaProperty) => {
        if (!isEditMode) return rowData.name;
        return (
            <InputText
                value={rowData.name}
                onChange={(e) => updateProperty(rowData.name, 'name', e.target.value)}
                className="w-full"
            />
        );
    };

    const typeAndDetailsEditor = (rowData: SchemaProperty) => {
        // Build all available types including formats
        const allTypeOptions = [
            ...JSON_TYPES,
            ...typeFormats.map(tf => ({ label: tf.label, value: `format:${tf.value}` }))
        ];

        // Determine current value
        let currentValue = rowData.type;
        if (rowData.format && rowData.type === 'string') {
            currentValue = `format:${rowData.format}`;
        }

        if (!isEditMode) {
            // View mode
            if (rowData.type === 'array') {
                const itemType = rowData.items?.type || 'string';
                return (
                    <div className="flex align-items-center gap-2">
                        <span>Array of {itemType}</span>
                        {itemType === 'object' && (
                            <Button
                                icon={<faIcons.FaArrowRight />}
                                className="p-button-text p-button-sm"
                                onClick={() => navigateToArrayItems(rowData.name)}
                                tooltip="Navigate to item definition"
                                tooltipOptions={{ position: 'top' }}
                            />
                        )}
                    </div>
                );
            } else if (rowData.type === 'object') {
                return (
                    <div className="flex align-items-center gap-2">
                        <span>Object</span>
                        <Button
                            icon={<faIcons.FaArrowRight />}
                            className="p-button-text p-button-sm"
                            onClick={() => navigateToProperty(rowData.name)}
                            tooltip="Navigate to object properties"
                            tooltipOptions={{ position: 'top' }}
                        />
                    </div>
                );
            } else if (rowData.format) {
                const formatLabel = typeFormats.find(tf => tf.value === rowData.format)?.label || rowData.format;
                return formatLabel;
            }
            return rowData.type;
        }

        // Edit mode
        return (
            <div className="flex align-items-center gap-2 w-full">
                <Dropdown
                    value={currentValue}
                    options={allTypeOptions}
                    onChange={(e) => {
                        const value = e.value;
                        if (value.startsWith('format:')) {
                            const format = value.substring(7);
                            updateProperty(rowData.name, 'type', 'string');
                            updateProperty(rowData.name, 'format', format);
                        } else {
                            updateProperty(rowData.name, 'type', value);
                            if (value === 'array') {
                                // Initialize items type
                                updateArrayItemType(rowData.name, 'string');
                            }
                        }
                    }}
                    className="flex-1"
                />
                {rowData.type === 'array' && (
                    <>
                        <span className="mx-2">of</span>
                        <Dropdown
                            value={rowData.items?.type || 'string'}
                            options={JSON_TYPES}
                            onChange={(e) => updateArrayItemType(rowData.name, e.value)}
                            className="flex-1"
                        />
                        {rowData.items?.type === 'object' && (
                            <Button
                                icon={<faIcons.FaArrowRight />}
                                className="p-button-text p-button-sm"
                                onClick={() => navigateToArrayItems(rowData.name)}
                                tooltip="Navigate to item definition"
                                tooltipOptions={{ position: 'top' }}
                            />
                        )}
                    </>
                )}
                {rowData.type === 'object' && (
                    <Button
                        icon={<faIcons.FaArrowRight />}
                        className="p-button-text p-button-sm"
                        onClick={() => navigateToProperty(rowData.name)}
                        tooltip="Navigate to object properties"
                        tooltipOptions={{ position: 'top' }}
                    />
                )}
                <Button
                    icon={<faIcons.FaTrash />}
                    className="p-button-text p-button-danger p-button-sm"
                    onClick={() => removeProperty(rowData.name)}
                    tooltip="Delete property"
                    tooltipOptions={{ position: 'top' }}
                />
            </div>
        );
    };

    const menuItems = useMemo(() => [
        ...(!isEditMode ? [{
            label: 'Edit',
            icon: <faIcons.FaPencil className='mr-2' />,
            command: onEdit
        }] : []),
        ...(isEditMode ? [{
            label: 'Add Property',
            icon: <faIcons.FaPlus className='mr-2' />,
            command: addProperty
        }, {
            label: 'Save',
            icon: <faIcons.FaCheck className='mr-2' />,
            command: onSave
        }, {
            label: 'Cancel',
            icon: <faIcons.FaXmark className='mr-2' />,
            command: onCancel
        }] : [])
    ], [isEditMode, onEdit, onSave, onCancel, addProperty]);

    const breadcrumbItems = getBreadcrumbItems();

    return (
        <div className="json-schema-editor" style={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
            <div className="px-4 py-2">
                <Menubar aria-label="Actions" model={menuItems} />
            </div>

            {currentPath.length > 0 && (
                <div className="px-4 py-2 border-bottom-1 surface-border">
                    <div style={{ fontSize: '0.9rem', color: 'var(--text-color-secondary)', cursor: 'pointer' }}>
                        {breadcrumbItems.map((item, index) => (
                            <span key={index}>
                                {index > 0 && <span className="mx-2">&gt;</span>}
                                <span
                                    onClick={() => navigateToBreadcrumb(index)}
                                    style={{ cursor: 'pointer', textDecoration: index < breadcrumbItems.length - 1 ? 'underline' : 'none' }}
                                >
                                    {item.name}
                                </span>
                            </span>
                        ))}
                    </div>
                </div>
            )}

            <div style={{ flex: 1, overflow: 'auto', padding: '1rem' }}>
                <DataTable
                    value={properties}
                    emptyMessage="No properties defined"
                    pt={{
                        root: { style: { border: 'none' } },
                        tbody: { style: { borderTop: '1px solid var(--surface-border)' } }
                    }}
                >
                    <Column
                        field="name"
                        header="Name"
                        body={nameEditor}
                        style={{ width: '30%' }}
                    />
                    <Column
                        header="Type"
                        body={typeAndDetailsEditor}
                        style={{ width: '70%' }}
                    />
                </DataTable>
            </div>
        </div>
    );
};
