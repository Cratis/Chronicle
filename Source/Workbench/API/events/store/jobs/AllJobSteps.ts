/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { ObservableQueryFor, QueryResultWithState, useObservableQuery } from '@aksio/applications/queries';
import { JobStepState } from './JobStepState';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{{microserviceId}}/{{tenantId}}/jobs/{{jobId}}/steps');

export interface AllJobStepsArguments {
    jobId: string;
    microserviceId: string;
    tenantId: string;
}
export class AllJobSteps extends ObservableQueryFor<JobStepState[], AllJobStepsArguments> {
    readonly route: string = '/api/events/store/{{microserviceId}}/{{tenantId}}/jobs/{{jobId}}/steps';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: JobStepState[] = [];

    constructor() {
        super(JobStepState, true);
    }

    get requestArguments(): string[] {
        return [
            'jobId',
            'microserviceId',
            'tenantId',
        ];
    }

    static use(args?: AllJobStepsArguments): [QueryResultWithState<JobStepState[]>] {
        return useObservableQuery<JobStepState[], AllJobSteps, AllJobStepsArguments>(AllJobSteps, args);
    }
}
