// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Constructor } from './Constructor';

/**
 * Represents a field on a type.
 */
 export class Field {
    constructor(readonly name: string, readonly type: Constructor, readonly enumerable: boolean, readonly derivatives: Constructor[]) {
    }
}
