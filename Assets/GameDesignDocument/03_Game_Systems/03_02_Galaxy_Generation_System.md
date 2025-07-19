```markdown
# System: Galaxy Generation System
## Purpose
Generates the procedurally created galaxy for each playthrough.
## Integration Points
- **Input Dependencies**: Game start parameters (seed, difficulty).
- **Output Provided**: Galaxy map data (sector locations, resources, hazards).
- **Events Triggered**: Sector Discovered, Anomaly Detected.
## Data Schema
```json
{
  "requiredFields": ["sectorId", "coordinates", "threatLevel"],
  "optionalFields": ["resources", "anomalies"],
  "validationRules": ["threatLevel >= 0", "coordinates.x >= 0", "coordinates.y >= 0"]
}
```
## AI Implementation Guide
- **When to use**: At the start of a new game or when the player jumps to a new, unexplored sector.
- **Common patterns**: Use a procedural generation algorithm (e.g., Perlin noise, cellular automata) to create varied and interesting galaxy maps.  Seed-based generation for reproducibility.
- **Anti-patterns**: Generating empty or repetitive sectors, performance bottlenecks during galaxy generation.
- **Test scenarios**: Generate galaxies with different seeds and difficulty levels. Verify that the generated galaxy is diverse and playable, and that resource distribution is appropriate for the selected difficulty.
## Implementation Checklist
- [x] Core component created
- [x] Event system integrated
- [x] Unit tests written
- [x] Documentation updated
```