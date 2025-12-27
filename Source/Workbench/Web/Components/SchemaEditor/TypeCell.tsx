// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Button } from 'primereact/button';
import { Dropdown } from 'primereact/dropdown';
import strings from 'Strings';
import * as faIcons from 'react-icons/fa6';
import { SchemaProperty, JSONSchemaType } from './types';

export interface TypeCellProps {
    rowData: SchemaProperty;
    isEditMode: boolean;
    typeFormats: { label: string; value: string }[];
    onUpdateProperty: (oldName: string, field: keyof SchemaProperty, value: unknown) => void;
    onUpdateArrayItemType: (propertyName: string, itemType: string) => void;
    onNavigateToProperty: (propertyName: string) => void;
    onNavigateToArrayItems: (propertyName: string) => void;
    onRemoveProperty: (propertyName: string) => void;
}

const JSON_TYPES = [
    { label: 'String', value: 'string' },
    { label: 'Number', value: 'number' },
    { label: 'Integer', value: 'integer' },
    { label: 'Boolean', value: 'boolean' },
    { label: 'Array', value: 'array' },
    { label: 'Object', value: 'object' }
];

export const TypeCell = ({
    rowData,
    isEditMode,
    typeFormats,
    onUpdateProperty,
    onUpdateArrayItemType,
    onNavigateToProperty,
    onNavigateToArrayItems,
    onRemoveProperty
}: TypeCellProps) => {
    const capitalize = (str: string) => str.charAt(0).toUpperCase() + str.slice(1);

    const allTypeOptions = [
        ...JSON_TYPES,
        ...typeFormats.map(tf => ({ label: tf.label, value: `format:${tf.value}` }))
    ];

    let currentValue = rowData.type;
    if (rowData.format && rowData.type === 'string') {
        currentValue = `format:${rowData.format}`;
    }

    if (!isEditMode) {
        if (rowData.type === 'array') {
            const itemType = rowData.items?.type || 'string';
            const isNavigable = itemType === 'object';
            return (
                <div
                    className="flex align-items-center gap-2 w-full"
                    style={{ height: '100%' }}
                    data-pr-tooltip={isNavigable ? strings.components.schemaEditor.tooltips.navigateToItemDefinition : undefined}
                    data-pr-position="top"
                >
                    <span>Array of {capitalize(itemType)}</span>
                    {isNavigable && (
                        <>
                            <div style={{ flex: 1 }} />
                            <span style={{ display: 'flex', alignItems: 'center' }}>
                                <faIcons.FaArrowRight style={{ fontSize: '1rem', color: 'var(--primary-color)' }} />
                            </span>
                        </>
                    )}
                </div>
            );
        } else if (rowData.type === 'object') {
            return (
                <div
                    className="flex align-items-center gap-2 w-full"
                    style={{ height: '100%' }}
                    data-pr-tooltip={strings.components.schemaEditor.tooltips.navigateToObjectProperties}
                    data-pr-position="top"
                >
                    <span>Object</span>
                    <div style={{ flex: 1 }} />
                    <span style={{ display: 'flex', alignItems: 'center' }}>
                        <faIcons.FaArrowRight style={{ fontSize: '1rem', color: 'var(--primary-color)' }} />
                    </span>
                </div>
            );
        } else if (rowData.format) {
            const formatLabel = typeFormats.find(tf => tf.value === rowData.format)?.label || rowData.format;
            return formatLabel;
        }
        return capitalize(rowData.type);
    }

    return (
        <div className="flex align-items-center gap-2 w-full" style={{ minHeight: '2.5rem' }}>
            <Dropdown
                value={currentValue}
                options={allTypeOptions}
                onChange={(e) => {
                    const value = e.value;
                    if (value.startsWith('format:')) {
                        const format = value.substring(7);
                        onUpdateProperty(rowData.name, 'type', 'string');
                        onUpdateProperty(rowData.name, 'format', format);
                    } else {
                        onUpdateProperty(rowData.name, 'type', value);
                    }
                }}
                className="flex-1"
            />
            {rowData.type === 'array' && rowData.items && (
                <>
                    <span style={{ whiteSpace: 'nowrap' }}>of</span>
                    <Dropdown
                        value={rowData.items.type || 'string'}
                        options={JSON_TYPES}
                        onChange={(e) => onUpdateArrayItemType(rowData.name, e.value)}
                        className="flex-1"
                    />
                </>
            )}
            <div style={{ marginLeft: 'auto', display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                {rowData.type === 'array' && rowData.items?.type === 'object' && (
                    <Button
                        icon={<faIcons.FaArrowRight />}
                        className="p-button-text p-button-sm"
                        onClick={() => onNavigateToArrayItems(rowData.name)}
                        tooltip={strings.components.schemaEditor.tooltips.navigateToItemDefinition}
                        tooltipOptions={{ position: 'top' }}
                    />
                )}
                {rowData.type === 'object' && (
                    <Button
                        icon={<faIcons.FaArrowRight />}
                        className="p-button-text p-button-sm"
                        onClick={() => onNavigateToProperty(rowData.name)}
                        tooltip={strings.components.schemaEditor.tooltips.navigateToObjectProperties}
                        tooltipOptions={{ position: 'top' }}
                    />
                )}
                <Button
                    icon={<faIcons.FaTrash />}
                    className="p-button-text p-button-danger p-button-sm"
                    onClick={() => onRemoveProperty(rowData.name)}
                    tooltip={strings.components.schemaEditor.tooltips.deleteProperty}
                    tooltipOptions={{ position: 'top' }}
                />
            </div>
        </div>
    );
};
