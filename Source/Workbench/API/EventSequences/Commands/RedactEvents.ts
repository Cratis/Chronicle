/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from 'Infrastructure';

import { Causation } from '../../Auditing/Causation';
import { Identity } from '../../Identities/Identity';

export class RedactEvents {

    @field(String)
    eventSourceId!: string;

    @field(String)
    reason!: string;

    @field(String, true)
    eventTypes!: string[];

    @field(Causation, true)
    causation!: Causation[];

    @field(Identity)
    causedBy!: Identity;
}
