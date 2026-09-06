// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { JsonSchema as ComponentsJsonSchema } from '@cratis/components/types';
import type { JsonSchema as ProjectionJsonSchema } from '@cratis/screenplay-language/projection';

/**
 * Both `@cratis/components` and `@cratis/screenplay-language` declare their own `JsonSchema`,
 * and the Workbench sits between them: the projection language supplies the schema the editor
 * offers completions from, while `SchemaEditor` edits the very same document.
 *
 * The two declarations describe the same parsed JSON and differ in exactly one member —
 * `JsonSchemaProperty.required`, which Components types as the JSON Schema `string[]` of
 * required property names and the projection language types as a `boolean`. Nothing in the
 * Workbench reads that member; both sides only pass the parsed document through. So the
 * values are interchangeable at runtime and the conversion is a re-type, not a transform.
 *
 * Keeping it to these two functions means the assertion lives in one documented place instead
 * of being sprinkled across the call sites that cross the boundary.
 */
export const toComponentsSchema = (schema: ProjectionJsonSchema): ComponentsJsonSchema =>
    schema as unknown as ComponentsJsonSchema;

/** The reverse of {@link toComponentsSchema}. */
export const toProjectionSchema = (schema: ComponentsJsonSchema): ProjectionJsonSchema =>
    schema as unknown as ProjectionJsonSchema;
