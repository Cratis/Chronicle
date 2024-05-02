// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from 'Infrastructure';

import { JobStatus } from './JobStatus';
import { JobStatusChanged } from './JobStatusChanged';
import { JobProgress } from './JobProgress';

export class JobState {

    @field(String)
    id!: string;

    @field(String)
    type!: string;

    @field(String)
    name!: string;

    @field(String)
    details!: string;

    @field(JobStatus)
    status!: JobStatus;

    @field(JobStatusChanged, true)
    statusChanges!: JobStatusChanged[];

    @field(JobProgress)
    progress!: JobProgress;
}
