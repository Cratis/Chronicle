// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { observable, makeAutoObservable } from 'mobx';

export class SequencesViewModel {

    constructor() {
        makeAutoObservable(this);
    }

    @observable stuff: number[] = [];
}
