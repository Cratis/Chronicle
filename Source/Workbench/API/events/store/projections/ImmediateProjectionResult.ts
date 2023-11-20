/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/fundamentals';

import { PropertyPath } from './PropertyPath';

export class ImmediateProjectionResult {

    @field(Object)
    model!: any;

    @field(PropertyPath, true)
    affectedProperties!: PropertyPath[];

    @field(Number)
    projectedEventsCount!: number;
}
