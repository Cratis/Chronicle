// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { injectable } from 'tsyringe';
import { FailedPartition } from 'Api/Concepts/Observation';

@injectable()
export class FailedPartitionsViewModel {
    constructor() {

    }

    selectedFailedPartition: FailedPartition | undefined;

    async retry() {
        if (this.selectedFailedPartition) {
            console.log('Retry');
        }
    }
}
