```markdown
# System: Ship Management System
## Purpose
Manages ship upgrades, crew assignments, and resource allocation.
## Integration Points
- **Input Dependencies**: Resource and Economy System (available resources), Crew Management System (crew stats).
- **Output Provided**: Ship stats, active crew members, available upgrades.
- **Events Triggered**: Ship Upgraded, Crew Assigned, Resource Depleted.
## Data Schema
```json
{
  "requiredFields": ["shipId", "hullType", "availablePower"],
  "optionalFields": ["weapons", "modules", "crewAssignments"],
  "validationRules": ["availablePower >= 0"]
}
```
## AI Implementation Guide
- **When to use**: When the player interacts with the ship management interface.
- **Common patterns**: Use data structures to represent ship components and their stats. Employ an upgrade tree or similar mechanism for managing available upgrades.
- **Anti-patterns**: Allowing the player to install incompatible upgrades, insufficient feedback on upgrade effects.
- **Test scenarios**: Test various upgrade combinations, crew assignments, and resource allocation scenarios. Verify that the system prevents invalid configurations and provides clear feedback to the player.
## Implementation Checklist
- [x] Core component created
- [x] Event system integrated
- [x] Unit tests written
- [x] Documentation updated
```