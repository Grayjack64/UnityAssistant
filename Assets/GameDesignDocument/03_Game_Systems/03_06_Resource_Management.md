# System: Resource Management
## Purpose
Manages the player's resources, including scrap (currency), fuel, missiles, drone parts, etc.

## Integration Points
- **Input Dependencies**: Combat System (rewards from combat), Event System (rewards from events), Ship Management System (costs for upgrades).
- **Output Provided**: UI (resource display), Ship Management System (resource availability for upgrades).
- **Events Triggered**: ResourceGained, ResourceLost, ResourceLow.

## Data Schema
```json
{
  "requiredFields": ["scrap", "fuel"],
  "optionalFields": ["missiles", "droneParts"],
  "validationRules": ["scrap >= 0", "fuel >= 0"]
}
```

## AI Implementation Guide
- **When to use**:  Throughout the game loop, particularly after combat and events.
- **Common patterns**:  Track resource changes through a central system, trigger warnings at low fuel levels.
- **Anti-patterns**: Allowing negative resources or exploits.
- **Test scenarios**: Gaining and losing resources, checking resource limits.

## Implementation Checklist
- [ ] Core component created
- [ ] Event system integrated
- [ ] Unit tests written
- [ ] Documentation updated