/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from 'Infrastructure';

import { SerializableDateTimeOffset } from '../Primitives/SerializableDateTimeOffset';

export class Causation {

    @field(SerializableDateTimeOffset)
    occurred!: SerializableDateTimeOffset;

    @field(String)
    type!: string;

    @field(Object)
    properties!: any;
}
