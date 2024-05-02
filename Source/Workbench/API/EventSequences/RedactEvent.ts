/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from 'Infrastructure';

import { Causation } from '../Auditing/Causation';
import { Identity } from '../Identities/Identity';

export class RedactEvent {

    @field(Number)
    sequenceNumber!: number;

    @field(String)
    reason!: string;

    @field(Causation, true)
    causation!: Causation[];

    @field(Identity)
    causedBy!: Identity;
}
