// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { Guid, JsonSerializer } from '@cratis/fundamentals';
import { FailedPartitionDetails as FailedPartition, GetFailedPartitions, RetryPartition } from 'Features/Observation';
import { FailedPartitionsViewModel } from '../../FailedPartitionsViewModel';

const scope = { eventStore: 'store', namespace: 'namespace' };
const origin = JsonSerializer.deserialize(FailedPartition, JSON.stringify({ id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', observerId: 'observer-a', partition: 'partition-a', attempts: [], isResolved: false }));
const other = JsonSerializer.deserialize(FailedPartition, JSON.stringify({ id: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', observerId: 'observer-b', partition: 'partition-b', attempts: [], isResolved: false }));

// Use a separately parsed, differently cased URL identity, not object identity.
const originId = Guid.parse(origin.id.toString()).toString().toUpperCase();
const otherId = other.id.toString();

describe('when returning to an available originating failure', () => {
    let vm: FailedPartitionsViewModel;
    let query: sinon.SinonStubbedInstance<GetFailedPartitions>;
    beforeEach(async () => {
        query = sinon.createStubInstance(GetFailedPartitions);
        query.perform.resolves({ isSuccess: true, data: [other, origin] } as never);
        vm = new FailedPartitionsViewModel(scope, query);
        await vm.handleQueryParams({ failedPartitionId: originId });
    });
    it('should exercise a transport-deserialized Guid identity', () => origin.id.should.be.instanceOf(Guid));
    it('should restore the exact row by Guid value', () => vm.selectedFailedPartition!.should.equal(origin));
    it('should use the same scope', () => query.perform.firstCall.args[0]!.should.deep.equal(scope));
    it('should deduplicate repeated URL notifications', async () => {
        await vm.handleQueryParams({ failedPartitionId: originId });
        query.perform.calledOnce.should.be.true;
    });
    it('should permit a fresh restore for a new origin', async () => {
        await vm.handleQueryParams({ failedPartitionId: otherId });
        vm.selectedFailedPartition!.should.equal(other);
    });
    it('should permit a fresh restore for a new scope', async () => {
        await vm.handleParams({ ...scope, namespace: 'new' });
        query.perform.lastCall.args[0]!.namespace.should.equal('new');
    });
});

for (const choice of [other, null]) {
    describe(`when the user ${choice ? 'selects B' : 'clears selection'} during return lookup`, () => {
        let vm: FailedPartitionsViewModel;
        let query: sinon.SinonStubbedInstance<GetFailedPartitions>;
        let command: sinon.SinonStub;
        beforeEach(async () => {
            query = sinon.createStubInstance(GetFailedPartitions);
            let finish!: (value: never) => void;
            query.perform.returns(new Promise(resolve => { finish = resolve; }));
            command = sinon.stub(RetryPartition.prototype, 'execute').resolves({} as never);
            vm = new FailedPartitionsViewModel(scope, query);
            const pending = vm.handleQueryParams({ failedPartitionId: originId });
            vm.selectFailedPartition(choice);
            finish({ isSuccess: true, data: [origin] } as never);
            await pending;
            await vm.handleQueryParams({ failedPartitionId: originId });
            await vm.retryRestoration();
            await vm.retry();
        });
        afterEach(() => sinon.restore());
        it('should preserve the users selection', () => (vm.selectedFailedPartition === (choice ?? undefined)).should.be.true);
        it('should not restart a consumed restoration', () => query.perform.calledOnce.should.be.true);
        it('should preserve Retry targeting or leave it disabled', () => {
            command.callCount.should.equal(choice ? 1 : 0);
            if (choice) {
                const target = command.firstCall.thisValue as RetryPartition;
                target.observerId.should.equal('observer-b');
                target.partition.should.equal('partition-b');
                target.eventStore.should.equal('store');
                target.namespace.should.equal('namespace');
                target.eventSequenceId.should.equal('');
            }
        });
    });
}

describe('when no origin was provided', () => {
    let query: sinon.SinonStubbedInstance<GetFailedPartitions>;
    beforeEach(async () => {
        query = sinon.createStubInstance(GetFailedPartitions);
        const vm = new FailedPartitionsViewModel(scope, query);
        await vm.handleQueryParams({});
    });
    it('should not fetch a restoration snapshot', () => query.perform.called.should.be.false);
});

for (const [result, state] of [
    [{ isSuccess: true, data: [other] }, 'missing'],
    [{ isSuccess: false, isAuthorized: false }, 'denied'],
    [{ isSuccess: false, isAuthorized: true, isValid: false }, 'failed']
] as const) {
    describe(`when return lookup is ${state}`, () => {
        let vm: FailedPartitionsViewModel;
        beforeEach(async () => {
            const query = sinon.createStubInstance(GetFailedPartitions);
            query.perform.resolves(result as never);
            vm = new FailedPartitionsViewModel(scope, query);
            await vm.handleQueryParams({ failedPartitionId: originId });
        });
        it('should report the lookup state', () => vm.restorationState.should.equal(state));
        it('should not select another row', () => (vm.selectedFailedPartition === undefined).should.be.true);
    });
}

describe('when the origin changes during return lookup', () => {
    let vm: FailedPartitionsViewModel;
    beforeEach(async () => {
        const query = sinon.createStubInstance(GetFailedPartitions);
        let finish!: (value: never) => void;
        query.perform.onFirstCall().returns(new Promise(resolve => { finish = resolve; }));
        query.perform.onSecondCall().resolves({ isSuccess: true, data: [other] } as never);
        vm = new FailedPartitionsViewModel(scope, query);
        const pending = vm.handleQueryParams({ failedPartitionId: originId });
        await vm.handleQueryParams({ failedPartitionId: otherId });
        finish({ isSuccess: true, data: [origin] } as never);
        await pending;
    });
    it('should discard the older origin response', () => vm.selectedFailedPartition!.should.equal(other));
});

describe('when return lookup throws', () => {
    let vm: FailedPartitionsViewModel;
    beforeEach(async () => {
        const query = sinon.createStubInstance(GetFailedPartitions);
        query.perform.onFirstCall().rejects(new Error('offline'));
        query.perform.onSecondCall().resolves({ isSuccess: true, data: [origin] } as never);
        vm = new FailedPartitionsViewModel(scope, query);
        await vm.handleQueryParams({ failedPartitionId: originId });
    });
    it('should report failure', () => vm.restorationState.should.equal('failed'));
    it('should permit an explicit retry', async () => {
        await vm.retryRestoration();
        vm.selectedFailedPartition!.should.equal(origin);
    });
});
