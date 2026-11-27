// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Build entry only (excluded from declarations): bundles the package styles into dist/styles.css, which hosts
// import through the "./styles.css" export. Keeping the import out of the public modules means no emitted
// declaration refers to a stylesheet.
import './Card/Card.css';
