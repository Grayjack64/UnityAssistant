# System: Combat
## Purpose
This system manages real-time-with-pause combat encounters between the player's ship and enemy ships.

## Integration Points
- **Input Dependencies**: Player input (targeting, pause), Ship System (ship stats, module status), Resource Management System (missile usage)
- **Output Provided**: Combat results (win/loss), Damage to ships, Resource changes (scrap gained)
- **Events Triggered**: `onCombatStart`, `onCombatEnd`, `onShipDamaged`, `onShipDestroyed`

## Data Schema
```json
{
  "requiredFields": ["playerShip", "enemyShips", "combatState"],
  "optionalFields": [],
  "validationRules": []
}
```

## AI Implementation Guide
- **When to use**: When the player encounters an enemy at a beacon.
- **Common patterns**: Use a state machine for managing combat phases (player turn, enemy turn, resolution).
- **Anti-patterns**: Avoid overly complex AI that makes combat unpredictable and unfair.
- **Test scenarios**: Player winning, player losing, different enemy types, using different ship modules.

## Implementation Checklist
- [ ] Core component created
- [ ] Event system integrated
- [ ] Unit tests written
- [ ] Documentation updated