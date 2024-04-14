/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from 'Infrastructure';

import { Key } from '../Keys/Key';
import { ArrayIndexers } from '../../Properties/ArrayIndexers';
import { ArrayIndexer } from '../../Properties/ArrayIndexer';
import { PropertyPath } from '../../Properties/PropertyPath';
import { IPropertyPathSegment } from '../../Properties/IPropertyPathSegment';
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
    lastAttempt!: FailedPartitionAttempt;
}
