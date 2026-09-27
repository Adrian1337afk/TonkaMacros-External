# TonkaMacros External Client – Version 4.2

Eigenständiger Windows-Input-Client. Er liest keinen Minecraft-Speicher, verändert keine Spieldateien und erzeugt keine Spielpakete. Die Abläufe bestehen ausschließlich aus simulierten Tastatur- und Maustasten-Eingaben. Der Mauszeiger wird nicht bewegt.

## Start

1. ZIP vollständig entpacken.
2. `Tonka-External-Client.exe` starten.
3. Kategorie öffnen und Keybinds auf deine Minecraft-Tasten einstellen.
4. Gewünschte Module rechts oben einzeln einschalten.
5. Oben `ARM CLIENT` aktivieren.
6. Minecraft fokussieren und den Activate Key verwenden.

`Ende` stoppt sofort. Bei Fokusverlust wird die Sequenz abgebrochen und eine gehaltene Maustaste losgelassen. Alle Werte werden in `tonka-external-client.ini` gespeichert.

## Vereinfachte Anchor-Sequenz

Version 4.2 verwendet wieder eine einzige, eindeutig getaktete Reihenfolge. Der gewählte Delay liegt zwischen jedem Schritt, damit kein Key losgeht, während der vorherige Rechtsklick noch verarbeitet wird:

`Anchor-Key → Rechtsklick → Glowstone-Key → Rechtsklick → Totem-Key → Rechtsklick`.

### Single Anchor

Empfohlener Start: `60 ms`. Auf stabilen lokalen Welten kann `45–55 ms` funktionieren. Wenn ein Schritt fehlt, in 5-ms-Schritten erhöhen. Alle vier Tasten – Activate, Anchor, Glowstone und Totem – sind frei wählbar.

### Double Anchor / Tripple Anchor

Zwei beziehungsweise drei vollständige Anchor-Zyklen. Jeder Anchor wird platziert, geladen und mit ausgewähltem Totem explodiert. Nach einer Explosion folgt der kurze **Air Handoff** zum nächsten Anchor.

Empfohlener Start: Step Delay `55 ms`, Air Handoff `18 ms`, Randomize `2 %`. Fehlt innerhalb eines Zyklus ein Schritt, Step Delay erhöhen. Wird der nächste Anchor nicht angenommen, Air Handoff zwischen `16–25 ms` testen. Der Server entscheidet trotzdem, ob das enge Blockupdate-Fenster akzeptiert wird.

### Obby Air Place

Vollständiger Ablauf: `Anchor-Key → Place → Glowstone-Key → Charge → Totem-Key → Explode → Obsidian-Key → zwei Place-Versuche`. Start: Step Delay `55`, Air Handoff `18`, Spam Gap `24 ms`.

## Stun Slam

`Axe → genau ein Shield-Break-Hit → 10-ms-Wechsel → Mace → genau ein Slam`. Der einzige sichtbare Timing-Regler ist **Mace Timing**, Standard `50 ms`; Click Hold wurde aus der Oberfläche entfernt. Randomize startet bei `5 %`. Der Activate Key ist frei wählbar – `R` ist nur die mitgelieferte Beispielbelegung. Der Client erkennt extern weder Schild noch Fallzustand; du musst ihn im richtigen Moment auslösen.

## Auto D-Tap

Version 4.1 führt den vollständigen Ablauf aus: `Sword Hit → Obsidian → Crystal 1 platzieren/brechen → Damage-Window abwarten → Crystal 2 platzieren/brechen`.

- Action Speed: `55 ms`
- Second Crystal: `520 ms`
- Crystal Break Gap: `45 ms`

Das zweite Timing hängt besonders stark von Server-Tickrate, Ping und eventuell veränderten No-Damage-Ticks ab. Wenn Crystal 1 funktioniert, Crystal 2 aber keinen getrennten Treffer erzeugt, **Second Crystal** in 10-ms-Schritten zwischen `480–580 ms` testen.

## Themes

Die Kategorie **Themes** besitzt einen vollständigen Farbwähler, editierbare Hex-Codes, elf schnelle Akzentfarben, zehn Textfarben, Fenster-Deckkraft und fünf Schriftarten: Segoe UI, Bahnschrift, Corbel, Candara und Trebuchet MS. Akzentfarbe, Kartentext, Schrift und Deckkraft werden automatisch gespeichert. Ein Hintergrundbild ist bewusst nicht enthalten.

## Cart

**Double Insta Cart** bleibt unverändert. **Pre-Bow Double Cart** startet stabiler mit: Bow Settle `55`, Charge `220`, manuelles Flick-Fenster `160`, Rail `55`, Cart `65`, Next Item `12`, Input Hold `18 ms`.

Pre-Bow: erst nach oben zielen und aktivieren; nach dem Schuss innerhalb des Flick-Fensters selbst nach unten flicken. Danach folgen Rail, Cart 1 und Cart 2.

## Performance und Grenze

Die Engine nutzt 5-ms-Hotkey-Polling, einen `AboveNormal`-Worker und schlafbasierte Klick-Holds, damit Spammen das Spiel nicht unnötig belastet.

Der Client kennt Ziel, Inventarinhalt, Treffer, Schildstatus, Server-Tick und Ping nicht. Deshalb gibt es keine 100-%-Ergebnis- oder „undetectable“-Garantie. Zeiten müssen für FPS, Ping und Server kalibriert werden. Nur in Singleplayer, auf dem eigenen Testserver oder dort verwenden, wo Makros erlaubt sind.
