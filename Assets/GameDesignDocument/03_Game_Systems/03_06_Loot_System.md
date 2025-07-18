# System: Loot System
## Purpose
*This system handles the generation and distribution of loot after combat or exploration.*
## Integration Points
- **Input Dependencies**: Ship Combat System (for determining drop chances), Galaxy Generation System (for sector difficulty), Economy System (for item values).
- **Output Provided**: List of items dropped, resource quantities.
- **Events Triggered**: `LootGenerated`, `LootCollected`.
## Data Schema
```json
{
  "requiredFields": ["lootTableID", "dropChance", "quantity"],
  "optionalFields": ["itemRarity"],
  "validationRules": ["dropChance >= 0", "dropChance <= 1"]
}
```
## AI Implementation Guide
- **When to use**: Triggered upon the destruction of an enemy ship or discovery of a hidden cache.
- **Common patterns**: Weighted random selection from loot tables. Scaling drop rates based on enemy difficulty. Ensuring that loot is appropriate for the sector and player level.
- **Anti-patterns**:  Generating loot that is useless to the player.  Making loot too rare or too common.  Ignoring resource limits.
- **Test scenarios**:
    - Defeating different types of enemies and verifying that the loot is appropriate.
    - Exploring different sectors and verifying that the loot is scaled to the difficulty.
    - Ensuring that the player can collect loot and add it to their inventory.
## Implementation Checklist
- [x] Core component created
- [x] Event system integrated
- [x] Unit tests written
- [x] Documentation updated