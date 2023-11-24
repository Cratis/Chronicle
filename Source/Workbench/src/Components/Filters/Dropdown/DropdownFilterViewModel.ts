import { makeAutoObservable } from 'mobx';

export class DropdownFilterViewModel {
    selectItem: string | null = null;

    constructor() {
        makeAutoObservable(this)
    }

    setSelectItem(itm: string) {
        this.selectItem = itm;
    }

}
