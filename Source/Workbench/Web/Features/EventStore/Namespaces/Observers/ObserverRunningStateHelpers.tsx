// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObserverRunningState } from 'Api/Observation/ObserverRunningState';
import { ColumnFilterElementTemplateOptions } from 'primereact/column';
import { MultiSelect, MultiSelectChangeEvent } from 'primereact/multiselect';
import { getObserverRunningStateAsText } from './getObserverRunningStateAsText';

interface ObserverRunningStateOption {
    name: string;
    value: ObserverRunningState | string;
}

const observerRunningStates = Object.values(ObserverRunningState)
    .filter((_) => typeof _ === 'number')
    .map<ObserverRunningStateOption>((_) => ({
        name: getObserverRunningStateAsText(_),
        value: _,
    }));
export const ObserverRunningStateFilterTemplate = (
    options: ColumnFilterElementTemplateOptions
) => {
    return (
        <MultiSelect
            value={options.value}
            options={observerRunningStates}
            itemTemplate={(option) => option.name}
            onChange={(e: MultiSelectChangeEvent) => {
                options.filterCallback(e.value);
            }}
            placeholder='All'
            optionLabel='name'
        />
    );
};
