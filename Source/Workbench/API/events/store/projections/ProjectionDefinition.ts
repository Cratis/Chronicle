/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/cratis-fundamentals';

import { ModelDefinition } from './ModelDefinition';
import { JsonObject } from './JsonObject';
import { EventType } from './EventType';
import { PropertyPath } from './PropertyPath';
import { AllDefinition } from './AllDefinition';
import { RemovedWithDefinition } from './RemovedWithDefinition';

export class ProjectionDefinition {

    @field(String)
    identifier!: string;

    @field(String)
    name!: string;

    @field(ModelDefinition)
    model!: ModelDefinition;

    @field(Boolean)
    isRewindable!: boolean;

    @field(JsonObject, true)
    initialModelState!: JsonObject[];

    @field(EventType, true)
    from!: EventType[];

    @field(EventType, true)
    join!: EventType[];

    @field(PropertyPath, true)
    children!: PropertyPath[];

    @field(AllDefinition)
    all!: AllDefinition;

    @field(RemovedWithDefinition)
    removedWith?: RemovedWithDefinition;
}
