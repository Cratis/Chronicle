// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from 'Infrastructure';

import { JobStatus } from './JobStatus';

export class JobStatusChanged {

    @field(JobStatus)
    status!: JobStatus;

    @field(Date)
    occurred!: Date;

    @field(String, true)
    exceptionMessages!: string[];

    @field(String)
    exceptionStackTrace!: string;
}
