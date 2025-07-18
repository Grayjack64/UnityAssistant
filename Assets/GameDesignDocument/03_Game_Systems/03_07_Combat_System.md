```markdown
# System: Combat System
## Purpose
*Handles all aspects of space combat, including ship movement, weapon firing, damage calculation, and AI behavior.*
## Integration Points
- **Input Dependencies**: Player Input System (for ship controls and weapon selection), Ship Management System (for ship stats and loadouts), AI System (for enemy behavior).
- **Output Provided**: Damage updates to Ship Management System, win/loss conditions to Game State System, visual effects to Visual Effects System.
- **Events Triggered**: `ShipDamaged`, `ShipDestroyed`, `CombatWon`, `CombatLost`, `WeaponFired`.
## Data Schema
```json
{
  "requiredFields": ["shipID", "targetID", "weaponType", "damageAmount", "armorPenetration"],
  "optionalFields": ["criticalHitChance", "statusEffect"],
  "validationRules": ["damageAmount >= 0", "armorPenetration >= 0", "criticalHitChance >= 0 && criticalHitChance <= 1"]
}
```
## AI Implementation Guide
- **When to use**: During combat encounters when enemy ships are present.
- **Common patterns**: Use a state machine for AI behavior (e.g., Attacking, Evading, Retreating).  Implement pathfinding for ship movement, considering obstacles and enemy positions. Prioritize targets based on threat level and ship type. Use probability distributions for weapon firing patterns.
- **Anti-patterns**:  Hardcoding enemy behavior. Ignoring ship stats and weapon loadouts when making combat decisions. Failing to adapt to changing combat conditions.
- **Test scenarios**:  1v1 combat with different ship types and weapon loadouts.  Multiple enemy encounters with varying difficulty levels. Combat scenarios with obstacles and environmental hazards.  Testing different AI behaviors (e.g., aggressive, defensive, evasive).
## Implementation Checklist
- [ ] Core combat loop implemented (turn-based or real-time with pause)
- [ ] Ship movement and weapon firing implemented
- [ ] Damage calculation and armor penetration implemented
- [ ] AI behavior for enemy ships implemented
- [ ] Visual effects for combat events implemented
- [ ] Unit tests written for damage calculation and AI behavior
- [ ] Documentation updated