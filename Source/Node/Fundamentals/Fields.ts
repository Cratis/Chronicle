// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import 'reflect-metadata';
import { Constructor } from './Constructor';
import { Field } from './Field';

/**
 * Represents a system working with fields on types.
 */
export class Fields {
    static addFieldToType(target: Constructor, field: string, fieldType: Constructor, enumerable: boolean, derivatives: Constructor[]) {
        let fields: Map<string, Field> = new Map<string, Field>();
        if (Reflect.hasOwnMetadata('fields', target)) {
            fields = Reflect.getOwnMetadata('fields', target);
        }
        fields.set(field, new Field(field, fieldType, enumerable, derivatives));
        Reflect.defineMetadata('fields', fields, target);
    }

    static getFieldsForType(target: Constructor): Field[] {
        const fields: Field[] = [];
        if (Reflect.hasOwnMetadata('fields', target)) {
            const fieldsMap = Reflect.getOwnMetadata('fields', target) as Map<string, Field>;
            for (const field of fieldsMap.entries()) {
                fields.push(field[1]);
            }
        }

        return fields;
    }
}
