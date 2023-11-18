/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResultWithState, useQuery, PerformQuery } from '@aksio/applications/queries';
import { SuggestionInformation } from './SuggestionInformation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{{microserviceId}}/{{tenantId}}/suggestions');

export interface GetSuggestionsArguments {
    microserviceId: string;
    tenantId: string;
}
export class GetSuggestions extends QueryFor<SuggestionInformation[], GetSuggestionsArguments> {
    readonly route: string = '/api/events/store/{{microserviceId}}/{{tenantId}}/suggestions';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: SuggestionInformation[] = [];

    constructor() {
        super(SuggestionInformation, true);
    }

    get requestArguments(): string[] {
        return [
            'microserviceId',
            'tenantId',
        ];
    }

    static use(args?: GetSuggestionsArguments): [QueryResultWithState<SuggestionInformation[]>, PerformQuery<GetSuggestionsArguments>] {
        return useQuery<SuggestionInformation[], GetSuggestions, GetSuggestionsArguments>(GetSuggestions, args);
    }
}
