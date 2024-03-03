// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObserverRunningState } from 'API/events/store/observers/ObserverRunningState';
import { ColumnFilterElementTemplateOptions } from 'primereact/column';
import { MultiSelect, MultiSelectChangeEvent } from 'primereact/multiselect';

export const getObserverRunningStateAsText = (
    runningState: ObserverRunningState | string
) => {
    switch (runningState) {
        case ObserverRunningState.new:
            return 'New';
        case ObserverRunningState.subscribing:
            return 'Subscribing';
        case ObserverRunningState.rewinding:
            return 'Rewinding';
        case ObserverRunningState.replaying:
            return 'Replaying';
        case ObserverRunningState.catchingUp:
            return 'CatchingUp';
        case ObserverRunningState.active:
            return 'Active';
        case ObserverRunningState.paused:
            return 'Paused';
        case ObserverRunningState.stopped:
            return 'Stopped';
        case ObserverRunningState.suspended:
            return 'Suspended';
        case ObserverRunningState.failed:
            return 'Failed';
        case ObserverRunningState.tailOfReplay:
            return 'TailOfReplay';
        case ObserverRunningState.disconnected:
            return 'Disconnected';
    }
    return '[N/A]';
};

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
