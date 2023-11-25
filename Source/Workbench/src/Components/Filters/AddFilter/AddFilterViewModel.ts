// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

type FilterObject = {
    filter: string;
    operator: string;
    value: string;
};
export class AddFilterViewModel {
    currentFilter = '';
    currentOperator = '';
    currentValue = '';
    appliedFilters: FilterObject[] = [];

    constructor() {
    }

    setCurrent(key: 'currentFilter' | 'currentOperator' | 'currentValue', value: string) {
        this[key] = value;
    }


    applyCurrentSelection() {
        this.appliedFilters.push({
            filter: this.currentFilter,
            operator: this.currentOperator,
            value: this.currentValue,
        });
        this.resetCurrentSelection();
    }

    resetCurrentSelection() {
        this.currentFilter = '';
        this.currentOperator = '';
        this.currentValue = '';
    }
}
