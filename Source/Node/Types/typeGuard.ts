// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { PrimitiveOrConstructor } from './PrimitiveOrConstructor';
import { PrimitiveTypeMap } from './PrimitiveTypeMap';

export type GuardedType<T extends PrimitiveOrConstructor> = T extends new(...args: any[]) => infer U ?
                                                                U
                                                                : T extends keyof PrimitiveTypeMap ? PrimitiveTypeMap[T] : never;

export function typeGuard<T extends PrimitiveOrConstructor>(o: any, className: T): o is GuardedType<T> {
    const localPrimitiveOrConstructor: PrimitiveOrConstructor = className;
    if (typeof localPrimitiveOrConstructor === 'string') {
        return typeof o === localPrimitiveOrConstructor;
    }
    return o instanceof localPrimitiveOrConstructor;
}
