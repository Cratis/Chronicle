// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Constructor } from '@aksio/cratis-fundamentals';
import 'reflect-metadata';

export function field(targetType: Constructor) {
    return function (target: any, propertyKey: string) {
        let fields: Map<string, Constructor> = new Map<string, Constructor>();
        if (Reflect.hasOwnMetadata('fields', target)) {
            fields = Reflect.getOwnMetadata('fields', target);
        }

        fields.set(propertyKey, targetType);
        Reflect.defineMetadata('fields', fields, target);
    };
}
