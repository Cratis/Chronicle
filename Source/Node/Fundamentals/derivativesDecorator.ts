// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Constructor } from './Constructor';

export function derivatives(...derivatives: Constructor[]) {
    return function (target: any, propertyKey: string) {
        debugger;
    };
}
