// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResultWithState, useQuery, PerformQuery } from 'Infrastructure/queries';
import { JobState } from './JobState';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{eventStore}/{namespace}/jobs');

export interface AllJobsArguments {
    eventStore: string;
    namespace: string;
}
export class AllJobs extends QueryFor<JobState[], AllJobsArguments> {
    readonly route: string = '/api/events/store/{eventStore}/{namespace}/jobs';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: JobState[] = [];

    constructor() {
        super(JobState, true);
    }

    get requestArguments(): string[] {
        return [
            'eventStore',
            'namespace',
        ];
    }

    static use(args?: AllJobsArguments): [QueryResultWithState<JobState[]>, PerformQuery<AllJobsArguments>] {
        return useQuery<JobState[], AllJobs, AllJobsArguments>(AllJobs, args);
    }
}
