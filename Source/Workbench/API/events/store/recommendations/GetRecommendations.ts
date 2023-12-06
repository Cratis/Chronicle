/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResultWithState, useQuery, PerformQuery } from '@aksio/applications/queries';
import { RecommendationInformation } from './RecommendationInformation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{{microserviceId}}/{{tenantId}}/recommendations');

export interface GetRecommendationsArguments {
    microserviceId: string;
    tenantId: string;
}
export class GetRecommendations extends QueryFor<RecommendationInformation[], GetRecommendationsArguments> {
    readonly route: string = '/api/events/store/{{microserviceId}}/{{tenantId}}/recommendations';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: RecommendationInformation[] = [];

    constructor() {
        super(RecommendationInformation, true);
    }

    get requestArguments(): string[] {
        return [
            'microserviceId',
            'tenantId',
        ];
    }

    static use(args?: GetRecommendationsArguments): [QueryResultWithState<RecommendationInformation[]>, PerformQuery<GetRecommendationsArguments>] {
        return useQuery<RecommendationInformation[], GetRecommendations, GetRecommendationsArguments>(GetRecommendations, args);
    }
}
