// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export enum PropertyType {
    String = "string",
    Byte = "byte",
    Number = "number",
    Boolean = "boolean",
    Date = "date",
    Guid = "guid",
    Object = "object",
    Enum = "enum",
    Array = "array",
    Unknown = "unknown"
}

export class EnumValueAndName {
    constructor(readonly value: string, readonly name: string) { }
}

export class Schema {

    readonly properties: any;
    constructor(readonly _schema: any) {
        if (_schema.properties) {
            this.properties = _schema.properties;
        }
        if (this._schema.allOf) {
            for (let type of this._schema.allOf) {
                if (type['$ref']) {
                    type = this._schema.definitions[type['$ref'].replace('#/definitions/', '')];
                }

                if (type.properties) {
                    this.properties = { ...this.properties, ...type.properties };
                }
            }
        }
    }

    getPropertyFromPath(propertyPath: string): any {
        const segments = propertyPath.split('.');
        let currentProperties = this.properties;
        let currentProperty: any;
        for (const segment of segments) {
            currentProperty = currentProperties[segment];
            if (currentProperty['$ref']) {
                const ref = currentProperty['$ref'].replace('#/definitions/', '');
                currentProperty = this._schema.definitions[ref];
                currentProperties = currentProperty.properties;
            }
        }

        return currentProperty;
    }

    getPropertyType(propertyPath: string): PropertyType {
        try {
            const currentProperty = this.getPropertyFromPath(propertyPath);

            if (currentProperty.enum && currentProperty['x-enumNames']) {
                return PropertyType.Enum;
            }

            switch (currentProperty.format) {
                case 'date-time':
                case 'date-time-offset':
                case 'date':
                case 'time': {
                    return PropertyType.Date;
                }

                case 'guid': {
                    return PropertyType.Guid;
                }

                case 'integer':
                case 'int32':
                case 'uint32':
                case 'int64':
                case 'uint64':
                case 'float':
                case 'double':
                case 'decimal': {
                    return PropertyType.Number;
                }

                case 'byte': {
                    return PropertyType.Byte;
                }
            }

            return currentProperty.type;
        } catch {
            return PropertyType.Unknown;
        }
    }

    getEnumValuesAndNames(propertyPath: string): EnumValueAndName[] {
        const currentProperty = this.getPropertyFromPath(propertyPath);
        const enumValuesAndNames: EnumValueAndName[] = [];

        (currentProperty.enum ?? []).forEach((value: string, index: number) => {
            enumValuesAndNames.push(new EnumValueAndName(value, currentProperty['x-enumNames'][index]));
        });

        return enumValuesAndNames;
    }
}
