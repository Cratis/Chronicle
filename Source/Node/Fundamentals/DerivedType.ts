// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Constructor } from './Constructor';

export class DerivedType {
    static set(target: Constructor, identifier: string) {
        Reflect.defineMetadata('derivedType', identifier, target);
    }

    static get(target: Constructor): string {
        return Reflect.getOwnMetadata('derivedType', target);
    }
}
