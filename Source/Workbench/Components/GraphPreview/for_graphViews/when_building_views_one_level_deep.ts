// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { buildViews } from '../graphViews';
import { organization } from './given/organization';

describe('when building views one level deep', () => {
    const built = buildViews(organization, 'Person', 1)!;
    const root = built.views.find(_ => _.name === built.rootView)!;
    const relation = root.members.find(_ => _.kind === 'relation')!;
    const target = built.views.find(_ => _.name === relation.view)!;

    it('should put the root view first', () => built.views[0].name.should.equal('Person'));
    it('should build a view for the root and one for the related node', () => built.views.length.should.equal(2));
    it('should follow only the edges that leave or enter the node', () => root.members.filter(_ => _.kind === 'relation').length.should.equal(1));
    it('should follow the edge', () => relation.edge!.should.equal('MEMBER_OF'));
    it('should follow it in the direction it leads from the node', () => relation.direction!.should.equal('outgoing'));
    it('should hold every related item', () => relation.cardinality!.should.equal('many'));
    it('should name the member after the edge', () => relation.name.should.equal('memberOf'));
    it('should anchor the target view at the node at the far end', () => target.root.should.equal('Group'));
    it('should read the properties of the edge in the target view', () => target.members.find(_ => _.kind === 'edgeProperty')!.source!.should.equal('since'));
    it('should not follow relations further than the depth', () => target.members.some(_ => _.kind === 'relation').should.be.false);
});
