// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { InputText } from 'primereact/inputtext';
import * as faIcons from 'react-icons/fa6';
import strings from 'Strings';
import { SchemaProperty } from './types';

export interface NameCellProps {
    rowData: SchemaProperty;
    isEditMode: boolean;
    onUpdate: (oldName: string, field: keyof SchemaProperty, value: unknown) => void;
    validationError?: string;
}

export const NameCell = ({ rowData, isEditMode, onUpdate, validationError }: NameCellProps) => {
    if (!isEditMode) {
        const isNavigable = rowData.type === 'object' || (rowData.type === 'array' && rowData.items?.type === 'object');
        const navigationTooltipText = rowData.type === 'object'
            ? strings.components.schemaEditor.tooltips.navigateToObjectProperties
            : strings.components.schemaEditor.tooltips.navigateToItemDefinition;

        return (
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                <span
                    className={isNavigable ? 'schema-navigation-tooltip' : undefined}
                    data-pr-tooltip={isNavigable ? navigationTooltipText : undefined}
                    data-pr-position="top"
                >
                    {rowData.name}
                </span>
                {rowData.description && (
                    <faIcons.FaCircleInfo
                        className="schema-description-tooltip"
                        style={{ color: 'var(--text-color-secondary)', fontSize: '0.875rem' }}
                        data-pr-tooltip={rowData.description}
                        data-pr-position="right"
                    />
                )}
            </div>
        );
    }

    return (
        <InputText
            value={rowData.name}
            onChange={(e) => onUpdate(rowData.name, 'name', e.target.value)}
            className={`w-full ${validationError ? 'p-invalid' : ''}`}
            data-pr-tooltip={validationError}
            data-pr-position="top"
        />
    );
};
