// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState, useEffect, useMemo, useCallback } from 'react';
import { Button } from 'primereact/button';
import { DataTable } from 'primereact/datatable';
import { Column } from 'primereact/column';
import { Menubar } from 'primereact/menubar';
import { Tooltip } from 'primereact/tooltip';
import strings from 'Strings';
import { AllTypeFormats } from 'Api/TypeFormats';
import { useQuery } from '@cratis/arc.react/queries';
import * as faIcons from 'react-icons/fa6';
import { NameCell } from './NameCell';
import { TypeCell } from './TypeCell';
import { JSONSchemaType, SchemaProperty, NavigationItem } from './types';
import css from './SchemaEditor.module.css';

export interface SchemaEditorProps {
    schema: JSONSchemaType;
    eventTypeName: string;
    isEditMode: boolean;
    onChange: (schema: JSONSchemaType) => void;
    onSave: () => void;
    onCancel: () => void;
    onEdit: () => void;
}

export const SchemaEditor = ({ schema, eventTypeName, isEditMode, onChange, onSave, onCancel, onEdit }: SchemaEditorProps) => {
    const [currentPath, setCurrentPath] = useState<string[]>([]);
    const [properties, setProperties] = useState<SchemaProperty[]>([]);
    const [typeFormats, setTypeFormats] = useState<{ label: string, value: string }[]>([]);
    const [currentSchema, setCurrentSchema] = useState<JSONSchemaType>(schema);

    useEffect(() => {
        if (!isEditMode) {
            setCurrentPath([]);
        }
    }, [isEditMode]);

    const typeFormatsQuery = AllTypeFormats.use();

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
    }, [schema]);

    useEffect(() => {
        loadPropertiesForCurrentPath();
    }, [currentPath, currentSchema, isEditMode]);

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

    const updateSchemaAtPath = useCallback((path: string[], updater: (schema: JSONSchemaType) => JSONSchemaType) => {
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
    }, [currentSchema, onChange]);

    const addProperty = useCallback(() => {
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
    }, [currentPath, updateSchemaAtPath]);

    const removeProperty = useCallback((propertyName: string) => {
        updateSchemaAtPath(currentPath, (schema) => {
            const newProps = { ...(schema.properties || {}) };
            delete newProps[propertyName];
            return { ...schema, properties: newProps };
        });
    }, [currentPath, updateSchemaAtPath]);

    const updateProperty = useCallback((oldName: string, field: keyof SchemaProperty, value: unknown) => {
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
    }, [currentPath, updateSchemaAtPath]);

    const updateArrayItemType = useCallback((propertyName: string, itemType: string) => {
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
    }, [currentPath, updateSchemaAtPath]);

    const navigateToProperty = useCallback((propertyName: string) => {
        setCurrentPath([...currentPath, propertyName]);
    }, [currentPath]);

    const navigateToArrayItems = useCallback((propertyName: string) => {
        setCurrentPath([...currentPath, propertyName, '$items']);
    }, [currentPath]);

    const navigateBack = useCallback(() => {
        if (currentPath.length > 0) {
            setCurrentPath(currentPath.slice(0, -1));
        }
    }, [currentPath]);

    const navigateToBreadcrumb = useCallback((index: number) => {
        const items = getBreadcrumbItems();
        setCurrentPath(items[index].path);
    }, [currentPath, eventTypeName]);

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

    const menuItems = useMemo(() => [
        ...(!isEditMode ? [{
            label: strings.components.schemaEditor.actions.edit,
            icon: <faIcons.FaPencil className='mr-2' />,
            command: onEdit
        }] : []),
        ...(isEditMode ? [{
            label: strings.components.schemaEditor.actions.save,
            icon: <faIcons.FaCheck className='mr-2' />,
            command: onSave
        }, {
            label: strings.components.schemaEditor.actions.cancel,
            icon: <faIcons.FaXmark className='mr-2' />,
            command: onCancel
        }, {
            label: strings.components.schemaEditor.actions.addProperty,
            icon: <faIcons.FaPlus className='mr-2' />,
            command: addProperty
        }] : [])
    ], [isEditMode, onEdit, onSave, onCancel, addProperty]);

    const breadcrumbItems = getBreadcrumbItems();
    const isAtRoot = currentPath.length === 0;

    return (
        <div className="schema-editor" style={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
                <div className="px-4 py-2">
                    <Menubar aria-label="Actions" model={menuItems} />
                </div>

                <div className="px-4 py-2 border-bottom-1 surface-border">
                    <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                        <Button
                            icon={<faIcons.FaArrowLeft />}
                            className="p-button-text p-button-sm"
                            onClick={navigateBack}
                            disabled={isAtRoot}
                            tooltip={strings.components.schemaEditor.actions.navigateBack}
                            tooltipOptions={{ position: 'top' }}
                        />
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
                </div>

                <div style={{ flex: 1, overflow: 'auto', padding: '1rem' }}>
                    <Tooltip target="[data-pr-tooltip]" mouseTrack mouseTrackTop={15} />
                    <DataTable
                        key={`${isEditMode}-${properties.length}-${properties.map(p => `${p.name}-${p.type}-${p.items?.type || ''}`).join('-')}`}
                        value={properties}
                        dataKey="name"
                        emptyMessage={strings.components.schemaEditor.emptyMessage}
                        rowClassName={(rowData: SchemaProperty) => {
                            if (!isEditMode && (rowData.type === 'object' || (rowData.type === 'array' && rowData.items?.type === 'object'))) {
                                return css.navigableRow;
                            }
                            return '';
                        }}
                        onRowClick={(e) => {
                            if (!isEditMode) {
                                const rowData = e.data as SchemaProperty;
                                if (rowData.type === 'object') {
                                    navigateToProperty(rowData.name);
                                } else if (rowData.type === 'array' && rowData.items?.type === 'object') {
                                    navigateToArrayItems(rowData.name);
                                }
                            }
                        }}
                        pt={{
                            root: { style: { border: 'none' } },
                            tbody: { style: { borderTop: '1px solid var(--surface-border)' } },
                            bodyCell: { style: { height: '3rem', padding: '0 0.75rem', verticalAlign: 'middle' } }
                        }}
                    >
                        <Column
                            field="name"
                            header={strings.components.schemaEditor.columns.property}
                            body={(rowData: SchemaProperty) => (
                                <NameCell
                                    rowData={rowData}
                                    isEditMode={isEditMode}
                                    onUpdate={updateProperty}
                                />
                            )}
                            style={{ width: '30%' }}
                        />
                        <Column
                            header={strings.components.schemaEditor.columns.type}
                            body={(rowData: SchemaProperty) => (
                                <TypeCell
                                    rowData={rowData}
                                    isEditMode={isEditMode}
                                    typeFormats={typeFormats}
                                    onUpdateProperty={updateProperty}
                                    onUpdateArrayItemType={updateArrayItemType}
                                    onNavigateToProperty={navigateToProperty}
                                    onNavigateToArrayItems={navigateToArrayItems}
                                    onRemoveProperty={removeProperty}
                                />
                            )}
                            style={{ width: '70%' }}
                        />
                    </DataTable>
                </div>
            </div>
    );
};
