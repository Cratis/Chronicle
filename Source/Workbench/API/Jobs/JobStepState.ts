// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from 'Infrastructure';

import { JobStepStatus } from './JobStepStatus';
import { JobStepStatusChanged } from './JobStepStatusChanged';
import { JobStepProgress } from './JobStepProgress';

export class JobStepState {

    @field(String)
    id!: string;

    @field(String)
    type!: string;

    @field(String)
    name!: string;

    @field(JobStepStatus)
    status!: JobStepStatus;

    @field(JobStepStatusChanged, true)
    statusChanges!: JobStepStatusChanged[];

    @field(JobStepProgress)
    progress!: JobStepProgress;
}
