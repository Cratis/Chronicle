// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Guid } from '@aksio/fundamentals';
import { FilterOperator } from './FilterOperator';

export class Filter {
    constructor(
        readonly id: Guid,
        readonly property: string,
        readonly operator: FilterOperator,
        readonly value: string) {
    }
}

