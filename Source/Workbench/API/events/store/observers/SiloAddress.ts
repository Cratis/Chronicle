/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/fundamentals';

import { IPEndPoint } from './IPEndPoint';

export class SiloAddress {

    @field(IPEndPoint)
    endpoint!: IPEndPoint;

    @field(Number)
    generation!: number;

    @field(Boolean)
    isClient!: boolean;
}
