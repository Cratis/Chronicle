/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

// eslint-disable-next-line header/header
import { field } from '@cratis/fundamentals';
import { Guid } from '@cratis/fundamentals';
import { Key } from '../Keys/Key';
import { ArrayIndexers } from '../Properties/ArrayIndexers';
import { ArrayIndexer } from '../Properties/ArrayIndexer';
import { PropertyPath } from '../Properties/PropertyPath';
import { IPropertyPathSegment } from '../Properties/IPropertyPathSegment';
import { FailedPartitionAttempt } from './FailedPartitionAttempt';

export class FailedPartition {

    @field(Guid)
    id!: Guid;

    @field(Key)
    partition!: Key;

    @field(String)
    observerId!: string;

    @field(FailedPartitionAttempt, true)
    attempts!: FailedPartitionAttempt[];

    @field(Boolean)
    isResolved!: boolean;

    @field(FailedPartitionAttempt)
    lastAttempt!: FailedPartitionAttempt;
}
