// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { isObservableProp, makeAutoObservable } from 'mobx';
import { ComparisonPanel } from '../ComparisonPanel';
import { ComparisonViewModel } from '../ComparisonViewModel';

const memoType = Symbol.for('react.memo');

// The unit-spec environment has no DOM, so a re-render cannot be observed directly. These
// specs pin the two halves of the contract instead: withViewModel makes the view model
// observable, and the panel reads that state only through an observer wrapper.
describe('when the panel presents observable view model state', () => {
    const viewModel = makeAutoObservable(new ComparisonViewModel());
    it('should be an observer component', () => (ComparisonPanel as unknown as { $$typeof: symbol }).$$typeof.should.equal(memoType));
    it('should present state that is observable', () => isObservableProp(viewModel, 'isOpen').should.be.true);
    it('should present selection that is observable', () => isObservableProp(viewModel, 'leftKey').should.be.true);
});
