```markdown
# System: Event System
## Purpose
Manages emergent events and story moments.
## Integration Points
- **Input Dependencies**: Galaxy Generation System (sector data), Ship Combat System (combat outcomes).
- **Output Provided**: Story events, dialogue, choices.
- **Events Triggered**: Event Started, Event Ended, Choice Made.
## Data Schema
```json
{
  "requiredFields": ["eventId", "eventType", "description"],
  "optionalFields": ["choices", "rewards", "consequences"],
  "validationRules": ["eventId != null"]
}
```
## AI Implementation Guide
- **When to use**: When the player enters a new sector or after a significant event (e.g., combat).
- **Common patterns**: Use a weighted random selection to determine which event to trigger. Branching dialogue trees for choices and consequences.
- **Anti-patterns**: Repetitive or uninteresting events, unclear consequences for choices, and events that break the core gameplay loop.
- **Test scenarios**: Trigger different events in various sectors and situations. Verify that the events are engaging, and that choices have meaningful consequences that align with the narrative.
## Implementation Checklist
- [x] Core component created
- [x] Event system integrated
- [x] Unit tests written
- [x] Documentation updated
```