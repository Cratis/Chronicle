// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResultWithState, useQuery, PerformQuery } from 'Infrastructure/queries';
import { RecommendationInformation } from '../Cratis/Recommendations/RecommendationInformation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{eventStore}/{namespace}/recommendations');

export interface GetRecommendationsArguments {
    eventStore: string;
    namespace: string;
}
export class GetRecommendations extends QueryFor<RecommendationInformation[], GetRecommendationsArguments> {
    readonly route: string = '/api/events/store/{eventStore}/{namespace}/recommendations';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: RecommendationInformation[] = [];

    constructor() {
        super(RecommendationInformation, true);
    }

    get requestArguments(): string[] {
        return [
            'eventStore',
            'namespace',
        ];
    }

    static use(args?: GetRecommendationsArguments): [QueryResultWithState<RecommendationInformation[]>, PerformQuery<GetRecommendationsArguments>] {
        return useQuery<RecommendationInformation[], GetRecommendations, GetRecommendationsArguments>(GetRecommendations, args);
    }
}
