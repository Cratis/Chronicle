/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/fundamentals';

import { Key } from './Key';
import { FailedPartitionAttempt } from './FailedPartitionAttempt';

export class FailedPartition {

    @field(String)
    id!: string;

    @field(Key)
    partition!: Key;

    @field(String)
    observerId!: string;

    @field(FailedPartitionAttempt, true)
    attempts!: FailedPartitionAttempt[];

    @field(Boolean)
    isResolved!: boolean;

    @field(FailedPartitionAttempt)
    lastAttempt?: FailedPartitionAttempt;
}
