// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { a_harness, code, context } from './given/a_harness';

describe('when generating code', () => {
    let harness: a_harness;
    beforeEach(() => { harness = new a_harness(); });
    afterEach(() => harness.dispose());

    it('should dispatch immediately when running', () => {
        harness.generation.run(context('a'));
        harness.requests.length.should.equal(1);
    });

    it('should coalesce a typing burst into one dispatch of the latest declaration', () => {
        ['a', 'ab', 'abc'].forEach(text => { harness.generation.schedule(context(text)); harness.clock.tick(100); });
        harness.clock.tick(300);
        harness.requests.map(r => r.context.declaration).should.deep.equal(['abc']);
    });

    it('should not dispatch before the delay has passed', () => {
        harness.generation.schedule(context('a'));
        harness.clock.tick(299);
        harness.requests.length.should.equal(0);
    });

    it('should not dispatch again for an equivalent context object', () => {
        harness.generation.run(context('a'));
        harness.generation.schedule(context('a'));
        harness.clock.tick(1000);
        harness.requests.length.should.equal(1);
    });

    it('should drop a queued edit when a language change runs immediately', () => {
        harness.generation.schedule(context('a'));
        harness.generation.run(context('a', 'Kotlin'));
        harness.clock.tick(1000);
        harness.requests.map(r => r.context.language).should.deep.equal(['Kotlin']);
    });

    it('should dispatch again when refreshing the same context', () => {
        harness.generation.run(context('a'));
        harness.generation.run(context('a'));
        harness.requests.length.should.equal(2);
    });

    it('should hide the previous output when running', () => {
        harness.generation.run(context('a'));
        (harness.visible === null).should.be.true;
    });

    it('should publish the pair of the current request', async () => {
        harness.generation.run(context('a'));
        harness.requests[0].resolve(code('a'));
        await harness.settle();
        harness.visible!.should.deep.equal(code('a'));
    });

    it('should ignore the older outcome when outcomes arrive in reverse order', async () => {
        harness.generation.run(context('a'));
        harness.generation.run(context('a', 'Kotlin'));
        harness.requests[1].resolve(code('kotlin'));
        harness.requests[0].resolve(code('csharp'));
        await harness.settle();
        harness.visible!.should.deep.equal(code('kotlin'));
    });

    it('should ignore an outcome that arrives while a newer edit is waiting', async () => {
        harness.generation.run(context('a'));
        harness.generation.schedule(context('ab'));
        harness.requests[0].resolve(code('a'));
        await harness.settle();
        harness.published.filter(p => p !== null).length.should.equal(0);
    });

    it('should publish the failure text for a current failure', async () => {
        harness.generation.run(context('a'));
        harness.requests[0].reject(new Error('down'));
        await harness.settle();
        harness.visible!.should.deep.equal({ declarative: '// Unable to generate code', modelBound: '// Unable to generate code' });
    });

    it('should ignore a failure of an obsolete request', async () => {
        harness.generation.run(context('a'));
        harness.generation.run(context('b'));
        harness.requests[0].reject(new Error('down'));
        await harness.settle();
        (harness.visible === null).should.be.true;
    });

    it('should neither dispatch nor publish after being cleared', async () => {
        harness.generation.schedule(context('a'));
        harness.generation.run(context('b'));
        harness.generation.clear();
        harness.clock.tick(1000);
        harness.requests[0].resolve(code('b'));
        await harness.settle();
        (harness.visible === null && harness.requests.length === 1).should.be.true;
    });

    it('should not publish after being cancelled on unmount', async () => {
        harness.generation.run(context('a'));
        harness.generation.cancel();
        harness.requests[0].resolve(code('a'));
        await harness.settle();
        (harness.visible === null).should.be.true;
    });

    it('should survive a cancel and restart cycle', () => {
        harness.generation.run(context('a'));
        harness.generation.cancel();
        harness.generation.run(context('a'));
        harness.requests.length.should.equal(2);
    });
});
