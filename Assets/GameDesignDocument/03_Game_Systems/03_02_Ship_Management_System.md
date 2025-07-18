# System: Ship Management System
## Purpose
*This system handles ship upgrades, crew management, resource allocation, and overall ship status.*
## Integration Points
- **Input Dependencies**:  Loot System (for acquired resources), Combat System (for damage reports), Economy System (for purchasing upgrades), UI System (for displaying information).
- **Output Provided**: Ship statistics, available upgrade slots, crew member assignments, resource levels.
- **Events Triggered**: `ShipUpgraded`, `CrewHired`, `ResourceChanged`, `SystemDamaged`, `SystemRepaired`.
## Data Schema
```json
{
  "requiredFields": ["shipID", "hullType", "resourceCapacity", "crewCapacity"],
  "optionalFields": ["installedModules", "assignedCrew"],
  "validationRules": ["resourceCapacity >= 0", "crewCapacity >= 0"]
}
```
## AI Implementation Guide
- **When to use**: Called when the player interacts with the ship's systems via the UI or when the ship takes damage during combat.
- **Common patterns**:  Data-driven design for ship statistics and upgrade costs.  Observer pattern for updating UI elements when ship stats change.
- **Anti-patterns**: Hardcoding upgrade paths.  Ignoring resource constraints when applying upgrades.  Allowing invalid crew assignments.
- **Test scenarios**:
    - Upgrading weapons systems and verifying damage output increases in combat.
    - Hiring crew and verifying their impact on ship performance (e.g., repair speed).
    - Allocating power to different systems and verifying their functionality changes.
    - Reaching resource capacity and being unable to collect more.
## Implementation Checklist
- [x] Core component created
- [x] Event system integrated
- [x] Unit tests written
- [x] Documentation updated