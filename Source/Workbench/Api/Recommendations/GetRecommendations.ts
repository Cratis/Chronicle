/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

// eslint-disable-next-line header/header
import { QueryFor, QueryResultWithState, Sorting, SortingActions, SortingActionsForQuery, Paging } from '@cratis/applications/queries';
import { useQuery, useQueryWithPaging, PerformQuery, SetSorting, SetPage, SetPageSize } from '@cratis/applications.react/queries';
import { RecommendationInformation } from '../Concepts/Recommendations/RecommendationInformation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/event-store/{{eventStore}}/{{namespace}}/recommendations');

class GetRecommendationsSortBy {
    private _id: SortingActionsForQuery<RecommendationInformation[]>;
    private _name: SortingActionsForQuery<RecommendationInformation[]>;
    private _description: SortingActionsForQuery<RecommendationInformation[]>;
    private _type: SortingActionsForQuery<RecommendationInformation[]>;
    private _occurred: SortingActionsForQuery<RecommendationInformation[]>;

    constructor(readonly query: GetRecommendations) {
        this._id = new SortingActionsForQuery<RecommendationInformation[]>('id', query);
        this._name = new SortingActionsForQuery<RecommendationInformation[]>('name', query);
        this._description = new SortingActionsForQuery<RecommendationInformation[]>('description', query);
        this._type = new SortingActionsForQuery<RecommendationInformation[]>('type', query);
        this._occurred = new SortingActionsForQuery<RecommendationInformation[]>('occurred', query);
    }

    get id(): SortingActionsForQuery<RecommendationInformation[]> {
        return this._id;
    }
    get name(): SortingActionsForQuery<RecommendationInformation[]> {
        return this._name;
    }
    get description(): SortingActionsForQuery<RecommendationInformation[]> {
        return this._description;
    }
    get type(): SortingActionsForQuery<RecommendationInformation[]> {
        return this._type;
    }
    get occurred(): SortingActionsForQuery<RecommendationInformation[]> {
        return this._occurred;
    }
}

class GetRecommendationsSortByWithoutQuery {
    private _id: SortingActions  = new SortingActions('id');
    private _name: SortingActions  = new SortingActions('name');
    private _description: SortingActions  = new SortingActions('description');
    private _type: SortingActions  = new SortingActions('type');
    private _occurred: SortingActions  = new SortingActions('occurred');

    get id(): SortingActions {
        return this._id;
    }
    get name(): SortingActions {
        return this._name;
    }
    get description(): SortingActions {
        return this._description;
    }
    get type(): SortingActions {
        return this._type;
    }
    get occurred(): SortingActions {
        return this._occurred;
    }
}

export interface GetRecommendationsArguments {
    eventStore: string;
    namespace: string;
}

export class GetRecommendations extends QueryFor<RecommendationInformation[], GetRecommendationsArguments> {
    readonly route: string = '/api/event-store/{eventStore}/{namespace}/recommendations';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: RecommendationInformation[] = [];
    private readonly _sortBy: GetRecommendationsSortBy;
    private static readonly _sortBy: GetRecommendationsSortByWithoutQuery = new GetRecommendationsSortByWithoutQuery();

    constructor() {
        super(RecommendationInformation, true);
        this._sortBy = new GetRecommendationsSortBy(this);
    }

    get requestArguments(): string[] {
        return [
            'eventStore',
            'namespace',
        ];
    }

    get sortBy(): GetRecommendationsSortBy {
        return this._sortBy;
    }

    static get sortBy(): GetRecommendationsSortByWithoutQuery {
        return this._sortBy;
    }

    static use(args?: GetRecommendationsArguments, sorting?: Sorting): [QueryResultWithState<RecommendationInformation[]>, PerformQuery<GetRecommendationsArguments>, SetSorting] {
        return useQuery<RecommendationInformation[], GetRecommendations, GetRecommendationsArguments>(GetRecommendations, args, sorting);
    }

    static useWithPaging(pageSize: number, args?: GetRecommendationsArguments, sorting?: Sorting): [QueryResultWithState<RecommendationInformation[]>, PerformQuery, SetSorting, SetPage, SetPageSize] {
        return useQueryWithPaging<RecommendationInformation[], GetRecommendations>(GetRecommendations, new Paging(0, pageSize), args, sorting);
    }
}
