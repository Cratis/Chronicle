// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState, useEffect, useMemo, useCallback } from 'react';
import { Button } from 'primereact/button';
import { DataTable } from 'primereact/datatable';
import { Column } from 'primereact/column';
import { Menubar } from 'primereact/menubar';
import { Tooltip } from 'primereact/tooltip';
import strings from 'Strings';
import { AllTypeFormats, TypeFormat } from 'Api/TypeFormats';
import * as faIcons from 'react-icons/fa6';
import { NameCell } from './NameCell';
import { TypeCell } from './TypeCell';
import { JsonSchema, JsonSchemaProperty, NavigationItem } from '../JsonSchema';
import css from './SchemaEditor.module.css';
import { MenuItem } from 'primereact/menuitem';

export interface NavigationItem {
    name: string;
    path: string[];
}

export interface SchemaEditorProps {
    schema: JsonSchema;
    eventTypeName?: string;
    canEdit?: boolean;
    canNotEditReason?: string;
    onChange?: (schema: JsonSchema) => void;
    onSave?: () => void;
    onCancel?: () => void;
    editMode?: boolean;
    saveDisabled?: boolean;
    cancelDisabled?: boolean;
}

export const SchemaEditor = ({ schema, eventTypeName = '', canEdit = true, canNotEditReason, onChange, onSave, onCancel, editMode, saveDisabled = false, cancelDisabled = false }: SchemaEditorProps) => {
    const [currentPath, setCurrentPath] = useState<string[]>([]);
    const [properties, setProperties] = useState<JsonSchemaProperty[]>([]);
    const [typeFormats, setTypeFormats] = useState<TypeFormat[]>([]);
    const [currentSchema, setCurrentSchema] = useState<JsonSchema>(schema);
    const [isEditMode, setIsEditMode] = useState(editMode ?? false);
    const [initialSchema, setInitialSchema] = useState<JsonSchema>(schema);
    const [validationErrors, setValidationErrors] = useState<Record<string, string>>({});

    useEffect(() => {
        if (!isEditMode) {
            setCurrentPath([]);
        }
    }, [isEditMode]);

    const validatePropertyName = useCallback((name: string, propertyId: string, allProperties: JsonSchemaProperty[]): string | undefined => {
        // Check for empty string
        if (!name || name.trim() === '') {
            return 'Property name cannot be empty';
        }

        // Check for valid identifier (alphanumeric, underscore, must start with letter or underscore)
        const validIdentifierPattern = /^[a-zA-Z_][a-zA-Z0-9_]*$/;
        if (!validIdentifierPattern.test(name)) {
            return 'Property name must start with a letter or underscore and contain only letters, numbers, and underscores';
        }

        // Check for duplicates (excluding current property)
        const duplicates = allProperties.filter(p => p.name === name && p.id !== propertyId);
        if (duplicates.length > 0) {
            return 'Property name must be unique';
        }

        return undefined;
    }, []);

    const validateAllProperties = useCallback((properties: JsonSchemaProperty[]) => {
        const errors: Record<string, string> = {};

        properties.forEach(prop => {
            const error = validatePropertyName(prop.name, prop.id!, properties);
            if (error) {
                errors[prop.id!] = error;
            }
        });

        setValidationErrors(errors);
        return Object.keys(errors).length === 0;
    }, [validatePropertyName]);

    const [typeFormatsQuery] = AllTypeFormats.use();

    useEffect(() => {
        if (typeFormatsQuery.data) {
            setTypeFormats(typeFormatsQuery.data);
        }
    }, [typeFormatsQuery.data]);

    useEffect(() => {
        setCurrentSchema(schema);
        setInitialSchema(JSON.parse(JSON.stringify(schema)));
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

        const schemaProps: JsonSchemaProperty[] = [];
        if (targetSchema.properties) {
            let idCounter = 0;
            for (const [name, property] of Object.entries(targetSchema.properties)) {
                schemaProps.push({
                    id: `prop-${currentPath.join('-')}-${idCounter++}`,
                    name,
                    type: property.type || 'string',
                    format: property.format,
                    description: property.description,
                    items: property.items,
                    properties: property.properties,
                    required: currentSchema.required?.includes(name) || false
                });
            }
        }

        setProperties(schemaProps);
        if (isEditMode) {
            validateAllProperties(schemaProps);
        }
    };

    const updateSchemaAtPath = useCallback((path: string[], updater: (schema: JsonSchema) => JsonSchema) => {
        const newSchema = JSON.parse(JSON.stringify(currentSchema));

        if (path.length === 0) {
            const updated = updater(newSchema);
            setCurrentSchema(updated);
            onChange?.(updated);
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
        onChange?.(newSchema);
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

    const updateProperty = useCallback((oldName: string, field: keyof JsonSchemaProperty, value: unknown, additionalUpdates?: Partial<JsonSchemaProperty>) => {
        updateSchemaAtPath(currentPath, (schema) => {
            const newProps = { ...(schema.properties || {}) };
            const prop = { ...(newProps[oldName] || {}) };

            if (field === 'name') {
                if (value !== oldName && !newProps[value as string]) {
                    newProps[value as string] = prop;
                    delete newProps[oldName];
                }
            } else if (field === 'type') {
                prop.type = value as string;
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

                // Apply additional updates (e.g., format) in the same transaction
                if (additionalUpdates) {
                    if ('format' in additionalUpdates) {
                        if (additionalUpdates.format) {
                            prop.format = additionalUpdates.format as string;
                        } else {
                            delete prop.format;
                        }
                    }
                }

                newProps[oldName] = prop;
            } else if (field === 'format') {
                if (value && value !== 'none') {
                    prop.format = value as string;
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

    const handleSave = useCallback(() => {
        onSave?.();
        setIsEditMode(false);
    }, [onSave]);

    const handleCancel = useCallback(() => {
        setCurrentSchema(JSON.parse(JSON.stringify(initialSchema)));
        onChange?.(JSON.parse(JSON.stringify(initialSchema)));
        setIsEditMode(false);
        onCancel?.();
    }, [initialSchema, onChange]);

    const handleEdit = useCallback(() => {
        setInitialSchema(JSON.parse(JSON.stringify(currentSchema)));
        setIsEditMode(true);
    }, [currentSchema]);

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

    const getCurrentDescription = useCallback(() => {
        let targetSchema = currentSchema;

        for (const segment of currentPath) {
            if (targetSchema.type === 'array' && segment === '$items') {
                targetSchema = targetSchema.items || {};
            } else if (targetSchema.properties && targetSchema.properties[segment]) {
                targetSchema = targetSchema.properties[segment];
            } else {
                return undefined;
            }
        }

        return targetSchema.description;
    }, [currentSchema, currentPath]);

    const hasValidationErrors = Object.keys(validationErrors).length > 0;

    const menuItems = useMemo(() => [
        ...(!isEditMode ? [{
            label: strings.components.schemaEditor.actions.edit,
            icon: <faIcons.FaPencil className='mr-2' />,
            command: canEdit ? handleEdit : undefined,
            className: !canEdit ? 'edit-disabled-with-reason' : undefined,
            template: !canEdit && canNotEditReason ? (item: MenuItem) => (
                <div
                    className="p-menuitem-link p-disabled"
                    data-pr-tooltip={canNotEditReason}
                    data-pr-position="bottom"
                    style={{ cursor: 'not-allowed', opacity: 0.6 }}
                >
                    {item.icon}
                    <span className="p-menuitem-text">{item.label}</span>
                </div>
            ) : undefined
        }] : []),
        ...(isEditMode ? [
            ...(!saveDisabled ? [{
                label: strings.components.schemaEditor.actions.save,
                icon: <faIcons.FaCheck className='mr-2' />,
                command: hasValidationErrors ? undefined : handleSave,
                disabled: hasValidationErrors
            }] : []),
            ...(!cancelDisabled ? [{
                label: strings.components.schemaEditor.actions.cancel,
                icon: <faIcons.FaXmark className='mr-2' />,
                command: handleCancel
            }] : []),
            {
                label: strings.components.schemaEditor.actions.addProperty,
                icon: <faIcons.FaPlus className='mr-2' />,
                command: addProperty
            }
        ] : [])
    ], [isEditMode, handleSave, handleCancel, handleEdit, addProperty, canEdit, canNotEditReason, hasValidationErrors, saveDisabled, cancelDisabled]);

    const breadcrumbItems = getBreadcrumbItems();
    const isAtRoot = currentPath.length === 0;
    const currentDescription = getCurrentDescription();

    return (
        <div className="schema-editor" style={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
                <div className="px-4 py-4">
                    <Tooltip target="[data-pr-tooltip]" />
                    <div className="schema-editor-menubar">
                        <Menubar aria-label="Actions" model={menuItems} />
                    </div>
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
                    {currentDescription && (
                        <div style={{ fontSize: '0.875rem', color: 'var(--text-color-secondary)', marginTop: '0.5rem', marginLeft: '2.5rem', fontStyle: 'italic', display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                            <faIcons.FaCircleInfo />
                            <span>{currentDescription}</span>
                        </div>
                    )}
                </div>

                <div style={{ flex: 1, overflow: 'auto', padding: '1rem' }}>
                    <Tooltip key={`nav-${eventTypeName}-${currentPath.join('/')}`} target=".schema-navigation-tooltip" mouseTrack mouseTrackTop={15} />
                    <Tooltip key={`desc-${eventTypeName}-${currentPath.join('/')}`} target=".schema-description-tooltip" />
                    <DataTable
                        key={`${isEditMode}-${currentPath.join('/')}`}
                        value={properties}
                        dataKey="id"
                        emptyMessage={strings.components.schemaEditor.emptyMessage}
                        rowClassName={(rowData: JsonSchemaProperty) => {
                            if (!isEditMode && (rowData.type === 'object' || (rowData.type === 'array' && rowData.items?.type === 'object'))) {
                                return css.navigableRow;
                            }
                            return '';
                        }}
                        onRowClick={(e) => {
                            if (!isEditMode) {
                                const rowData = e.data as JsonSchemaProperty;
                                if (rowData.type === 'object') {
                                    navigateToProperty(rowData.name);
                                } else if (rowData.type === 'array' && rowData.items?.type === 'object') {
                                    navigateToArrayItems(rowData.name);
                                }
                            }
                        }}
                        pt={{
                            root: { style: { border: 'none' } },
                            tbody: { style: { borderTop: '1px solid var(--surface-border)' } }
                        }}
                    >
                        <Column
                            field="name"
                            header={strings.components.schemaEditor.columns.property}
                            body={(rowData: JsonSchemaProperty) => (
                                <NameCell
                                    rowData={rowData}
                                    isEditMode={isEditMode}
                                    onUpdate={updateProperty}
                                    validationError={validationErrors[rowData.id!]}
                                />
                            )}
                            style={{ width: '30%' }}
                        />
                        <Column
                            header={strings.components.schemaEditor.columns.type}
                            body={(rowData: JsonSchemaProperty) => (
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
