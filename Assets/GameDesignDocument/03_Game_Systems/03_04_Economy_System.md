# System: Economy System
## Purpose
*This system manages the flow of resources, trading, and pricing of items within the game.*
## Integration Points
- **Input Dependencies**:  Loot System (for acquired resources), Ship Management System (for upgrade costs), AI System (for market demand), UI System (for displaying prices).
- **Output Provided**:  Item prices, resource availability, trading opportunities.
- **Events Triggered**: `TransactionCompleted`, `PriceChanged`, `MarketUpdated`.
## Data Schema
```json
{
  "requiredFields": ["itemID", "basePrice", "supply", "demand"],
  "optionalFields": ["priceModifier", "availability"],
  "validationRules": ["basePrice >= 0", "supply >= 0", "demand >= 0"]
}
```
## AI Implementation Guide
- **When to use**: Called when the player interacts with a merchant or accesses the market UI.
- **Common patterns**: Supply and demand curves for determining item prices.  Dynamic pricing based on player actions and global events.
- **Anti-patterns**:  Static pricing.  Allowing the player to exploit the market.  Creating resource bottlenecks.
- **Test scenarios**:
    - Buying and selling items and verifying that prices change accordingly.
    - Flooding the market with a particular resource and observing the price plummet.
    - Starving the market of a resource and observing the price skyrocket.
    - Ensuring that the player can always afford basic necessities.
## Implementation Checklist
- [x] Core component created
- [x] Event system integrated
- [x] Unit tests written
- [x] Documentation updated