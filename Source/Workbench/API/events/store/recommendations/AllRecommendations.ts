/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { ObservableQueryFor, QueryResultWithState, useObservableQuery } from '@aksio/applications/queries';
import { RecommendationInformation } from './RecommendationInformation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{{microserviceId}}/{{tenantId}}/recommendations/observe');

export interface AllRecommendationsArguments {
    microserviceId: string;
    tenantId: string;
}
export class AllRecommendations extends ObservableQueryFor<RecommendationInformation[], AllRecommendationsArguments> {
    readonly route: string = '/api/events/store/{{microserviceId}}/{{tenantId}}/recommendations/observe';
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

    static use(args?: AllRecommendationsArguments): [QueryResultWithState<RecommendationInformation[]>] {
        return useObservableQuery<RecommendationInformation[], AllRecommendations, AllRecommendationsArguments>(AllRecommendations, args);
    }
}
