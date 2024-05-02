// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
