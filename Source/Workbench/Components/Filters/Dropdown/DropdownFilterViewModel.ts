// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export class DropdownFilterViewModel {
    selectItem: string | null = null;

    constructor() {
    }

    setSelectItem(itm: string) {
        this.selectItem = itm;
    }

}
