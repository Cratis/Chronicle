// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { PropertyPathResolverProxyHandler } from '../PropertyPathResolverProxyHandler';

class MyType {
    get value(): number {
        return 42;
    }
}

describe('when accessing property three levels down', () => {
    const handler = new PropertyPathResolverProxyHandler();
    const proxy = new Proxy({}, handler);
    const accessor = (_: MyType) => _.value;
    accessor(proxy);

    it('should hold the entry property for the second level', () => handler.property.should.equal('value'));
    it('should hold a full path', () => handler.path.should.equal('value'));
    it('should only have 1 segment', () => handler.segments.should.be.lengthOf(1));
    it('should hold the value segment third', () => handler.segments[0].should.equal('value'));
});
