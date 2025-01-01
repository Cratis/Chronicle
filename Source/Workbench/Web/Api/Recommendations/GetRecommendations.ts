/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { QueryFor, QueryResultWithState, Sorting, SortingActions, SortingActionsForQuery, Paging } from '@cratis/applications/queries';
import { useQuery, useQueryWithPaging, PerformQuery, SetSorting, SetPage, SetPageSize } from '@cratis/applications.react/queries';
import { Recommendation } from '../Contracts/Recommendations/Recommendation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/event-store/{{eventStore}}/{{namespace}}/recommendations');

class GetRecommendationsSortBy {
    private _id: SortingActionsForQuery<Recommendation[]>;
    private _name: SortingActionsForQuery<Recommendation[]>;
    private _description: SortingActionsForQuery<Recommendation[]>;
    private _type: SortingActionsForQuery<Recommendation[]>;
    private _occurred: SortingActionsForQuery<Recommendation[]>;

    constructor(readonly query: GetRecommendations) {
        this._id = new SortingActionsForQuery<Recommendation[]>('id', query);
        this._name = new SortingActionsForQuery<Recommendation[]>('name', query);
        this._description = new SortingActionsForQuery<Recommendation[]>('description', query);
        this._type = new SortingActionsForQuery<Recommendation[]>('type', query);
        this._occurred = new SortingActionsForQuery<Recommendation[]>('occurred', query);
    }

    get id(): SortingActionsForQuery<Recommendation[]> {
        return this._id;
    }
    get name(): SortingActionsForQuery<Recommendation[]> {
        return this._name;
    }
    get description(): SortingActionsForQuery<Recommendation[]> {
        return this._description;
    }
    get type(): SortingActionsForQuery<Recommendation[]> {
        return this._type;
    }
    get occurred(): SortingActionsForQuery<Recommendation[]> {
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

export class GetRecommendations extends QueryFor<Recommendation[], GetRecommendationsArguments> {
    readonly route: string = '/api/event-store/{eventStore}/{namespace}/recommendations';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: Recommendation[] = [];
    private readonly _sortBy: GetRecommendationsSortBy;
    private static readonly _sortBy: GetRecommendationsSortByWithoutQuery = new GetRecommendationsSortByWithoutQuery();

    constructor() {
        super(Recommendation, true);
        this._sortBy = new GetRecommendationsSortBy(this);
    }

    get requiredRequestArguments(): string[] {
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

    static use(args?: GetRecommendationsArguments, sorting?: Sorting): [QueryResultWithState<Recommendation[]>, PerformQuery<GetRecommendationsArguments>, SetSorting] {
        return useQuery<Recommendation[], GetRecommendations, GetRecommendationsArguments>(GetRecommendations, args, sorting);
    }

    static useWithPaging(pageSize: number, args?: GetRecommendationsArguments, sorting?: Sorting): [QueryResultWithState<Recommendation[]>, PerformQuery, SetSorting, SetPage, SetPageSize] {
        return useQueryWithPaging<Recommendation[], GetRecommendations>(GetRecommendations, new Paging(0, pageSize), args, sorting);
    }
}
