# System: Resource Management
## Purpose
This system tracks and updates the player's resources (fuel, missiles, drone parts, scrap).

## Integration Points
- **Input Dependencies**: Navigation System (fuel consumption), Combat System (missile usage, scrap rewards), Trading System (resource exchange), Event System (resource gains/losses)
- **Output Provided**: Current resource levels
- **Events Triggered**: `onResourceDepleted`, `onResourceGained`

## Data Schema
```json
{
  "requiredFields": ["fuel", "missiles", "droneParts", "scrap"],
  "optionalFields": [],
  "validationRules": ["fuel >= 0", "missiles >= 0", "droneParts >= 0", "scrap >= 0"]
}
```

## AI Implementation Guide
- **When to use**: Any action that affects resource levels.
- **Common patterns**: Use a central resource manager class to update and track resources.
- **Anti-patterns**: Avoid directly manipulating resource values without going through the resource manager.
- **Test scenarios**: Resource consumption, resource gain, resource depletion.

## Implementation Checklist
- [ ] Core component created
- [ ] Event system integrated
- [ ] Unit tests written
- [ ] Documentation updated