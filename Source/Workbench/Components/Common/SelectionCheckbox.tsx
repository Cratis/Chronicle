// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Checkbox } from 'primereact/checkbox';

/**
 * Props for {@link SelectionCheckbox}.
 */
export interface SelectionCheckboxProps {
    /** Whether the row this checkbox represents is currently selected. */
    checked: boolean;

    /** Invoked when the checkbox is toggled by the user. */
    onToggle: () => void;

    /** Accessible name for the checkbox. Defaults to `'Select row'`. */
    ariaLabel?: string;
}

/**
 * A single row-selection checkbox for use as a {@link Column} `body` renderer in a bulk-select
 * table - the checkbox column itself carries the selection state, independent of the surrounding
 * `DataPage`/`DataTable`'s own single-row `selection`, since that mechanism only ever tracks one
 * row at a time.
 */
export const SelectionCheckbox = ({ checked, onToggle, ariaLabel = 'Select row' }: SelectionCheckboxProps) => (
    <Checkbox.Root checked={checked} onCheckedChange={onToggle} aria-label={ariaLabel}>
        <Checkbox.Box>
            <Checkbox.Indicator />
        </Checkbox.Box>
    </Checkbox.Root>
);
