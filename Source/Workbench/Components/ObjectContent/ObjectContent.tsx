// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Json } from 'Features/index';
import { Tooltip } from 'primereact/tooltip';
import React, { useState, useCallback, useMemo, useEffect } from 'react';
import * as faIcons from 'react-icons/fa6';
import strings from 'Strings';
import { ObjectNavigationalBar } from 'Components';
import { JsonSchema, JsonSchemaProperty } from 'Components/JsonSchema';
import { InputText } from 'primereact/inputtext';
import { InputNumber } from 'primereact/inputnumber';
import { Checkbox } from 'primereact/checkbox';
import { Calendar } from 'primereact/calendar';
import { InputTextarea } from 'primereact/inputtextarea';

export interface ObjectContentProps {
    object: Json;
    timestamp?: Date;
    schema: JsonSchema;
    editMode?: boolean;
    onChange?: (object: Json) => void;
    onValidationChange?: (hasErrors: boolean) => void;
}

export const ObjectContent = ({ object, timestamp, schema, editMode = false, onChange, onValidationChange }: ObjectContentProps) => {
    const [navigationPath, setNavigationPath] = useState<string[]>([]);
    const [validationErrors, setValidationErrors] = useState<Record<string, string>>({});

    const validateValue = useCallback((propertyName: string, value: Json, property: JsonSchemaProperty): string | undefined => {
        // In edit mode, all fields must have a value
        if (editMode) {
            if (value === null || value === undefined || value === '') {
                return 'This field is required';
            }
        } else {
            // In view mode, only check schema-defined required fields
            const isRequired = schema.required?.includes(propertyName);
            if (isRequired && (value === null || value === undefined || value === '')) {
                return 'This field is required';
            }
        }

        if (property.type === 'string' && typeof value === 'string') {
            if (property.format === 'email' && value && !value.match(/^[^\s@]+@[^\s@]+\.[^\s@]+$/)) {
                return 'Invalid email format';
            }
            if (property.format === 'uri' && value && !value.match(/^https?:\/\/.+/)) {
                return 'Invalid URI format';
            }
        }

        if (property.type === 'number' || property.type === 'integer') {
            if (value !== null && value !== undefined && value !== '' && isNaN(Number(value))) {
                return 'Must be a valid number';
            }
        }

        return undefined;
    }, [schema, editMode]);

    // Validate all properties whenever object or schema changes in edit mode
    useEffect(() => {
        if (!editMode || navigationPath.length > 0) return;

        const errors: Record<string, string> = {};
        const properties = schema.properties || {};

        Object.entries(properties).forEach(([propertyName, property]) => {
            const value = (object as Record<string, Json>)[propertyName];
            const error = validateValue(propertyName, value, property as JsonSchemaProperty);
            if (error) {
                errors[propertyName] = error;
            }
        });

        setValidationErrors(errors);
    }, [object, schema, editMode, navigationPath, validateValue]);

    useEffect(() => {
        if (editMode && onValidationChange) {
            const hasErrors = Object.keys(validationErrors).length > 0;
            onValidationChange(hasErrors);
        }
    }, [validationErrors, editMode, onValidationChange]);

    const getValueAtPath = useCallback((data: Json, path: string[]): Json | null => {
        let current: Json = data;
        for (const segment of path) {
            if (current === null || current === undefined) return null;
            if (typeof current === 'object' && !Array.isArray(current) && current !== null) {
                current = (current as { [key: string]: Json })[segment];
            } else {
                return null;
            }
        }
        return current;
    }, []);

    const navigateToProperty = useCallback((key: string) => {
        setNavigationPath([...navigationPath, key]);
    }, [navigationPath]);

    const navigateToBreadcrumb = useCallback((index: number) => {
        if (index === 0) {
            setNavigationPath([]);
        } else {
            setNavigationPath(navigationPath.slice(0, index));
        }
    }, [navigationPath]);

    const currentData = useMemo(() => {
        if (navigationPath.length === 0) {
            return object;
        }

        const lastKey = navigationPath[navigationPath.length - 1];
        const pathToParent = navigationPath.slice(0, -1);

        const parentValue = pathToParent.length > 0
            ? getValueAtPath(object, pathToParent)
            : object;

        if (parentValue && typeof parentValue === 'object' && !Array.isArray(parentValue)) {
            const value = (parentValue as { [k: string]: Json })[lastKey];

            if (Array.isArray(value)) {
                return value;
            } else if (value && typeof value === 'object') {
                return value;
            }
        }

        return object;
    }, [object, navigationPath, getValueAtPath]);

    const currentProperties = useMemo(() => {
        const properties = schema.properties || {};

        if (navigationPath.length === 0) {
            return properties;
        }

        return {};
    }, [schema, navigationPath]);

    const tableStyle: React.CSSProperties = {
        width: '100%',
        borderCollapse: 'collapse',
        fontFamily: '-apple-system, BlinkMacSystemFont, "SF Mono", monospace',
        fontSize: '13px',
    };

    const rowStyle: React.CSSProperties = {
        borderBottom: '1px solid rgba(255,255,255,0.1)',
    };

    const labelStyle: React.CSSProperties = {
        padding: '8px 12px',
        color: 'rgba(255,255,255,0.6)',
        textAlign: 'left',
        fontWeight: 500,
        width: '140px',
        whiteSpace: 'nowrap',
    };

    const valueStyle: React.CSSProperties = {
        padding: '8px 12px',
        color: '#fff',
        textAlign: 'left',
    };

    const infoIconStyle: React.CSSProperties = {
        fontSize: '0.875rem',
        color: 'var(--text-color-secondary)',
        flexShrink: 0,
    };

    const updateValue = useCallback((propertyName: string, newValue: Json) => {
        if (!onChange) return;

        const updatedObject = { ...(object as Record<string, Json>) };
        updatedObject[propertyName] = newValue;
        onChange(updatedObject);
    }, [object, onChange]);

    const renderEditField = (propertyName: string, property: JsonSchemaProperty, value: Json) => {
        const error = validationErrors[propertyName];

        const handleChange = (newValue: Json) => {
            updateValue(propertyName, newValue);
            const validationError = validateValue(propertyName, newValue, property);
            setValidationErrors(prev => {
                const newErrors = { ...prev };
                if (validationError) {
                    newErrors[propertyName] = validationError;
                } else {
                    delete newErrors[propertyName];
                }
                return newErrors;
            });
        };

        const inputStyle = {
            width: '100%',
            ...(error ? { borderColor: 'var(--red-500)' } : {})
        };

        if (property.type === 'boolean') {
            return (
                <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
                    <Checkbox
                        checked={Boolean(value)}
                        onChange={(e) => handleChange(e.checked ?? false)}
                    />
                    {error && <small className="p-error">{error}</small>}
                </div>
            );
        }

        if (property.type === 'number' || property.type === 'integer') {
            return (
                <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
                    <InputNumber
                        value={(value === null || value === undefined) ? null : Number(value)}
                        onValueChange={(e) => handleChange(e.value ?? null)}
                        mode="decimal"
                        useGrouping={false}
                        style={inputStyle}
                    />
                    {error && <small className="p-error">{error}</small>}
                </div>
            );
        }

        if (property.type === 'string' && property.format === 'date-time') {
            const dateValue = value ? new Date(value as string) : null;
            return (
                <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
                    <Calendar
                        value={dateValue}
                        onChange={(e) => handleChange(e.value instanceof Date ? e.value.toISOString() : null)}
                        showTime
                        showIcon
                        style={inputStyle}
                    />
                    {error && <small className="p-error">{error}</small>}
                </div>
            );
        }

        if (property.type === 'string' && property.format === 'date') {
            const dateValue = value ? new Date(value as string) : null;
            return (
                <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
                    <Calendar
                        value={dateValue}
                        onChange={(e) => handleChange(e.value instanceof Date ? e.value.toISOString().split('T')[0] : null)}
                        showIcon
                        style={inputStyle}
                    />
                    {error && <small className="p-error">{error}</small>}
                </div>
            );
        }

        if (property.type === 'array') {
            return (
                <div
                    className="flex align-items-center gap-2"
                    style={{ color: 'rgba(255,255,255,0.6)', fontStyle: 'italic' }}
                >
                    <span>Array editing not yet supported</span>
                </div>
            );
        }

        if (property.type === 'object') {
            return (
                <div
                    className="flex align-items-center gap-2"
                    style={{ color: 'rgba(255,255,255,0.6)', fontStyle: 'italic' }}
                >
                    <span>Object editing not yet supported</span>
                </div>
            );
        }

        // Default to text input (for strings and unknown types)
        const isLongText = (value as string)?.length > 50;

        if (isLongText) {
            return (
                <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
                    <InputTextarea
                        value={String(value ?? '')}
                        onChange={(e) => handleChange(e.target.value)}
                        rows={3}
                        style={inputStyle}
                    />
                    {error && <small className="p-error">{error}</small>}
                </div>
            );
        }

        return (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
                <InputText
                    value={String(value ?? '')}
                    onChange={(e) => handleChange(e.target.value)}
                    style={inputStyle}
                />
                {error && <small className="p-error">{error}</small>}
            </div>
        );
    };

    const renderValue = (value: Json, propertyName: string) => {
        if (value === null || value === undefined) return '';

        if (Array.isArray(value)) {
            return (
                <div
                    className="flex align-items-center gap-2 cursor-pointer"
                    onClick={() => navigateToProperty(propertyName)}
                    style={{ color: 'var(--primary-color)', display: 'flex', alignItems: 'center' }}
                >
                    <span>{strings.eventStore.namespaces.readModels.labels.array}[{value.length}]</span>
                    <faIcons.FaArrowRight style={{ fontSize: '0.875rem', display: 'inline-flex' }} />
                </div>
            );
        }

        if (typeof value === 'object') {
            return (
                <div
                    className="flex align-items-center gap-2 cursor-pointer"
                    onClick={() => navigateToProperty(propertyName)}
                    style={{ color: 'var(--primary-color)', display: 'flex', alignItems: 'center' }}
                >
                    <span>{strings.eventStore.namespaces.readModels.labels.object}</span>
                    <faIcons.FaArrowRight style={{ fontSize: '0.875rem', display: 'inline-flex' }} />
                </div>
            );
        }

        return String(value);
    };

    const renderTable = () => {
        if (Array.isArray(currentData)) {
            if (currentData.length === 0) return <div style={{ padding: '12px', color: 'rgba(255,255,255,0.6)' }}>Empty array</div>;

            const firstItem = currentData[0];
            if (typeof firstItem === 'object' && firstItem !== null && !Array.isArray(firstItem)) {
                const keys = Object.keys(firstItem);

                return (
                    <table style={tableStyle}>
                        <tbody>
                            {currentData.map((item, index) => (
                                <React.Fragment key={index}>
                                    {index > 0 && (
                                        <tr style={{ height: '8px', background: 'rgba(255,255,255,0.05)' }}>
                                            <td colSpan={2}></td>
                                        </tr>
                                    )}
                                    {keys.map((key) => (
                                        <tr key={`${index}-${key}`} style={rowStyle}>
                                            <td style={labelStyle}>{key}</td>
                                            <td style={valueStyle}>{renderValue((item as Record<string, Json>)[key], key)}</td>
                                        </tr>
                                    ))}
                                </React.Fragment>
                            ))}
                        </tbody>
                    </table>
                );
            } else {
                return (
                    <table style={tableStyle}>
                        <tbody>
                            {currentData.map((item, index) => (
                                <tr key={index} style={rowStyle}>
                                    <td style={labelStyle}>[{index}]</td>
                                    <td style={valueStyle}>{renderValue(item, `[${index}]`)}</td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                );
            }
        }

        const entries = navigationPath.length === 0
            ? Object.entries(currentProperties)
            : Object.entries(currentData as { [key: string]: Json });

        return (
            <table style={tableStyle}>
                <tbody>
                    {entries.map(([propertyName, propertyDef]: [string, JsonSchemaProperty | Json]) => {
                        const value = (currentData as Record<string, Json>)[propertyName];

                        const isSchemaProperty = navigationPath.length === 0;
                        const property = isSchemaProperty && typeof propertyDef === 'object' && propertyDef !== null && 'type' in propertyDef
                            ? (propertyDef as JsonSchemaProperty)
                            : null;
                        const description = property?.description;

                        return (
                            <tr key={propertyName} style={rowStyle}>
                                <td style={labelStyle}>
                                    <span style={{ display: 'inline-flex', alignItems: 'center', gap: '6px' }}>
                                        {propertyName}
                                        {description && (
                                            <faIcons.FaCircleInfo
                                                className="property-info-icon"
                                                style={infoIconStyle}
                                                data-pr-tooltip={description}
                                                data-pr-position="right" />
                                        )}
                                    </span>
                                </td>
                                <td style={valueStyle}>
                                    {editMode && property ?
                                        renderEditField(propertyName, property, value) :
                                        renderValue(value as Json, propertyName)
                                    }
                                </td>
                            </tr>
                        );
                    })}
                </tbody>
            </table>
        );
    };

    return (
        <div className="order-content" style={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
            <Tooltip target="[data-pr-tooltip]" />
            <ObjectNavigationalBar
                navigationPath={navigationPath}
                onNavigate={navigateToBreadcrumb}
            />
            {renderTable()}
            {timestamp && (
                <div style={{
                    marginTop: '20px',
                    padding: '12px',
                    background: 'rgba(100, 150, 255, 0.1)',
                    borderRadius: '8px',
                    fontSize: '12px',
                    color: 'rgba(255,255,255,0.6)'
                }}>
                    Timestamp: {timestamp.toLocaleString()}
                </div>
            )}
        </div>
    );
};
