// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export interface JSONSchemaType {
    type?: string;
    format?: string;
    description?: string;
    properties?: Record<string, JSONSchemaType>;
    items?: JSONSchemaType;
    required?: string[];
}

export interface SchemaProperty {
    id?: string;
    name: string;
    type: string;
    format?: string;
    description?: string;
    items?: JSONSchemaType;
    properties?: Record<string, JSONSchemaType>;
    required?: boolean;
}

export interface NavigationItem {
    name: string;
    path: string[];
}
