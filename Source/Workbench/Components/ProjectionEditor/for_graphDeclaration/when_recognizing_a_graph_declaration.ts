// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { isGraphDeclaration, organizationGraphExample } from '../graphDeclaration';

describe('when recognizing a graph declaration', () => {
    it('should recognize the example', () => isGraphDeclaration(organizationGraphExample).should.be.true);
    it('should recognize a projection that builds a graph', () => isGraphDeclaration('projection Org => graph Organization\n  node Person\n').should.be.true);
    it('should recognize it after a concept line', () => isGraphDeclaration('concept PersonId : Uuid\n\nprojection Org => graph Organization\n').should.be.true);
    it('should not recognize a projection onto a read model', () => isGraphDeclaration('projection Orders => OrderReadModel\n  from OrderPlaced\n').should.be.false);
    it('should not recognize a read model that is named graph-something', () => isGraphDeclaration('projection Orders => GraphSettings\n').should.be.false);
    it('should not recognize an empty declaration', () => isGraphDeclaration('').should.be.false);
    it('should not recognize a mention in a comment', () => isGraphDeclaration('# projection Org => graph Organization').should.be.false);
});
