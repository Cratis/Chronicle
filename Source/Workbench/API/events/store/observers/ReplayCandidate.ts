/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/fundamentals';

import { ObserverKey } from './ObserverKey';
import { ReplayCandidateReason } from './ReplayCandidateReason';

export class ReplayCandidate {

    @field(String)
    id!: string;

    @field(String)
    observerId!: string;

    @field(ObserverKey)
    observerKey!: ObserverKey;

    @field(ReplayCandidateReason, true)
    reasons!: ReplayCandidateReason[];

    @field(Date)
    occurred!: Date;
}
