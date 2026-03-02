# Locale-Specific Greeting Patterns

## Common Patterns by Locale

| Locale | Formal | Informal | Time-Aware |
|--------|--------|----------|------------|
| en-US | "Welcome" | "Hey there!" | "Good morning/afternoon/evening" |
| en-GB | "Welcome" | "Hiya!" | "Good morning/afternoon/evening" |
| es-ES | "Bienvenido/a" | "¡Hola!" | "Buenos días/tardes/noches" |
| fr-FR | "Bienvenue" | "Salut !" | "Bonjour/Bonsoir" |
| de-DE | "Willkommen" | "Hallo!" | "Guten Morgen/Tag/Abend" |
| ja-JP | "ようこそ" | "こんにちは" | "おはよう/こんにちは/こんばんは" |

## Best Practices

- Always provide a fallback to English
- Use ICU message format for interpolation when possible
- Consider cultural norms: some locales expect formal greetings by default
- Time boundaries vary by culture (e.g., "afternoon" starts at noon in US, after lunch in Spain)
