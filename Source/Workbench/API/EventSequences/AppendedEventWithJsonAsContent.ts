// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from 'Infrastructure';

import { EventMetadata } from '../Cratis/Kernel/Contracts/Events/EventMetadata';
import { EventType } from '../Cratis/Kernel/Contracts/Events/EventType';
import { EventContext } from '../Cratis/Kernel/Contracts/Events/EventContext';
import { SerializableDateTimeOffset } from '../Cratis/Kernel/Contracts/Primitives/SerializableDateTimeOffset';
import { Causation } from '../Cratis/Kernel/Contracts/Auditing/Causation';
import { Identity } from '../Cratis/Kernel/Contracts/Identities/Identity';
import { EventObservationState } from '../Cratis/Kernel/Contracts/Events/EventObservationState';

export class AppendedEventWithJsonAsContent {

    @field(EventMetadata)
    metadata!: EventMetadata;

    @field(EventContext)
    context!: EventContext;

    @field(Object)
    content!: any;
}
