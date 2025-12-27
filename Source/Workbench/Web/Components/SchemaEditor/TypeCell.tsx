// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Button } from 'primereact/button';
import { Dropdown } from 'primereact/dropdown';
import strings from 'Strings';
import * as faIcons from 'react-icons/fa6';
import { TypeFormat } from 'Api/TypeFormats';
import { SchemaProperty } from './types';

export interface TypeCellProps {
    rowData: SchemaProperty;
    isEditMode: boolean;
    typeFormats: TypeFormat[];
    onUpdateProperty: (oldName: string, field: keyof SchemaProperty, value: unknown) => void;
    onUpdateArrayItemType: (propertyName: string, itemType: string) => void;
    onNavigateToProperty: (propertyName: string) => void;
    onNavigateToArrayItems: (propertyName: string) => void;
    onRemoveProperty: (propertyName: string) => void;
}

const CONTAINER_TYPES = [
    { label: 'array', value: 'array' },
    { label: 'object', value: 'object' }
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
    const formatOptions = typeFormats.map(tf => {
        const format = (!tf.format || tf.format === '' ? tf.jsonType : tf.format);
        return { label: format, value: format };
    });

    const allTypeOptions = [
        ...formatOptions,
        ...CONTAINER_TYPES
    ];

    // Use format if available, otherwise use jsonType (from rowData.type)
    const displayValue = rowData.format || rowData.type;
    const currentValue = rowData.format || rowData.type;

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
                    <span>Array of {itemType}</span>
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
        }
        // Display format if available, otherwise type
        return displayValue;
    }

    return (
        <div className="flex align-items-center gap-2 w-full" style={{ minHeight: '2.5rem' }}>
            <Dropdown
                value={currentValue}
                options={allTypeOptions}
                onChange={(e) => {
                    const value = e.value;
                    if (value === 'array' || value === 'object') {
                        onUpdateProperty(rowData.name, 'type', value);
                        onUpdateProperty(rowData.name, 'format', undefined);
                    } else {
                        // Check if it's a format value
                        const typeFormat = typeFormats.find(tf => tf.format === value || tf.jsonType === value);
                        if (typeFormat) {
                            onUpdateProperty(rowData.name, 'type', typeFormat.jsonType);
                            onUpdateProperty(rowData.name, 'format', value);
                        } else {
                            // It's a jsonType value (string, number, integer, boolean)
                            onUpdateProperty(rowData.name, 'type', value);
                            onUpdateProperty(rowData.name, 'format', undefined);
                        }
                    }
                }}
                className="flex-1"
            />
            {rowData.type === 'array' && rowData.items && (
                <>
                    <span style={{ whiteSpace: 'nowrap' }}>of</span>
                    <Dropdown
                        value={rowData.items.type || 'string'}
                        options={allTypeOptions}
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
