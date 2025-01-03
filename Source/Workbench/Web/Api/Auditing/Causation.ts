/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { field } from '@cratis/fundamentals';
import { SerializableDateTimeOffset } from '../Primitives/SerializableDateTimeOffset';

export class Causation {

    @field(SerializableDateTimeOffset)
    occurred!: SerializableDateTimeOffset;

    @field(String)
    type!: string;

    @field(Object)
    properties!: any;
}
