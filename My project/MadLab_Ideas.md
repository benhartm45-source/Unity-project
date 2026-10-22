# Mad Lab - ideas for later

## GAME HUB (coming back to this!)
Turn the game into a hub with several games in one app.
- One HUB scene with a game picker, and ONE SCENE PER GAME (Mad Lab, Arena Survival, new ones).
- Only the game you're playing is loaded, so the other games don't cause lag.
- Each game gets a "back to hub" button in its pause menu. One .exe, one icon.
- Things to fix when we build it:
  - Mad Lab's scripts start themselves in EVERY scene (RuntimeInitializeOnLoadMethod AfterSceneLoad:
    MLGame.Boot, MainMenu.Boot, ...). They must only start in the Mad Lab scene.
  - Clean up anything kept between scenes (DontDestroyOnLoad: sounds, network) when switching games.
  - Graphics settings (LOW / MEDIUM / HIGH) can be shared by all the games.
  - If the other games are in separate Unity projects, they have to be copied into this project first.
- Costs: bigger download and a few seconds of loading when you pick a game. Not lag.

## Other ideas
Quick wins:
- Loot crates / airdrops with rare materials in the middle of the map
- Kill feed ("Ben blew up Sam's lab with the OMEGA RAY")
- Legendary-recipe announcement to everyone
- Unique sounds for the new guns

Bigger:
- Monsters around the map (swamp slimes, crater boss that drops Stardust / Meteorite)
- Gadgets: grappling hook, jump pad, shield wall, teleporter pad
- Pets / drones that follow you (heal drone, slime buddy, turret drone)
- Gun rarities (common -> legendary) and upgrading guns at the workbench
- Day / night cycle (some materials only at night)
- Weather: acid rain, lightning storm, fog

Modes:
- Teams (2v2), raid mode (one mega-lab vs everyone), recipe speedrun

Progression:
- Achievements and stats, scientist skins

## Already done
- 250 new secret items (guns, armour, potions, bombs, enchants)
- Recipe book (B), hint scrolls (random every game), "SO CLOSE!" feedback
- OMEGA RAY (10 of every material, recipe never shown)
- Online sync: host runs the clock, the rival labs and decides the winner
- Lab decorations: posters, lava lamps, Hall of Discoveries trophy shelf (LabDecor.cs)
- Lab pets (slime tank, robot cat that hisses at intruders), lab levels 1-5, lab radio, security screen (LabExtras.cs)
- Holiday themes: Halloween (pumpkins, bats, ghost boss) and Winter (snow, snowmen, Snowball Gun) - Settings > Map Theme (Holiday.cs)

## More ideas (not done yet)
- Shrink / grow ray, portal gun, clone potion (decoy), freeze-time bomb, magnet glove
- Lab explosion on really wrong mixes, mutation zones, UFO dropping alien materials
- Spectator mode, mystery potions, random map events (meteor shower, golden hour)
- Secret rooms, minimap, quick chat, leaderboard, tutorial, key rebinding
