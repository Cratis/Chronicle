/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from 'Infrastructure';

import { EventTypeRegistration } from '../Users/einari/Projects/Cratis/Cratis/Source/Tools/ProxyGenerator/Cratis/Kernel/Contracts/Events/EventTypeRegistration';
import { EventType } from '../Users/einari/Projects/Cratis/Cratis/Source/Tools/ProxyGenerator/Cratis/Kernel/Contracts/Events/EventType';

export class RegisterEventTypes {

    @field(EventTypeRegistration, true)
    types!: EventTypeRegistration[];
}
