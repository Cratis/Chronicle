// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';

export const ProjectionHelpPanel: React.FC = () => {
    return (
        <div style={{ padding: '20px', height: '100%', overflowY: 'auto', backgroundColor: '#1e1e1e', color: '#d4d4d4' }}>
            <h2 style={{ color: '#ffffff', fontWeight: 600, marginTop: 0, marginBottom: '20px' }}>
                Projection DSL Quick Reference
            </h2>

            <div style={{ borderBottom: '1px solid #3e3e42', marginBottom: '20px' }} />

            <div style={{ marginBottom: '20px' }}>
                <h3 style={{ color: '#4ec9b0', fontWeight: 600, marginTop: 0, marginBottom: '10px' }}>
                    Basic Structure
                </h3>
                <pre style={{ backgroundColor: '#2d2d30', padding: '12px', borderRadius: '4px', fontSize: '13px', margin: 0 }}>
{`projection MyProjection => MyReadModel
  from MyEvent
    property = eventProperty`}
                </pre>
            </div>

            <div style={{ marginBottom: '20px' }}>
                <h3 style={{ color: '#4ec9b0', fontWeight: 600, marginTop: 0, marginBottom: '10px' }}>
                    Defaults
                </h3>
                <div style={{ fontSize: '13px' }}>
                    <div style={{ marginBottom: '5px' }}>• <strong>AutoMap enabled</strong> - Matching properties are automatically copied from events</div>
                    <div style={{ marginBottom: '5px' }}>• <strong>Key defaults to $eventSourceId</strong> - No explicit key mapping needed unless using a different property</div>
                    <div style={{ marginBottom: '5px' }}>• Use <strong style={{ color: '#569cd6' }}>no automap</strong> to disable automatic property mapping</div>
                </div>
            </div>

            <div style={{ marginBottom: '20px' }}>
                <h3 style={{ color: '#4ec9b0', fontWeight: 600, marginTop: 0, marginBottom: '10px' }}>
                    Keywords
                </h3>
                <div style={{ fontSize: '13px' }}>
                    <div style={{ marginBottom: '5px' }}><strong style={{ color: '#569cd6' }}>projection</strong> - Define a projection and its target read model</div>
                    <div style={{ marginBottom: '5px' }}><strong style={{ color: '#569cd6' }}>from</strong> - Specify event type to project from</div>
                    <div style={{ marginBottom: '5px' }}><strong style={{ color: '#569cd6' }}>every</strong> - Apply mappings to all events</div>
                    <div style={{ marginBottom: '5px' }}><strong style={{ color: '#569cd6' }}>key</strong> - Define the read model key (defaults to $eventSourceId)</div>
                    <div style={{ marginBottom: '5px' }}><strong style={{ color: '#569cd6' }}>parent</strong> - Define parent key for hierarchical data</div>
                    <div style={{ marginBottom: '5px' }}><strong style={{ color: '#569cd6' }}>join</strong> - Join with another collection</div>
                    <div style={{ marginBottom: '5px' }}><strong style={{ color: '#569cd6' }}>children</strong> - Define child collection operations</div>
                    <div style={{ marginBottom: '5px' }}><strong style={{ color: '#569cd6' }}>increment</strong> - Increase a numeric value by 1</div>
                    <div style={{ marginBottom: '5px' }}><strong style={{ color: '#569cd6' }}>decrement</strong> - Decrease a numeric value by 1</div>
                    <div style={{ marginBottom: '5px' }}><strong style={{ color: '#569cd6' }}>count</strong> - Count occurrences</div>
                    <div style={{ marginBottom: '5px' }}><strong style={{ color: '#569cd6' }}>add</strong> - Add value to a property (add &lt;property&gt; by &lt;expression&gt;)</div>
                    <div style={{ marginBottom: '5px' }}><strong style={{ color: '#569cd6' }}>subtract</strong> - Subtract value from a property (subtract &lt;property&gt; by &lt;expression&gt;)</div>
                    <div style={{ marginBottom: '5px' }}><strong style={{ color: '#569cd6' }}>remove</strong> - Remove read model instance or child item</div>
                    <div style={{ marginBottom: '5px' }}><strong style={{ color: '#569cd6' }}>no automap</strong> - Disable automatic property mapping</div>
                </div>
            </div>

            <div style={{ marginBottom: '20px' }}>
                <h3 style={{ color: '#4ec9b0', fontWeight: 600, marginTop: 0, marginBottom: '10px' }}>
                    Expressions
                </h3>
                <div style={{ fontSize: '13px' }}>
                    <div style={{ marginBottom: '5px' }}><strong>$eventSourceId</strong> - Event source identifier (default key)</div>
                    <div style={{ marginBottom: '5px' }}><strong>$eventContext.</strong> - Event metadata (occurred, sequenceNumber, correlationId, causationId, eventType, eventSourceId)</div>
                    <div style={{ marginBottom: '5px' }}><strong>property</strong> - Event property reference</div>
                    <div style={{ marginBottom: '5px' }}><strong>`template $&#123;expr&#125;`</strong> - Template strings with interpolation</div>
                    <div style={{ marginBottom: '5px' }}><strong>Literals</strong> - true, false, null, numbers, "strings"</div>
                </div>
            </div>

            <div style={{ marginBottom: '20px' }}>
                <h3 style={{ color: '#4ec9b0', fontWeight: 600, marginTop: 0, marginBottom: '10px' }}>
                    Key Patterns
                </h3>
                <pre style={{ backgroundColor: '#2d2d30', padding: '12px', borderRadius: '4px', fontSize: '13px', margin: 0 }}>
{`// Simple key (inline)
from MyEvent key userId
  name = name

// Composite key
from MyEvent
  key CompositeKey
    userId = userId
    tenantId = tenantId

// Parent relationship
from ChildEvent
  parent parentId
  name = name`}
                </pre>
            </div>

            <div style={{ marginBottom: '20px' }}>
                <h3 style={{ color: '#4ec9b0', fontWeight: 600, marginTop: 0, marginBottom: '10px' }}>
                    Common Patterns
                </h3>
                <pre style={{ backgroundColor: '#2d2d30', padding: '12px', borderRadius: '4px', fontSize: '13px', margin: 0 }}>
{`// Count events
from MyEvent
  count eventCount

// Arithmetic operations
from PaymentReceived
  add balance by amount
  count paymentCount

from PaymentRefunded
  subtract balance by amount

// Join collection
join orders on customerId
  events OrderPlaced
    totalOrders = $count

// Child collection
children items identified by itemId
  from ItemAdded
    name = name
    quantity = quantity`}
                </pre>
            </div>

            <div style={{ marginBottom: '20px' }}>
                <h3 style={{ color: '#4ec9b0', fontWeight: 600, marginTop: 0, marginBottom: '10px' }}>
                    Tips
                </h3>
                <div style={{ fontSize: '13px' }}>
                    <div style={{ marginBottom: '5px' }}>• Use <strong>Tab</strong> for indentation (2 spaces)</div>
                    <div style={{ marginBottom: '5px' }}>• Press <strong>Ctrl+Space</strong> for auto-completion</div>
                    <div style={{ marginBottom: '5px' }}>• Hover over keywords for documentation</div>
                    <div style={{ marginBottom: '5px' }}>• Auto-completion is context-sensitive</div>
                    <div style={{ marginBottom: '5px' }}>• Errors appear inline with red markers</div>
                    <div style={{ marginBottom: '5px' }}>• Use <strong>causedBy</strong> for identity tracking</div>
                    <div style={{ marginBottom: '5px' }}>• Use <strong>eventContext</strong> for metadata</div>
                </div>
            </div>

            <div style={{ borderBottom: '1px solid #3e3e42', marginBottom: '20px' }} />

            <div>
                <a href="https://www.cratis.io/docs/Chronicle/projections" target="_blank" rel="noopener noreferrer" style={{ fontSize: '13px', color: '#4ec9b0', display: 'block', marginBottom: '5px' }}>
                    Documentation
                </a>
                <a href="https://www.cratis.io/docs/Chronicle/projections/projection-definition-language" target="_blank" rel="noopener noreferrer" style={{ fontSize: '13px', color: '#4ec9b0', display: 'block' }}>
                    PDL Reference
                </a>
            </div>
        </div>
    );
};
