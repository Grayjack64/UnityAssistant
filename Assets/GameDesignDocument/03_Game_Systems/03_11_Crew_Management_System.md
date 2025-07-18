# System: Crew Management System
## Purpose
*Allows the player to hire, assign, and manage crew members on their ship, each with unique skills and abilities.*
## Integration Points
- **Input Dependencies**: Ship Management System (crew capacity), Outpost System (hiring crew), Combat System (crew injuries/deaths).
- **Output Provided**: Ship Management System (crew bonuses), Combat System (crew skills during combat), Story System (crew interactions).
- **Events Triggered**: `CrewHired`, `CrewFired`, `CrewAssigned`, `CrewSkillUsed`.
## Data Schema
```json
{
  "requiredFields": ["crewName", "crewSkill", "crewRank", "crewSalary"],
  "optionalFields": ["crewMorale", "crewSpecialization", "crewTraits"],
  "validationRules": ["crewSkill in ['Gunnery', 'Engineering', 'Navigation', 'Medical']", "crewRank >= 0 && crewRank <= 5"]
}
```
## AI Implementation Guide
- **When to use**: When the player hires a new crew member, assigns a crew member to a station, or when crew members gain experience.
- **Common patterns**: Store crew data in a dedicated data structure. Implement a system for assigning crew members to specific ship systems. Calculate crew bonuses based on skill level and station assignment. Implement a morale system to affect crew performance.
- **Anti-patterns**: Hardcoding crew skills and bonuses. Failing to provide meaningful choices for crew assignments. Ignoring crew morale.
- **Test scenarios**: Hiring crew members with different skills and specialties. Assigning crew members to different stations and verifying their bonuses. Testing the effects of morale on crew performance.
## Implementation Checklist
- [ ] Crew data structure implemented
- [ ] Crew hiring system implemented
- [ ] Crew assignment system implemented
- [ ] Crew skill and bonus system implemented
- [ ] Crew morale system implemented
- [ ] Unit tests written for crew skill calculations
- [ ] Documentation updated