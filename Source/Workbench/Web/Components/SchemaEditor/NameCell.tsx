// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { InputText } from 'primereact/inputtext';
import strings from 'Strings';
import { SchemaProperty } from './types';

export interface NameCellProps {
    rowData: SchemaProperty;
    isEditMode: boolean;
    onUpdate: (oldName: string, field: keyof SchemaProperty, value: unknown) => void;
}

export const NameCell = ({ rowData, isEditMode, onUpdate }: NameCellProps) => {
    if (!isEditMode) {
        const isNavigable = rowData.type === 'object' || (rowData.type === 'array' && rowData.items?.type === 'object');
        const tooltipText = rowData.type === 'object'
            ? strings.components.schemaEditor.tooltips.navigateToObjectProperties
            : strings.components.schemaEditor.tooltips.navigateToItemDefinition;

        return (
            <span
                data-pr-tooltip={isNavigable ? tooltipText : undefined}
                data-pr-position="top"
            >
                {rowData.name}
            </span>
        );
    }

    return (
        <InputText
            value={rowData.name}
            onChange={(e) => onUpdate(rowData.name, 'name', e.target.value)}
            className="w-full"
        />
    );
};
