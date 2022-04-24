// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IObservableQueryFor, OnNextResult } from './IObservableQueryFor';
import Handlebars from 'handlebars';
import { ObservableQueryConnection } from './ObservableQueryConnection';
import { ObservableQuerySubscription } from './ObservableQuerySubscription';
import { ValidateRequestArguments } from './ValidateRequestArguments';
import { IObservableQueryConnection } from './IObservableQueryConnection';
import { NullObservableQueryConnection } from './NullObservableQueryConnection';

/**
 * Represents an implementation of {@link IQueryFor}.
 * @template TDataType Type of data returned by the query.
 */
export abstract class ObservableQueryFor<TDataType, TArguments = {}> implements IObservableQueryFor<TDataType, TArguments> {
    abstract readonly route: string;
    abstract readonly routeTemplate: Handlebars.TemplateDelegate<any>;
    abstract readonly defaultValue: TDataType;
    abstract get requestArguments(): string[];

    /** @inheritdoc */
    subscribe(callback: OnNextResult, args?: TArguments): ObservableQuerySubscription<TDataType> {
        let actualRoute = this.route;
        let connection: IObservableQueryConnection<TDataType>;

        if (!ValidateRequestArguments(this.constructor.name, this.requestArguments, args)) {
            connection = new NullObservableQueryConnection(this.defaultValue);
        } else {
            actualRoute = this.routeTemplate(args);
            connection = new ObservableQueryConnection<TDataType>(actualRoute);
        }

        const subscriber = new ObservableQuerySubscription(connection);
        connection.connect(callback);
        return subscriber;
    }
}
