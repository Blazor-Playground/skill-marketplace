---
name: greeting-customizer
description: Customize greeting messages for CLI tools and applications. Generates locale-aware, context-appropriate greetings. USE FOR: creating greeting strings, localizing welcome messages, generating time-of-day greetings, customizing onboarding messages. DO NOT USE FOR: full i18n/l10n frameworks, translation services, UI component design.
---

# Greeting Customizer

Generate context-appropriate greeting messages for CLI tools and applications.

## When to Use

- User wants a greeting message for their tool's startup banner
- User needs time-of-day-aware greetings (Good morning / Good afternoon / Good evening)
- User wants to localize a welcome message
- User needs onboarding text for a new user experience

## How to Execute

1. Ask the user what context the greeting is for (CLI banner, web app, notification, etc.)
2. Determine if time-of-day awareness is needed
3. Generate the greeting with appropriate tone:
   - **CLI tools:** concise, technical, optionally with version info
   - **Web apps:** friendly, may include user's name
   - **Notifications:** brief, action-oriented

## Examples

**CLI banner greeting:**
```
Welcome to SkillForge v1.2.0
Your skill marketplace is ready. Run --help to get started.
```

**Time-aware greeting:**
```csharp
string GetGreeting(string userName) => DateTime.Now.Hour switch
{
    < 12 => $"Good morning, {userName}!",
    < 17 => $"Good afternoon, {userName}!",
    _    => $"Good evening, {userName}!"
};
```

## References

See `references/locale-patterns.md` for locale-specific greeting conventions.
