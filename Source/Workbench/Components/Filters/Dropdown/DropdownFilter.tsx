// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Dropdown, DropdownChangeEvent } from 'primereact/dropdown';
import css from '../Filter.module.css';
export interface DropdownFilterProps {
    options: any;
    value: string | number | undefined;
    onChange: (evt: DropdownChangeEvent) => void;
}

export const DropdownFilter = (props: DropdownFilterProps) => {
    const { value, options, onChange } = props;
    return (
        <Dropdown
            value={value}
            options={options}
            optionLabel='label'
            onChange={onChange}
            className={css.dropdown}
            placeholder='Select item'
        />
    );
};
