```markdown
# System: Crew Management System
## Purpose
Handles crew hiring, training, skills, and assignments on the ship.
## Integration Points
- **Input Dependencies**: Ship Management System (crew assignments), Resource and Economy System (crew salaries).
- **Output Provided**: Crew stats, skill levels, availability.
- **Events Triggered**: Crew Hired, Crew Fired, Skill Increased, Crew Assigned.
## Data Schema
```json
{
  "requiredFields": ["crewId", "name", "skillType", "skillLevel"],
  "optionalFields": ["specialization", "morale"],
  "validationRules": ["skillLevel >= 0", "skillLevel <= 10"]
}
```
## AI Implementation Guide
- **When to use**: When the player interacts with the crew management interface.
- **Common patterns**: Store crew data in a data structure (e.g., list, dictionary). Use modifiers based on crew skill to enhance ship performance.
- **Anti-patterns**: Ignoring crew skills in ship performance calculations, overly complex crew management mechanics, and crew members with useless skills.
- **Test scenarios**: Test hiring, firing, and assigning crew members with different skill sets. Verify that crew skills have a meaningful impact on ship performance.
## Implementation Checklist
- [x] Core component created
- [x] Event system integrated
- [x] Unit tests written
- [x] Documentation updated
```