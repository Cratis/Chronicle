/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/fundamentals';

import { FailedPartitionAttempt } from './FailedPartitionAttempt';

export class FailedPartition {

    @field(String)
    id!: string;

    @field(String)
    partition!: string;

    @field(String)
    observerId!: string;

    @field(FailedPartitionAttempt, true)
    attempts!: FailedPartitionAttempt[];

    @field(Boolean)
    isResolved!: boolean;
}
