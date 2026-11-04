// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { AppendedEvents, AppendedEvent } from 'Features/Sequences';
import { SequenceEventDetailsViewModel } from '../SequenceEventDetailsViewModel';

const scope = { eventStore: 'store', namespace: 'namespace', eventSequenceId: 'custom/sequence' };
const event = (number: number) => ({ context: { sequenceNumber: number } }) as AppendedEvent;

for (const number of [0, 2147483647]) {
    describe(`when inspecting supported sequence number ${number}`, () => {
        let query: sinon.SinonStubbedInstance<AppendedEvents>;
        let vm: SequenceEventDetailsViewModel;
        beforeEach(async () => {
            query = sinon.createStubInstance(AppendedEvents);
            query.perform.resolves({ isSuccess: true, data: [event(number)], totalItems: 0 } as never);
            vm = new SequenceEventDetailsViewModel(query);
            await vm.handleProps({ ...scope, sequenceNumber: String(number) });
        });
        it('should request only the target page', () => query.paging.page.should.equal(number));
        it('should request exactly one event', () => query.paging.pageSize.should.equal(1));
        it('should preserve scope without an event source filter', () => query.perform.firstCall.args[0]!.should.deep.equal(scope));
        it('should expose the exact event regardless of totals', () => vm.event!.context.sequenceNumber.should.equal(number));
    });
}

for (const number of ['-1', '1.5', 'NaN', 'Infinity', '9007199254740992', '2147483648', '', '1e2']) {
    describe(`when inspecting unsupported number ${number}`, () => {
        let query: sinon.SinonStubbedInstance<AppendedEvents>;
        let vm: SequenceEventDetailsViewModel;
        beforeEach(async () => {
            query = sinon.createStubInstance(AppendedEvents);
            vm = new SequenceEventDetailsViewModel(query);
            await vm.handleProps({ ...scope, sequenceNumber: number });
        });
        it('should not query', () => query.perform.called.should.be.false);
        it('should report unavailable', () => vm.state.should.equal('unavailable'));
    });
}

for (const [result, state] of [
    [{ isSuccess: true, data: [] }, 'missing'],
    [{ isSuccess: true, data: [event(1)] }, 'missing'],
    [{ isSuccess: false, isAuthorized: false }, 'denied'],
    [{ isSuccess: false, isAuthorized: true, isValid: false }, 'failed'],
    [{ isSuccess: false, isAuthorized: true, hasExceptions: true }, 'failed']
] as const) {
    describe(`when exact event lookup reports ${state}`, () => {
        let vm: SequenceEventDetailsViewModel;
        beforeEach(async () => {
            const query = sinon.createStubInstance(AppendedEvents);
            query.perform.resolves(result as never);
            vm = new SequenceEventDetailsViewModel(query);
            await vm.handleProps({ ...scope, sequenceNumber: '0' });
        });
        it('should report the honest state', () => vm.state.should.equal(state));
        it('should not expose a nearby event', () => (vm.event === undefined).should.be.true);
    });
}

describe('when routes change during an exact event lookup', () => {
    let vm: SequenceEventDetailsViewModel;
    let query: sinon.SinonStubbedInstance<AppendedEvents>;
    let finish: (result: never) => void;
    beforeEach(async () => {
        query = sinon.createStubInstance(AppendedEvents);
        query.perform.onFirstCall().returns(new Promise(resolve => { finish = resolve; }));
        query.perform.onSecondCall().resolves({ isSuccess: true, data: [event(2)] } as never);
        vm = new SequenceEventDetailsViewModel(query);
        const first = vm.handleProps({ ...scope, sequenceNumber: '0' });
        vm.state.should.equal('loading');
        await vm.handleProps({ ...scope, sequenceNumber: '2' });
        finish({ isSuccess: true, data: [event(0)] } as never);
        await first;
    });
    it('should ignore the older response', () => vm.event!.context.sequenceNumber.should.equal(2));
    it('should deduplicate equivalent props', async () => {
        await vm.handleProps({ ...scope, sequenceNumber: '2' });
        query.perform.callCount.should.equal(2);
    });
});

describe('when an exact event lookup throws', () => {
    let vm: SequenceEventDetailsViewModel;
    beforeEach(async () => {
        const query = sinon.createStubInstance(AppendedEvents);
        query.perform.onFirstCall().rejects(new Error('offline'));
        query.perform.onSecondCall().resolves({ isSuccess: true, data: [event(0)] } as never);
        vm = new SequenceEventDetailsViewModel(query);
        await vm.handleProps({ ...scope, sequenceNumber: '0' });
    });
    it('should report failure', () => vm.state.should.equal('failed'));
    it('should allow an explicit retry', async () => {
        await vm.retry();
        vm.event!.context.sequenceNumber.should.equal(0);
    });
});
