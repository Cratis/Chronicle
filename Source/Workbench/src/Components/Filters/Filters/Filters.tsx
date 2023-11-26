// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DropdownFilter } from '../Dropdown/DropdownFilter';
import { DropdownChangeEvent } from 'primereact/dropdown';
import { FiltersViewModel } from './FiltersViewModel';
import { OverlayPanel } from 'primereact/overlaypanel';
import { testOptions } from '../../Common/TestOptions';
import { Button } from 'primereact/button';
import css from './Filters.module.css';
import { Chip } from 'primereact/chip';
import { useRef } from 'react';
import { withViewModel } from 'MVVM';

export const Filters = withViewModel(FiltersViewModel, ({ viewModel }) => {
    const op = useRef<OverlayPanel>(null);

    const disabled =
        !viewModel?.currentFilter &&
        !viewModel?.currentOperator &&
        !viewModel?.currentValue;

    const handleChange = (
        evt: DropdownChangeEvent,
        type: 'currentFilter' | 'currentOperator' | 'currentValue'
    ) => {
        switch (type) {
            case 'currentFilter':
                viewModel?.setCurrent(type, evt.value);
                break;
            case 'currentOperator':
                viewModel?.setCurrent(type, evt.value);
                break;
            case 'currentValue':
                viewModel?.setCurrent(type, evt.value);
                break;
            default:
                break;
        }
    };

    const handleApply = (evt: React.SyntheticEvent) => {
        viewModel.applyCurrentSelection();
        op.current?.toggle(evt);
    };

    return (
        <div>
            <div className={css.ctaContainer}>
                {viewModel.appliedFilters.length !== 0 && (
                    <div className={css.tagChips}>
                        {viewModel.appliedFilters.map((_, idx) => (
                            <Chip
                                key={idx}
                                label={`${_.filter}, ${_.operator}, ${_.value}`}
                                removable
                            />
                        ))}
                    </div>
                )}
                <Button
                    rounded
                    label='Add filter'
                    icon='pi pi-filter'
                    onClick={(e) => op.current && op.current.toggle(e)}
                />
            </div>
            <OverlayPanel ref={op} className={css.overlaypanel}>
                <h1 className={css.header}>Add filter</h1>
                <div className={css.labelAndDropdown}>
                    <p>Filter:</p>
                    <DropdownFilter
                        options={testOptions}
                        value={viewModel?.currentFilter}
                        onChange={(evt) => handleChange(evt, 'currentFilter')}
                    />
                </div>
                <div className={css.labelAndDropdown}>
                    <p>Operator:</p>
                    <DropdownFilter
                        options={testOptions}
                        value={viewModel?.currentOperator}
                        onChange={(evt) => handleChange(evt, 'currentOperator')}
                    />
                </div>
                <div className={css.labelAndDropdown}>
                    <p>Value:</p>
                    <DropdownFilter
                        options={testOptions}
                        value={viewModel?.currentValue}
                        onChange={(evt) => handleChange(evt, 'currentValue')}
                    />
                </div>
                <div className={css.cta}>
                    <Button
                        size='small'
                        label='Apply'
                        disabled={disabled}
                        onClick={(evt) => handleApply(evt)}
                    />
                    <Button
                        size='small'
                        label='Cancel'
                        onClick={(e) => op.current && op.current.toggle(e)}
                    />
                </div>
            </OverlayPanel>
        </div>
    );
});
