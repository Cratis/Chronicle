// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Object proxy handler
 * @template TObject
 */
export class PropertyPathResolverProxyHandler implements ProxyHandler<any> {
    _property = '';
    _segments: string[] = [];

    constructor(private readonly _root?: PropertyPathResolverProxyHandler) {
    }

    get property(): string {
        return this._property;
    }

    get segments(): readonly string[] {
        return this._segments;
    }

    get path(): string {
        return this._segments.join('.');
    }

    private addSegment(segment: string) {
        this._segments.push(segment);
    }

    /** @inheritdoc */
    get(target: any, p: PropertyKey, receiver: any): any {
        const root = this._root || this;

        const childProperty = new Proxy({}, new PropertyPathResolverProxyHandler(root));
        this._property = p.toString();
        root.addSegment(this._property);

        return childProperty;
    }
}
