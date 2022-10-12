// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Constructor } from './Constructor';
import { Fields } from './Fields';

export function field(targetType: Constructor, enumerable?: boolean, derivatives?: Constructor[]) {
    return function (target: any, propertyKey: string) {
        Fields.addFieldToType(target.constructor, propertyKey, targetType, enumerable || false, derivatives || []);
    };
}
