// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { buildViews } from '../graphViews';
import { organization } from './given/organization';

describe('when building views for a node alone', () => {
    const built = buildViews(organization, 'Person', 0)!;

    it('should build a single view', () => built.views.length.should.equal(1));
    it('should name the root view after the label', () => built.rootView.should.equal('Person'));
    it('should read every property of the node', () => built.views[0].members.map(_ => _.name).should.deep.equal(['personId', 'name']));
    it('should read no relation', () => built.views[0].members.some(_ => _.kind === 'relation').should.be.false);
    it('should offer the properties of the root for filtering', () => built.properties.should.deep.equal(['personId', 'name']));
    it('should know the key property of the root', () => built.keyProperty.should.equal('personId'));
});
