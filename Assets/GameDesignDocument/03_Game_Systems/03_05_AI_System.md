# System: AI System
## Purpose
*This system controls the behavior of non-player characters (NPCs), including enemies, allies, and merchants.*
## Integration Points
- **Input Dependencies**:  Ship Combat System (for target selection), Ship Management System (for ship stats), Galaxy Generation System (for sector layout).
- **Output Provided**:  NPC actions, movement, dialogue.
- **Events Triggered**: `NPCSpawned`, `NPCDetected`, `NPCAction`.
## Data Schema
```json
{
  "requiredFields": ["npcID", "faction", "behaviorType"],
  "optionalFields": ["aggressionLevel", "morale"],
  "validationRules": ["aggressionLevel >= 0", "aggressionLevel <= 1"]
}
```
## AI Implementation Guide
- **When to use**: Continuously running for active NPCs, triggered by events for reactive NPCs.
- **Common patterns**: Behavior trees, state machines, goal-oriented action planning (GOAP). Use of NavMesh for pathfinding in sectors with derelict stations.
- **Anti-patterns**:  Hardcoding NPC behavior.  Ignoring player actions.  Creating predictable patterns.
- **Test scenarios**:
    - Enemy ships engaging the player in combat.
    - Friendly ships assisting the player in combat.
    - Merchants offering different prices based on player reputation.
    - NPCs reacting to player choices in dialogue.
## Implementation Checklist
- [x] Core component created
- [x] Event system integrated
- [x] Unit tests written
- [x] Documentation updated