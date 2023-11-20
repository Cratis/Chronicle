// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { observable, makeAutoObservable } from 'mobx';
import { AppendedEventWithJsonAsContent } from 'API/events/store/sequence/AppendedEventWithJsonAsContent';

export class SequencesViewModel {

    constructor() {
        makeAutoObservable(this);
    }

    @observable events: AppendedEventWithJsonAsContent[] = [];
}
