/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { field } from '@cratis/fundamentals';
import { Guid } from '@cratis/fundamentals';
import { SerializableDateTimeOffset } from '../Primitives/SerializableDateTimeOffset';

export class Recommendation {

    @field(Guid)
    id!: Guid;

    @field(String)
    name!: string;

    @field(String)
    description!: string;

    @field(String)
    type!: string;

    @field(SerializableDateTimeOffset)
    occurred!: SerializableDateTimeOffset;
}
