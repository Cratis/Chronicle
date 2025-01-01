/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { field } from '@cratis/fundamentals';
import { JobStatus } from './JobStatus';
import { SerializableDateTimeOffset } from '../Primitives/SerializableDateTimeOffset';

export class JobStatusChanged {

    @field(Number)
    status!: JobStatus;

    @field(SerializableDateTimeOffset)
    occurred!: SerializableDateTimeOffset;

    @field(String, true)
    exceptionMessages!: string[];

    @field(String)
    exceptionStackTrace!: string;
}
