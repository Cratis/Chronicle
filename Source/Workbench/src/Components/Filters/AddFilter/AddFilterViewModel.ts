import { makeAutoObservable } from 'mobx';


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
        makeAutoObservable(this);
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
