# System: Rebel Fleet
## Purpose
This system manages the advancing Rebel fleet that pressures the player to keep moving through the galaxy.

## Integration Points
- **Input Dependencies**: Navigation System (player location), Galaxy Generation System (sector data)
- **Output Provided**: Rebel fleet position, Distance to player
- **Events Triggered**: `onRebelFleetAdvance`, `onRebelFleetOvertake`

## Data Schema
```json
{
  "requiredFields": ["currentSector", "position"],
  "optionalFields": [],
  "validationRules": []
}
```

## AI Implementation Guide
- **When to use**: After each player turn or after a certain number of beacons are visited.
- **Common patterns**: Use a simple timer or counter to trigger fleet advancement.
- **Anti-patterns**: Avoid unpredictable or unfair fleet movement.
- **Test scenarios**: Fleet advancing, fleet overtaking the player, player escaping the fleet.


## Implementation Checklist
- [ ] Core component created
- [ ] Event system integrated
- [ ] Unit tests written
- [ ] Documentation updated