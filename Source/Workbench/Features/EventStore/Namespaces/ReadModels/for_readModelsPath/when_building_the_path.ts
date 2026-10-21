// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { readModelsPath } from '../readModelsPath';

describe('when building the read models path', () => {
    it('should be the page itself when nothing is selected', () => readModelsPath('System', 'Default').should.equal('/event-store/System/Default/read-models'));
    it('should add the read model', () => readModelsPath('System', 'Default', { readModel: 'Orders' }).should.equal('/event-store/System/Default/read-models/Orders'));
    it('should add the occurrence below the read model', () => readModelsPath('System', 'Default', { readModel: 'Orders', occurrence: '2' }).should.equal('/event-store/System/Default/read-models/Orders/2'));
    it('should add the instance below the occurrence', () => readModelsPath('System', 'Default', { readModel: 'Orders', occurrence: '2', instanceKey: 'abc' }).should.equal('/event-store/System/Default/read-models/Orders/2/abc'));
    it('should ignore an occurrence without a read model', () => readModelsPath('System', 'Default', { occurrence: '2' }).should.equal('/event-store/System/Default/read-models'));
    it('should never carry the prefix the Workbench is served under, which is the router\'s to add', () => readModelsPath('System', 'Default').startsWith('/event-store/').should.be.true);
});
