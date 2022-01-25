// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IObservableQueryFor, OnNextResult } from './IObservableQueryFor';
import Handlebars from 'handlebars';
import { ObservableQueryConnection } from './ObservableQueryConnection';
import { ObservableQuerySubscription } from './ObservableQuerySubscription';

/**
 * Represents an implementation of {@link IQueryFor}.
 * @template TDataType Type of data returned by the query.
 */
export abstract class ObservableQueryFor<TDataType, TArguments = {}> implements IObservableQueryFor<TDataType, TArguments> {
    abstract readonly route: string;
    abstract readonly routeTemplate: Handlebars.TemplateDelegate<any>;

    abstract readonly defaultValue: TDataType;
    abstract readonly requiresArguments: boolean;

    /** @inheritdoc */
    subscribe(callback: OnNextResult, args?: TArguments): ObservableQuerySubscription<TDataType> {
        let actualRoute = this.route;
        if (args && Object.keys(args).length > 0) {
            actualRoute = this.routeTemplate(args);
        }

        const connection = new ObservableQueryConnection<TDataType>(actualRoute);
        const subscriber = new ObservableQuerySubscription(connection);
        connection.connect(callback);
        return subscriber;
    }
}
