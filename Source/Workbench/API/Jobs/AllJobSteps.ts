// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResultWithState, useQuery, PerformQuery } from 'Infrastructure/queries';
import { JobStepState } from './JobStepState';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{eventStore}/{namespace}/jobs/{jobId}/steps/{jobId}/steps');

export interface AllJobStepsArguments {
    eventStore: string;
    namespace: string;
    jobId: string;
}
export class AllJobSteps extends QueryFor<JobStepState[], AllJobStepsArguments> {
    readonly route: string = '/api/events/store/{eventStore}/{namespace}/jobs/{jobId}/steps/{jobId}/steps';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: JobStepState[] = [];

    constructor() {
        super(JobStepState, true);
    }

    get requestArguments(): string[] {
        return [
            'eventStore',
            'namespace',
            'jobId',
        ];
    }

    static use(args?: AllJobStepsArguments): [QueryResultWithState<JobStepState[]>, PerformQuery<AllJobStepsArguments>] {
        return useQuery<JobStepState[], AllJobSteps, AllJobStepsArguments>(AllJobSteps, args);
    }
}
