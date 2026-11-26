// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { a_harness, message } from './given/a_harness';

describe('when validating edits', () => {
    let harness: a_harness;
    beforeEach(() => { harness = new a_harness(); });
    afterEach(() => harness.dispose());

    it('should not request before the delay has passed', () => {
        harness.edit('capture A');
        harness.pass(499);
        harness.requests.length.should.equal(0);
    });

    it('should request the latest declaration once the delay has passed', () => {
        harness.edit('capture A');
        harness.pass(300);
        harness.edit('capture AB');
        harness.pass();
        harness.requests.map(r => r.snapshot.declaration).should.deep.equal(['capture AB']);
    });

    it('should publish the messages of the current request', async () => {
        harness.edit('capture A');
        harness.pass();
        harness.requests[0].resolve([message('first')]);
        await harness.settle();
        harness.published.should.deep.equal([[message('first')]]);
    });

    it('should ignore a response that arrives while the next edit is still waiting', async () => {
        harness.edit('capture A');
        harness.pass();
        harness.edit('capture AB');
        harness.requests[0].resolve([message('old')]);
        await harness.settle();
        harness.published.length.should.equal(0);
    });

    it('should ignore the older response when responses arrive in reverse order', async () => {
        harness.edit('capture A');
        harness.pass();
        harness.edit('capture AB');
        harness.pass();
        harness.requests[1].resolve([message('new')]);
        harness.requests[0].resolve([message('old')]);
        await harness.settle();
        harness.published.should.deep.equal([[message('new')]]);
    });

    it('should clear the messages for a blank declaration', () => {
        harness.edit('   ');
        harness.published.should.deep.equal([[]]);
    });

    it('should ignore a response that arrives after the declaration was cleared', async () => {
        harness.edit('capture A');
        harness.pass();
        harness.edit('');
        harness.requests[0].resolve([message('old')]);
        await harness.settle();
        harness.published.should.deep.equal([[]]);
    });

    it('should not request for a declaration that was cancelled while waiting', () => {
        harness.edit('capture A');
        harness.validation.cancel();
        harness.pass();
        harness.requests.length.should.equal(0);
    });

    it('should ignore a response that arrives after the selection changed', async () => {
        harness.edit('capture A');
        harness.pass();
        harness.validation.cancel();
        harness.requests[0].resolve([message('old')]);
        await harness.settle();
        harness.published.length.should.equal(0);
    });

    it('should request again for a new event store with the same declaration', () => {
        harness.edit('capture A', 'one');
        harness.pass();
        harness.edit('capture A', 'two');
        harness.pass();
        harness.requests.map(r => r.snapshot.eventStore).should.deep.equal(['one', 'two']);
    });

    it('should publish nothing for a request that did not succeed', async () => {
        harness.edit('capture A');
        harness.pass();
        harness.requests[0].resolve(null);
        await harness.settle();
        harness.published.length.should.equal(0);
    });

    it('should publish nothing for a request that failed', async () => {
        harness.edit('capture A');
        harness.pass();
        harness.requests[0].reject(new Error('down'));
        await harness.settle();
        harness.published.length.should.equal(0);
    });

    it('should keep working after being restarted by cancel and schedule', async () => {
        harness.edit('capture A');
        harness.validation.cancel();
        harness.edit('capture A');
        harness.pass();
        harness.requests.length.should.equal(1);
    });
});
