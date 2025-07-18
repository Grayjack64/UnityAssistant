# System: Ship Combat System
## Purpose
*This system manages all aspects of ship-to-ship combat, including weapons, damage, shields, and AI opponents.*
## Integration Points
- **Input Dependencies**: Ship Management System (for ship stats and upgrades), AI System (for enemy behavior), Input System (for player commands).
- **Output Provided**: Damage calculations, enemy destruction, resource drops (loot), combat log.
- **Events Triggered**: `ShipDamaged`, `ShipDestroyed`, `CombatStarted`, `CombatEnded`.
## Data Schema
```json
{
  "requiredFields": ["shipID", "weaponType", "targetID", "damageAmount"],
  "optionalFields": ["criticalHit", "shieldPenetration"],
  "validationRules": ["damageAmount > 0"]
}
```
## AI Implementation Guide
- **When to use**: Activated when two or more ships are within combat range and hostile to each other.
- **Common patterns**: Finite State Machines (FSM) for AI behavior (e.g., Attacking, Evading, Repairing).  Use of coroutines for timed events like weapon cooldowns.  Object pooling for projectiles.
- **Anti-patterns**:  Performing complex calculations every frame.  Hardcoding enemy behavior.  Ignoring armor and shield values.
- **Test scenarios**:
    - Ship A with basic weapons vs. Ship B with basic shields.
    - Ship A with high-penetration weapons vs. Ship B with strong shields.
    - Ship A vs. multiple weaker enemies.
    - Ship A with system damage fighting Ship B.
## Implementation Checklist
- [x] Core component created
- [x] Event system integrated
- [x] Unit tests written
- [x] Documentation updated