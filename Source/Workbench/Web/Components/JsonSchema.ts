// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export interface JsonSchema {
    title?: string;
    type?: string;
    format?: string;
    description?: string;
    properties?: Record<string, JsonSchemaProperty>;
    items?: JsonSchema;
    required?: string[];
}

export interface JsonSchemaProperty {
    id?: string;
    type: string;
    format?: string;
    description?: string;
    items?: JsonSchema;
    properties?: Record<string, JsonSchemaProperty>;
    required?: boolean;
}
