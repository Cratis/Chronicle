// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Constructor } from './Constructor';
import { Field } from './Field';
import { Fields } from './Fields';
import { DerivedType } from './DerivedType';

type typeSerializer = (value: any) => any;

const typeSerializers: Map<Constructor, typeSerializer> = new Map<Constructor, typeSerializer>([
    [Number, (value: any) => value],
    [String, (value: any) => value],
    [Boolean, (value: any) => value],
    [Date, (value: any) => new Date(value)]
]);

const deserializeValue = (field: Field, value: any) => {
    if (typeSerializers.has(field.type)) {
        return typeSerializers.get(field.type)!(value);
    } else {
        let type = field.type;
        if (field.derivatives.length > 0 && value[JsonSerializer.DerivedTypeIdProperty]) {
            const derivedTypeId = value[JsonSerializer.DerivedTypeIdProperty];
            type = field.derivatives.find(_ => DerivedType.get(_) == derivedTypeId) || type;
        }

        return JsonSerializer.deserialize(type, JSON.stringify(value));
    }
};

/**
 * Represents a serializer for JSON.
 */
export class JsonSerializer {
    static readonly DerivedTypeIdProperty: string = "_derivedTypeId";

    /**
     * Deserialize a JSON string to the specific type.
     * @param {Constructor} targetType Type to deserialize to.
     * @param {string} json Actual JSON to deserialize.
     * @returns An instance of the target type.
     */
    static deserialize<TResult extends {}>(targetType: Constructor<TResult>, json: string): TResult {
        const parsed = JSON.parse(json);
        return this.deserializeFromInstance<TResult>(targetType, parsed);
    }

    /**
     * Deserialize a array JSON string to an array of the specific instance type.
     * @param {Constructor} targetType Type to deserialize to.
     * @param {string} json Actual JSON to deserialize.
     * @returns An array of instances of the target type.
     */
    static deserializeArray<TResult extends {}>(targetType: Constructor<TResult>, json: string): TResult[] {
        const parsed = JSON.parse(json);
        return this.deserializeArrayFromInstance(targetType, parsed);
    }

    /**
     * Deserialize an any instance to a specific instance type.
     * @param {Constructor} targetType Type to deserialize to.
     * @param {*} instance Actual instance to deserialize.
     * @returns An instance of the target type.
     */
    static deserializeFromInstance<TResult extends {}>(targetType: Constructor<TResult>, instance: any): TResult {
        const fields = Fields.getFieldsForType(targetType as Constructor);
        const deserialized = new targetType();
        for (const field of fields) {
            let value = instance[field.name];
            if (value) {
                if (field.enumerable) {
                    value = value.map(_ => deserializeValue(field, _));
                } else {
                    value = deserializeValue(field, value);
                }
            }

            deserialized[field.name] = value;
        }

        if ((targetType as Constructor) == Object) {
            const objectFields = Object.keys(instance).filter((value, index, arr) => {
                return !fields.some(_ => _.name == value);
            });

            for (const field of objectFields) {
                deserialized[field] = instance[field];
            }
        }

        return deserialized;
    }

    /**
     * Deserialize an array of any instances to an array of specific instance types.
     * @param {Constructor} targetType Type to deserialize to.
     * @param {instances} instances Actual instances to deserialize.
     * @returns An array of instances of the target type.
     */
    static deserializeArrayFromInstance<TResult extends {}>(targetType: Constructor<TResult>, instances: any): TResult[] {
        const deserialized: TResult[] = [];

        for (const instance of instances) {
            deserialized.push(this.deserializeFromInstance<TResult>(targetType, instance));
        }

        return deserialized;
    }
}
