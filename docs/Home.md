# 🎮 Horticulture Unity Project Wiki

**Welcome to the Horticulture developer wiki!** This is your central hub for everything related to developing and maintaining the Horticulture educational game.

## 🚀 Quick Start

New to the project? Start here:

1. **[[developer-onboarding|Developer Onboarding]]** - Get your environment set up
2. **[[ARCHITECTURE|Architecture Overview]]** - Understand the system design
3. **[[card-core-system|Card System]]** - Learn the core mechanics
4. **[[testing-guide|Testing Guide]]** - Write and run tests

## 📚 Documentation Sections

### Getting Started
- [[developer-onboarding|Developer Onboarding Guide]]
- [[Quick-Reference|Quick Reference]]
- [[Common-Workflows|Common Development Workflows]]
- [[Troubleshooting|Troubleshooting Guide]]

### Core Systems
- [[card-core-system|Card Core System]]
- [[Plant-System|Plant Management System]]
- [[game-state-system-documentation|Game State & Persistence]]
- [[analytics-system|Analytics System]]
- [[audio-system-documentation|Audio System]]
- [[ui-input-management|UI & Input Management]]

### Game Features
- [[classes-system-documentation|Classes System]]
- [[cinematics-system-documentation|Cinematics System]]
- [[animation-hooks|Animation System]]
- [[i-location-card-system|Location Card System]]
- [[plant-location-card-slots-technical-design|Plant Location Slots]]

### Modding & Extensions
- [[modding-guide|Modding Guide]]
- [[mod-guide|Mod Development]]
- [[mod-loading-system-documentation|Mod Loading System]]

### API Reference
- [[api-reference|Complete API Reference]]
- [[Card-System-API|Card System API]]
- [[Plant-System-API|Plant System API]]
- [[Game-State-API|Game State API]]

### Guides & Tutorials
- [[testing-guide|Testing Guide]]
- [[user-guide|User Guide]]
- [[Feature-Design-Template|Feature Design Template]]

## 🔧 System Architecture

```
┌─────────────────────────────────────────┐
│          Presentation Layer             │
│  ┌──────────┐  ┌────────────────────┐  │
│  │ UI/Input │  │ Visual Effects     │  │
│  └──────────┘  └────────────────────┘  │
└─────────────────────────────────────────┘
┌─────────────────────────────────────────┐
│         Business Logic Layer            │
│  ┌──────────┐  ┌────────────────────┐  │
│  │ Card Core│  │ Plant Management   │  │
│  └──────────┘  └────────────────────┘  │
└─────────────────────────────────────────┘
┌─────────────────────────────────────────┐
│            Data Layer                   │
│  ┌──────────┐  ┌────────────────────┐  │
│  │ SaveLoad │  │ Configuration      │  │
│  └──────────┘  └────────────────────┘  │
└─────────────────────────────────────────┘
```

See [[ARCHITECTURE|Architecture Documentation]] for details.

## 🎯 Key Concepts

### Card Game System
The core gameplay loop involving plant placement, affliction management, and treatment application. [[card-core-system|Learn more →]]

### Plant Management
Individual plant behavior, health tracking, and affliction/treatment systems. [[Plant-System|Learn more →]]

### Game State & Persistence
Complete game state serialization and save/load functionality. [[game-state-system-documentation|Learn more →]]

### Analytics & Tracking
Player behavior tracking and performance metrics. [[analytics-system|Learn more →]]

## 🛠️ Common Tasks

### Working with Cards
- [[Adding-New-Card-Types|Adding New Card Types]]
- [[Card-Testing-Patterns|Testing Card Behavior]]
- [[Card-Serialization|Card Serialization]]

### Working with Plants
- [[Plant-Afflictions|Managing Afflictions]]
- [[Plant-Treatments|Applying Treatments]]
- [[Plant-Visual-Effects|Visual Effects System]]

### Development Workflows
- [[Code-Review-Process|Code Review Process]]
- [[Feature-Development|Feature Development]]
- [[Bug-Fixing-Workflow|Bug Fixing]]
- [[Performance-Optimization|Performance Optimization]]

## 🔍 Search & Navigation Tips

- Use `Ctrl/Cmd + O` to quickly search for pages
- Click on `[[wiki links]]` to navigate between pages
- Use the graph view to visualize page relationships
- Check "Backlinks" to see what references each page

## 📊 Project Stats

- **Unity Version**: 6000.1.11f1+
- **Primary Language**: C#
- **Architecture**: Component-based with Singleton coordinators
- **Testing**: Unity Test Framework (NUnit)

## 🤝 Contributing

Before making changes:
1. Review [[Code-Standards|Coding Standards]]
2. Follow [[Testing-Best-Practices|Testing Best Practices]]
3. Update relevant documentation
4. Submit for [[Code-Review-Process|code review]]

## 📞 Getting Help

- **Architecture Questions**: See [[ARCHITECTURE|Architecture Docs]]
- **API Usage**: Check [[api-reference|API Reference]]
- **Testing Issues**: Consult [[testing-guide|Testing Guide]]
- **Bugs/Issues**: Follow [[Bug-Reporting|Bug Reporting Process]]

## 🔄 Recently Updated

- [[urea-diminishing-returns-documentation|Urea Diminishing Returns System]]
- [[self-replicating-card-feature|Self-Replicating Card Feature]]
- [[plant-location-card-slots-technical-design|Plant Location Card Slots]]
- [[mod-loading-system-documentation|Mod Loading System]]

---

**Last Updated**: {{date}}
**Wiki Version**: 2.0
**Maintainers**: Development Team

