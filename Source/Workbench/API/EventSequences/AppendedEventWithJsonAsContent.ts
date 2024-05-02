/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from 'Infrastructure';

import { EventMetadata } from '../Users/einari/Projects/Cratis/Cratis/Source/Tools/ProxyGenerator/Cratis/Kernel/Contracts/Events/EventMetadata';
import { EventType } from '../Users/einari/Projects/Cratis/Cratis/Source/Tools/ProxyGenerator/Cratis/Kernel/Contracts/Events/EventType';
import { EventContext } from '../Users/einari/Projects/Cratis/Cratis/Source/Tools/ProxyGenerator/Cratis/Kernel/Contracts/Events/EventContext';
import { SerializableDateTimeOffset } from '../Users/einari/Projects/Cratis/Cratis/Source/Tools/ProxyGenerator/Cratis/Kernel/Contracts/Primitives/SerializableDateTimeOffset';
import { Causation } from '../Users/einari/Projects/Cratis/Cratis/Source/Tools/ProxyGenerator/Cratis/Kernel/Contracts/Auditing/Causation';
import { Identity } from '../Users/einari/Projects/Cratis/Cratis/Source/Tools/ProxyGenerator/Cratis/Kernel/Contracts/Identities/Identity';
import { EventObservationState } from '../Users/einari/Projects/Cratis/Cratis/Source/Tools/ProxyGenerator/Cratis/Kernel/Contracts/Events/EventObservationState';

export class AppendedEventWithJsonAsContent {

    @field(EventMetadata)
    metadata!: EventMetadata;

    @field(EventContext)
    context!: EventContext;

    @field(Object)
    content!: any;
}
