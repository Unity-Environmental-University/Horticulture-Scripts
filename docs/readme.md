# Horticulture Documentation

Welcome to the comprehensive documentation for the Horticulture Unity educational game. This directory contains all technical documentation, guides, and references for developers, educators, and users.

## Documentation Overview

### For Developers

#### Getting Started
- **[Developer Onboarding Guide](developer-onboarding.md)** - Complete guide for new developers joining the project
- **[Architecture Overview](architecture.md)** - System design, patterns, and technical architecture
- **[API Reference](api-reference.md)** - Complete API documentation for all public classes and methods

#### Development Resources
- **[Card Core System](card-core-system.md)** - Detailed documentation of the card game mechanics
- **[Analytics System](analytics-system.md)** - Comprehensive analytics tracking and data collection system
- **[Testing Guide](testing-guide.md)** - Testing strategies, frameworks, and best practices
- **[Plant Location Cards Design](plant-location-card-slots-technical-design.md)** - Technical design for location card feature

### For Users and Educators

- **[User Guide](user-guide.md)** - Complete gameplay guide including mechanics, strategies, and educational content
- **[Main README](../readme.md)** - Project overview, quick start, and general information

### Quick Navigation

## Core Systems Documentation

### Card Game System
The heart of Horticulture's gameplay mechanics:
- Turn-based progression system
- Card selection and placement mechanics
- Economic scoring system
- Strategic decision making

**Key Documents:**
- [Card Core System](card-core-system.md) - Complete system documentation
- [API Reference - Card System](api-reference.md#card-system) - Programming interface

### Plant Management System
Realistic plant health and pest management simulation:
- Individual plant behavior and state tracking
- Affliction and treatment application
- Visual feedback and health representation
- Infection level tracking system

**Key Documents:**
- [Architecture - Plant Management](architecture.md#plant-management-system) - System design
- [API Reference - Plant Management](api-reference.md#plant-management) - Programming interface

### Game State System
Complete game persistence and save/load functionality:
- Serialization of all game state
- Cross-session persistence
- Version compatibility management

**Key Documents:**
- [Architecture - Persistence](architecture.md#persistence-architecture) - Design overview
- [API Reference - Game State](api-reference.md#game-state) - Programming interface

### Analytics System
Comprehensive player data tracking and performance measurement:
- Round performance metrics and progression tracking
- Treatment effectiveness and educational outcome measurement
- Player behavior and strategic decision analysis
- Victory vs profitability distinction for game balance

**Key Documents:**
- [Analytics System](analytics-system.md) - Complete analytics documentation
- [API Reference - Analytics](api-reference.md#analytics) - Programming interface

## Educational Content

### Integrated Pest Management (IPM)
The game teaches real-world sustainable agriculture principles:
- **Biological Control**: Natural pest management methods
- **Cultural Control**: Environmental and agricultural practices
- **Chemical Control**: Judicious use of pesticides as a last resort
- **Economic Thresholds**: Cost-benefit analysis of treatments

### Learning Objectives
- Pest and disease identification
- Treatment selection and application
- Economic decision making in agriculture
- Sustainable farming practices
- Environmental impact awareness

## Development Workflow

### Code Quality Process
1. **Implementation**: Follow coding standards and architecture patterns
2. **Code Review**: Submit to code-reviewer agent for quality assurance
3. **Documentation Review**: Submit to documentation-engineer agent
4. **Testing**: Write and run appropriate tests
5. **Integration**: Merge with comprehensive validation

### Documentation Standards
- **API Documentation**: XML documentation for all public members
- **Architecture Updates**: Keep system design documentation current
- **User Documentation**: Maintain guides for gameplay and educational content
- **Testing Documentation**: Document test strategies and procedures

## File Structure

```
docs/
├── readme.md                             # This file - documentation index
├── developer-onboarding.md               # New developer guide
├── user-guide.md                         # Complete user manual
├── architecture.md                       # System architecture overview
├── api-reference.md                      # Complete API documentation
├── testing-guide.md                      # Testing strategies and practices
├── analytics-system.md                   # Analytics tracking and data collection
├── card-core-system.md                   # Card system detailed documentation
├── plant-location-card-slots-technical-design.md  # Location cards feature design
└── plant-spots.md                       # Plant location concepts
```

## Getting Help

### For Developers
- **Architecture Questions**: See [architecture.md](architecture.md) for system design
- **API Usage**: Check [api-reference.md](api-reference.md) for method signatures
- **Testing Issues**: Consult [testing-guide.md](testing-guide.md) for best practices
- **Onboarding**: Follow [developer-onboarding.md](developer-onboarding.md) step by step

### For Users
- **Gameplay Help**: See [user-guide.md](user-guide.md) for complete instructions
- **Educational Content**: Review the IPM sections in the user guide
- **Technical Issues**: Check the troubleshooting sections

### For Educators
- **Curriculum Integration**: Use the educational content sections
- **Learning Objectives**: Review the IPM principles documentation
- **Assessment Ideas**: Based on game mechanics and real-world connections

## Contributing to Documentation

### Documentation Updates
When modifying code or adding features:
1. Update relevant API documentation
2. Modify architecture documentation if system design changes
3. Update user guides for new features
4. Add or update testing documentation

### Documentation Review Process
All documentation changes should be reviewed by the documentation-engineer agent to ensure:
- Clarity and accuracy
- Consistency with existing documentation
- Appropriate technical depth
- Proper formatting and organization

### Style Guidelines
- **Clear and Concise**: Write for the intended audience
- **Complete Examples**: Include working code samples
- **Visual Aids**: Use diagrams and flowcharts where helpful
- **Consistent Formatting**: Follow established markdown patterns

## Version Information

- **Documentation Version**: 1.0.0
- **Game Version**: Current development branch
- **Last Updated**: Current as of latest project state
- **Unity Version**: 6000.1.11f1+

## Contact and Support

For questions about this documentation or the Horticulture project:
- Review the appropriate guide in this directory
- Check the main project README for contact information
- Consult the development team for project-specific questions

---

**Note**: This documentation reflects the current state of the Horticulture project. As the project evolves, documentation will be updated to reflect changes in architecture, features, and best practices.
