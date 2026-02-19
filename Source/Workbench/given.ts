// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Constructor } from '@cratis/fundamentals';
import { Suite } from 'mocha';

export type ContextForSuite<TContext extends object> = (this: Suite, context: TContext) => void;

export function given<TContext extends object>(contextType: Constructor<TContext>, callback: ContextForSuite<TContext>) {
    return function (this: Suite) {
        const context = new contextType(this);
        callback.call(this, context);
    };
}
