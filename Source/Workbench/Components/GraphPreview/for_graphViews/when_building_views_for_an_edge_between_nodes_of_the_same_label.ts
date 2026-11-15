// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { buildViews } from '../graphViews';
import { organization } from './given/organization';

describe('when building views for an edge between nodes of the same label', () => {
    const built = buildViews(organization, 'Group', 1)!;
    const relations = built.views[0].members.filter(_ => _.kind === 'relation');

    it('should follow the edge in both directions', () => relations.filter(_ => _.edge === 'PART_OF').map(_ => _.direction).should.deep.equal(['outgoing', 'incoming']));
    it('should give each direction its own member', () => new Set(relations.map(_ => _.name)).size.should.equal(relations.length));
    it('should name the incoming relation as such', () => relations.map(_ => _.name).should.include('incomingPartOf'));
    it('should give every relation an explicit direction', () => relations.every(_ => !!_.direction).should.be.true);
    it('should give every view a unique name', () => new Set(built.views.map(_ => _.name)).size.should.equal(built.views.length));
});
