# System: Ship System
## Purpose
This system manages the player's ship, including its modules, upgrades, and overall stats.

## Integration Points
- **Input Dependencies**: Resource Management System (scrap for upgrades), Upgrade System (new modules/upgrades)
- **Output Provided**: Ship stats, Module status, Ship layout
- **Events Triggered**: `onModuleUpgrade`, `onShipUpgrade`

## Data Schema
```json
{
  "requiredFields": ["modules", "hullIntegrity", "shields"],
  "optionalFields": ["crew"],
  "validationRules": ["hullIntegrity >= 0", "shields >= 0"]
}
```

## AI Implementation Guide
- **When to use**: When upgrading the ship or during combat to calculate damage and effects.
- **Common patterns**: Use a component-based system for managing modules.
- **Anti-patterns**: Avoid monolithic ship classes that are difficult to modify and extend.
- **Test scenarios**: Upgrading modules, taking damage, repairing the ship.

## Implementation Checklist
- [ ] Core component created
- [ ] Event system integrated
- [ ] Unit tests written
- [ ] Documentation updated