// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Constructor } from './Constructor';
import { Field } from './Field';
import { Fields } from './Fields';

type typeSerializer = (value: any) => any;

const typeSerializers: Map<Constructor, typeSerializer> = new Map<Constructor, typeSerializer>([
    [Number, (value: any) => value],
    [String, (value: any) => value],
    [Date, (value: any) => new Date(value)]
]);

const deserializeValue = (field: Field, value: any) => {
    if (typeSerializers.has(field.type)) {
        return typeSerializers.get(field.type)!(value);
    } else {
        return JsonSerializer.deserialize(field.type, JSON.stringify(value));
    }
}

export class JsonSerializer {

    static deserialize<TResult extends {}>(targetType: Constructor<TResult>, json: string) {
        const parsed = JSON.parse(json);
        const fields = Fields.getFieldsForType(targetType as Constructor);

        const deserialized = new targetType();
        for (const field of fields) {
            let value = parsed[field.name];
            if (value) {
                if (field.enumerable) {
                    value = value.map(_ => deserializeValue(field, _));
                } else {
                    value = deserializeValue(field, value);
                }
            }

            deserialized[field.name] = value;
        }

        return deserialized;
    }
}
