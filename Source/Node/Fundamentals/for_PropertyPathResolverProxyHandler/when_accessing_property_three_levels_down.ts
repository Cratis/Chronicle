// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { PropertyPathResolverProxyHandler } from '../PropertyPathResolverProxyHandler';

class ThirdLevelType {
    get value(): number {
        return 42;
    }
}

class SecondLevelType {
    get thirdLevel() {
        return new ThirdLevelType();
    }
}

class TopLevelType {
    secondLevel: SecondLevelType;

    constructor() {
        this.secondLevel = new SecondLevelType();
    }
}

describe('when accessing property three levels down', () => {
    const handler = new PropertyPathResolverProxyHandler();
    const proxy = new Proxy({}, handler);
    const accessor = (_: TopLevelType) => _.secondLevel.thirdLevel.value;
    accessor(proxy);

    it('should hold the entry property for the second level', () => handler.property.should.equal('secondLevel'));
    it('should hold a full path', () => handler.path.should.equal('secondLevel.thirdLevel.value'));
    it('should only have 3 segments', () => handler.segments.should.be.lengthOf(3));
    it('should hold the second level segment first', () => handler.segments[0].should.equal('secondLevel'));
    it('should hold the third level segment second', () => handler.segments[1].should.equal('thirdLevel'));
    it('should hold the value segment third', () => handler.segments[2].should.equal('value'));
});
