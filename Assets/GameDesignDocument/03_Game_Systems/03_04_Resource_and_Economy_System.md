```markdown
# System: Resource and Economy System
## Purpose
Manages the buying, selling, and salvaging of resources and equipment.
## Integration Points
- **Input Dependencies**: Ship Combat System (loot drops), Galaxy Generation System (resource distribution in sectors).
- **Output Provided**: Available resources, transaction results, market prices.
- **Events Triggered**: Resource Acquired, Resource Sold, Market Updated.
## Data Schema
```json
{
  "requiredFields": ["resourceType", "quantity", "marketPrice"],
  "optionalFields": ["quality", "source"],
  "validationRules": ["quantity >= 0", "marketPrice >= 0"]
}
```
## AI Implementation Guide
- **When to use**: When the player interacts with a merchant or salvage operation.
- **Common patterns**: Implement a simple supply and demand model to dynamically adjust market prices. Store resources in a data structure (e.g., dictionary) for efficient access.
- **Anti-patterns**: Static market prices, exploits that allow the player to generate unlimited resources, and confusing or inconsistent pricing schemes.
- **Test scenarios**: Test buying, selling, and salvaging resources under various market conditions. Ensure that the system prevents exploits and provides clear feedback to the player.
## Implementation Checklist
- [x] Core component created
- [x] Event system integrated
- [x] Unit tests written
- [x] Documentation updated
```