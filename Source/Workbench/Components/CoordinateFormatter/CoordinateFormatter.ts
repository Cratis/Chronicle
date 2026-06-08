// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const CoordinateFormat = 'coordinate';

interface CoordinateValue {
    longitude: number;
    latitude: number;
}

function isCoordinateValue(value: unknown): value is CoordinateValue {
    return (
        value !== null &&
        typeof value === 'object' &&
        'longitude' in value &&
        'latitude' in value &&
        typeof (value as CoordinateValue).longitude === 'number' &&
        typeof (value as CoordinateValue).latitude === 'number'
    );
}

/**
 * Formats a coordinate value as a human-readable string.
 * @param value - The coordinate value to format.
 * @returns The formatted string, e.g. `lat: 51.5074, long: -0.1278`.
 */
export function formatCoordinate(value: CoordinateValue): string {
    return `lat: ${value.latitude}, long: ${value.longitude}`;
}

interface JsonSchemaProperty {
    type?: string;
    format?: string;
    properties?: Record<string, JsonSchemaProperty>;
}

interface JsonSchema {
    properties?: Record<string, JsonSchemaProperty>;
}

/**
 * Walks a JSON content object and its schema, replacing any property whose schema
 * carries `format: "coordinate"` with the formatted `lat: x, long: y` string so
 * that the ObjectContentEditor can render it as a plain string instead of an
 * expanded sub-object.
 *
 * Both the content and the schema are transformed: the schema property type is
 * changed to `string` so the editor does not attempt to expand the value.
 *
 * @param content - The raw JSON content object.
 * @param schema  - The JSON schema describing the content.
 * @returns A pair of `[transformedContent, transformedSchema]`.
 */
export function applyCoordinateFormatting(
    content: Record<string, unknown>,
    schema: JsonSchema
): [Record<string, unknown>, JsonSchema] {
    if (!schema.properties || typeof content !== 'object' || content === null) {
        return [content, schema];
    }

    const transformedContent = { ...content };
    const transformedSchemaProperties: Record<string, JsonSchemaProperty> = {};

    for (const [key, propertySchema] of Object.entries(schema.properties)) {
        if (propertySchema.format === CoordinateFormat && isCoordinateValue(transformedContent[key])) {
            transformedContent[key] = formatCoordinate(transformedContent[key] as CoordinateValue);
            transformedSchemaProperties[key] = { type: 'string' };
        } else {
            transformedSchemaProperties[key] = propertySchema;
        }
    }

    return [transformedContent, { ...schema, properties: transformedSchemaProperties }];
}
