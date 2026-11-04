// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { Guid, JsonSerializer } from '@cratis/fundamentals';
import { AllObservers, FailedPartitionDetails as FailedPartition } from 'Features/Observation';
import { FailedPartitionDetailsViewModel } from '../FailedPartitionDetailsViewModel';

const scope = { eventStore: 'store', namespace: 'namespace' };
const item = JsonSerializer.deserialize(FailedPartition, JSON.stringify({ id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', observerId: 'observer', partition: 'not-an-event-source', attempts: [], isResolved: false }));

describe('when resolving a failed attempts observer', () => {
    let query: sinon.SinonStubbedInstance<AllObservers>;
    let vm: FailedPartitionDetailsViewModel;
    beforeEach(async () => {
        query = sinon.createStubInstance(AllObservers);
        query.perform.resolves({ isSuccess: true, data: [{ id: 'observer', eventSequenceId: 'custom/sequence' }] } as never);
        vm = new FailedPartitionDetailsViewModel(query);
        await vm.handleProps({ ...scope, item });
    });
    it('should exercise a transport-deserialized Guid identity', () => item.id.should.be.instanceOf(Guid));
    it('should query the same scope', () => query.perform.firstCall.args[0]!.should.deep.equal(scope));
    it('should use the actual observer sequence and zero', () => vm.eventPath(0)!.should.contain('event/0?eventSequenceId=custom%2Fsequence'));
    it('should use each attempt number independently', () => vm.eventPath(7)!.should.contain('event/7?'));
    it('should retain only the origin id for returning', () => new URL(vm.eventPath(0)!, 'https://unit.invalid').searchParams.get('failedPartitionId')!.should.equal(item.id.toString()));
    it('should reject unsupported attempt numbers', () => (vm.eventPath(2147483648) === undefined).should.be.true);
    it('should deduplicate equivalent props', async () => {
        await vm.handleProps({ ...scope, item });
        query.perform.calledOnce.should.be.true;
    });
});

for (const [result, state] of [
    [{ isSuccess: true, data: [] }, 'missing'],
    [{ isSuccess: true, data: [{ id: 'observer', eventSequenceId: '' }] }, 'missing'],
    [{ isSuccess: false, isAuthorized: false }, 'denied'],
    [{ isSuccess: false, isAuthorized: true }, 'failed']
] as const) {
    describe(`when the observer lookup is ${state}`, () => {
        let vm: FailedPartitionDetailsViewModel;
        beforeEach(async () => {
            const query = sinon.createStubInstance(AllObservers);
            query.perform.resolves(result as never);
            vm = new FailedPartitionDetailsViewModel(query);
            await vm.handleProps({ ...scope, item });
        });
        it('should report the lookup state', () => vm.state.should.equal(state));
        it('should not guess an event log link', () => (vm.eventPath(0) === undefined).should.be.true);
    });
}

describe('when observer lookup throws', () => {
    let vm: FailedPartitionDetailsViewModel;
    beforeEach(async () => {
        const query = sinon.createStubInstance(AllObservers);
        query.perform.rejects(new Error('offline'));
        vm = new FailedPartitionDetailsViewModel(query);
        await vm.handleProps({ ...scope, item });
    });
    it('should report failed', () => vm.state.should.equal('failed'));
    it('should not expose a link', () => (vm.eventPath(0) === undefined).should.be.true);
});

describe('when the selected failure changes during observer lookup', () => {
    let vm: FailedPartitionDetailsViewModel;
    beforeEach(async () => {
        const query = sinon.createStubInstance(AllObservers);
        let finish!: (value: never) => void;
        query.perform.onFirstCall().returns(new Promise(resolve => { finish = resolve; }));
        query.perform.onSecondCall().resolves({ isSuccess: true, data: [] } as never);
        vm = new FailedPartitionDetailsViewModel(query);
        const first = vm.handleProps({ ...scope, item });
        await vm.handleProps({ ...scope, namespace: 'other', item: { ...item, id: Guid.parse('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb') } });
        finish({ isSuccess: true, data: [{ id: 'observer', eventSequenceId: 'old' }] } as never);
        await first;
    });
    it('should discard the stale observer', () => (vm.eventPath(0) === undefined).should.be.true);
});
