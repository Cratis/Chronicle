/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { field } from '@cratis/fundamentals';
import { Guid } from '@cratis/fundamentals';
import { JobProgress } from './JobProgress';
import { JobStatus } from './JobStatus';
import { JobStatusChanged } from './JobStatusChanged';
import { SerializableDateTimeOffset } from '../Primitives/SerializableDateTimeOffset';

export class Job {

    @field(Guid)
    id!: Guid;

    @field(String)
    details!: string;

    @field(String)
    type!: string;

    @field(Number)
    status!: JobStatus;

    @field(SerializableDateTimeOffset)
    created!: SerializableDateTimeOffset;

    @field(JobStatusChanged, true)
    statusChanges!: JobStatusChanged[];

    @field(JobProgress)
    progress!: JobProgress;
}
