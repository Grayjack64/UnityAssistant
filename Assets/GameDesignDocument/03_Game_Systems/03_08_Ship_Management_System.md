# System: Ship Management System
## Purpose
*Manages all aspects of the player's and enemy ships, including their stats, upgrades, crew, and inventory.*
## Integration Points
- **Input Dependencies**:  Combat System (damage updates), Loot System (items acquired), Outpost System (ship upgrades and repairs), Crew Management System (crew assignments and skills).
- **Output Provided**:  Ship stats to Combat System, inventory data to Loot System and Outpost System, crew information to Crew Management System.
- **Events Triggered**: `ShipUpgraded`, `ShipRepaired`, `CrewAssigned`, `CrewRemoved`, `InventoryChanged`.
## Data Schema
```json
{
  "requiredFields": ["shipHullType", "armor", "shieldCapacity", "weaponSlots", "crewCapacity"],
  "optionalFields": ["enginePower", "sensorRange", "fuelCapacity", "specialAbilities"],
  "validationRules": ["armor >= 0", "shieldCapacity >= 0", "weaponSlots >= 0", "crewCapacity >= 0"]
}
```
## AI Implementation Guide
- **When to use**: When a ship is created, damaged, repaired, upgraded, or when crew assignments change. Also used to determine enemy loadouts.
- **Common patterns**: Use a component-based architecture to represent ship systems (e.g., shields, engines, weapons). Implement a data structure to store ship inventory. Use events to notify other systems of ship state changes.
- **Anti-patterns**: Directly modifying ship stats without proper validation. Hardcoding ship stats and upgrade paths. Failing to handle crew deaths or injuries.
- **Test scenarios**: Upgrading ship systems with different modules. Assigning crew members with different skills to ship systems. Taking damage from different weapon types. Running out of fuel or ammunition.
## Implementation Checklist
- [ ] Core ship data structure implemented
- [ ] Ship upgrade system implemented
- [ ] Crew management system integrated
- [ ] Inventory management system integrated
- [ ] Unit tests written for ship stat calculations
- [ ] Documentation updated