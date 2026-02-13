// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export interface JsonSchema {
    title?: string;
    name?: string;
    $id?: string;
    $ref?: string;
    type?: string;
    format?: string;
    description?: string;
    properties?: Record<string, JsonSchemaProperty>;
    items?: JsonSchema;
    required?: string[];
    definitions?: Record<string, JsonSchema>;
}

export interface JsonSchemaProperty {
    id?: string;
    name?: string;
    type: string;
    format?: string;
    description?: string;
    items?: JsonSchema;
    properties?: Record<string, JsonSchemaProperty>;
    required?: boolean;
    $ref?: string;
}

export interface NavigationItem {
    name: string;
    path: string[];
}
