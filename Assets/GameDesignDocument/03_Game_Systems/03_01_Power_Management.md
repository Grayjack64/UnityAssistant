# System: Power Management
## Purpose
This system manages the allocation of power to different ship systems, affecting their performance and capabilities.

## Integration Points
- **Input Dependencies**: Player input (power allocation sliders/buttons), Ship Systems (current power draw, max power draw), Combat System (damage to power conduits).
- **Output Provided**: Ship Systems (available power), UI (power levels display), Combat System (system effectiveness based on power).
- **Events Triggered**: PowerOverload, PowerShortage, SystemOffline.

## Data Schema
```json
{
  "requiredFields": ["systemName", "currentPower", "maxPower", "powerAllocated"],
  "optionalFields": ["isOverloaded", "isOffline"],
  "validationRules": ["powerAllocated >= 0", "powerAllocated <= maxPower"]
}
```

## AI Implementation Guide
- **When to use**: During combat, during exploration (passive power drain).
- **Common patterns**: Prioritize weapons and shields in combat, prioritize engines during exploration. If damaged, reduce power to non-essential systems.
- **Anti-patterns**: Evenly distributing power regardless of situation.
- **Test scenarios**:  Full power to engines, full power to weapons, power overload, zero power.

## Implementation Checklist
- [ ] Core component created
- [ ] Event system integrated
- [ ] Unit tests written
- [ ] Documentation updated