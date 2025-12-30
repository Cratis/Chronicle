// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export interface JsonSchema {
    type?: string;
    format?: string;
    description?: string;
    properties?: Record<string, JsonSchema>;
    items?: JsonSchema;
    required?: string[];
}

export interface JsonSchemaProperty {
    id?: string;
    name: string;
    type: string;
    format?: string;
    description?: string;
    items?: JsonSchema;
    properties?: Record<string, JsonSchema>;
    required?: boolean;
}
