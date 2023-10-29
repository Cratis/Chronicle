/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { ObservableQueryFor, QueryResultWithState, useObservableQuery } from '@aksio/applications/queries';
import { JobState } from './JobState';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/jobs/{{microserviceId}}/{{tenantId}}');

export interface AllJobsArguments {
    microserviceId: string;
    tenantId: string;
}
export class AllJobs extends ObservableQueryFor<JobState[], AllJobsArguments> {
    readonly route: string = '/api/jobs/{{microserviceId}}/{{tenantId}}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: JobState[] = [];

    constructor() {
        super(JobState, true);
    }

    get requestArguments(): string[] {
        return [
            'microserviceId',
            'tenantId',
        ];
    }

    static use(args?: AllJobsArguments): [QueryResultWithState<JobState[]>] {
        return useObservableQuery<JobState[], AllJobs, AllJobsArguments>(AllJobs, args);
    }
}
