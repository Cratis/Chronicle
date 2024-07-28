// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { INavigation, UrlChangedCallback } from './INavigation';

export class Navigation implements INavigation {
    private readonly _callbacks: UrlChangedCallback[] = [];

    constructor() {
        const previousUrl = location.href;
        const observer = new MutationObserver((mutations) => {
            if (location.href !== previousUrl) {
                for( const callback of this._callbacks ) {
                    callback(location.href, previousUrl);
                }
            }
        });
        observer.observe(document.body,
            {
                childList: true,
                subtree: true
            }
        );
    }

    onUrlChanged(callback: UrlChangedCallback): void {
        this._callbacks.push(callback);
    }
}
