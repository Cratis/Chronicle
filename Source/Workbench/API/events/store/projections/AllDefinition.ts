/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/cratis-fundamentals';

import { PropertyPath } from './PropertyPath';

export class AllDefinition {

    @field(PropertyPath, true)
    properties!: PropertyPath[];

    @field(Boolean)
    includeChildren!: boolean;
}
