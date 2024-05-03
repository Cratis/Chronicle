// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export type UrlChangedCallback = (url: string, previousUrl: string) => void;

export abstract class INavigation {
    abstract onUrlChanged(callback: UrlChangedCallback): void;
}

