# System: Ship Management
## Purpose
This system manages the player's ship, including upgrades, repairs, and crew assignments.

## Integration Points
- **Input Dependencies**: Player input (upgrade selection, crew assignment), Resource Management System (available resources).
- **Output Provided**: Ship data (current stats, crew assignments), UI (ship display).
- **Events Triggered**: ShipUpgraded, CrewAssigned, ShipRepaired.

## Data Schema
```json
{
  "requiredFields": ["shipType", "systemStats", "crewAssignments"],
  "optionalFields": ["damage"],
  "validationRules": ["shipType != null"]
}
```

## AI Implementation Guide
- **When to use**: Outside of combat, at specific stations.
- **Common patterns**: Suggest optimal upgrades and crew assignments based on ship type and playstyle.
- **Anti-patterns**: Allowing impossible configurations or exploits.
- **Test scenarios**: Upgrading different systems, assigning crew to different stations.

## Implementation Checklist
- [ ] Core component created
- [ ] Event system integrated
- [ ] Unit tests written
- [ ] Documentation updated