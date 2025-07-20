# System: Combat System
## Purpose
This system governs real-time combat encounters, handling ship actions, weapon effects, and damage calculation.

## Integration Points
- **Input Dependencies**: Player input (weapon firing, system targeting), Ship System Targeting, Power Management, AI System (enemy actions).
- **Output Provided**: Game State (combat outcome), UI (combat feedback).
- **Events Triggered**: CombatStart, CombatEnd, ShipDamaged, ShipDestroyed.

## Data Schema
```json
{
  "requiredFields": ["playerShip", "enemyShips", "activeWeapons"],
  "optionalFields": ["environmentalHazards"],
  "validationRules": ["playerShip != null"]
}
```

## AI Implementation Guide
- **When to use**: During combat encounters.
- **Common patterns**: Enemy AI prioritizes survival and disabling player ship systems.  Use event-driven architecture for handling weapon fire and damage.
- **Anti-patterns**: Overly complex or unpredictable combat logic.
- **Test scenarios**: Player vs. single enemy, player vs. multiple enemies, different weapon combinations.

## Implementation Checklist
- [ ] Core component created
- [ ] Event system integrated
- [ ] Unit tests written
- [ ] Documentation updated