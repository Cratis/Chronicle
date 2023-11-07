/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResultWithState, useQuery, PerformQuery } from '@aksio/applications/queries';
import { ReplayCandidate } from './ReplayCandidate';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{{microserviceId}}/{{tenantId}}/observers/replay-candidates');

export interface AllReplayCandidatesArguments {
    microserviceId: string;
    tenantId: string;
}
export class AllReplayCandidates extends QueryFor<ReplayCandidate[], AllReplayCandidatesArguments> {
    readonly route: string = '/api/events/store/{{microserviceId}}/{{tenantId}}/observers/replay-candidates';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: ReplayCandidate[] = [];

    constructor() {
        super(ReplayCandidate, true);
    }

    get requestArguments(): string[] {
        return [
            'microserviceId',
            'tenantId',
        ];
    }

    static use(args?: AllReplayCandidatesArguments): [QueryResultWithState<ReplayCandidate[]>, PerformQuery<AllReplayCandidatesArguments>] {
        return useQuery<ReplayCandidate[], AllReplayCandidates, AllReplayCandidatesArguments>(AllReplayCandidates, args);
    }
}
