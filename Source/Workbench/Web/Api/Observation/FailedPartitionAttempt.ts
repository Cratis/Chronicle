/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { field } from '@cratis/fundamentals';
import { SerializableDateTimeOffset } from '../Primitives/SerializableDateTimeOffset';

export class FailedPartitionAttempt {

    @field(SerializableDateTimeOffset)
    occurred!: SerializableDateTimeOffset;

    @field(Number)
    sequenceNumber!: number;

    @field(String, true)
    messages!: string[];

    @field(String)
    stackTrace!: string;
}
