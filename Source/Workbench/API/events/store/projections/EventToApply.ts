/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

<<<<<<<< HEAD:Source/Workbench/API/events/store/jobs/JobStatus.ts
export enum JobStatus {
    none = 0,
    preparing = 1,
    preparingSteps = 2,
    running = 3,
    completedSuccessfully = 4,
    completedWithFailures = 5,
    paused = 6,
    stopped = 7,
========
import { field } from '@aksio/fundamentals';

import { EventType } from './EventType';

export class EventToApply {

    @field(EventType)
    eventType!: EventType;

    @field(Object)
    content!: any;
>>>>>>>> main:Source/Workbench/API/events/store/projections/EventToApply.ts
}
