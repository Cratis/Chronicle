/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/fundamentals';

import { AddressFamily } from './AddressFamily';

export class IPEndPoint {

    @field(Number)
    address!: number;

    @field(Number)
    port!: number;

    @field(Number)
    addressFamily!: AddressFamily;
}
