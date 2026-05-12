# Debug Duo — MVP

3D cooperative puzzle game. Two players each build a Scratch-like command queue, press **Run**, and watch both characters execute simultaneously.

## Quick start

1. Open the project in Unity 6 (6000.0.66f2).
2. Wait for the editor to compile the `DebugDuo` scripts (no errors expected).
3. From the menu bar: **Debug Duo → Build MVP Scene**.
4. The scene `Assets/Scenes/DebugDuo.unity` is created and opened.
5. Press **Play**.

That's the full MVP. The builder spawns floor/walls, two players, the door, the pressure button, both goals, the canvas with command buttons, the Run/Reset bar, and the win panel — all wired up.

## How to play

### Level layout
- **Player A (blue)** — top lane. Blocked by a wooden door.
- **Player B (orange)** — bottom lane. Free path, but has a red pressure button.
- Pressing the button opens A's door.
- Each player must reach the yellow goal pad in their own lane.

### Controls
Each player panel has these buttons:
- Move Forward (2 units)
- Turn Left (-90°)
- Turn Right (+90°)
- Wait (1 second)
- Press Button (activates a button within 1.5 units)
- Undo / Clear

Bottom bar:
- **RUN** — both queues execute at the same time.
- **RESET** — restore players, door, button, goals.

### Sample winning solution
- **Player B**: Move, Move, Press Button, Move, Move, Move
- **Player A**: Wait, Move, Move, Move, Move, Move

Both reach goals simultaneously and the win panel appears.

## Files

```
Assets/Scripts/DebugDuo/
  CommandType.cs            — enum
  PlayerCommandExecutor.cs  — runs a queue of commands
  CommandQueueUI.cs         — wires UI buttons to a player
  RunManager.cs             — Run/Reset, runs both queues in parallel
  ButtonTrigger.cs          — pressure button
  DoorController.cs         — animated door, blocks movement when closed
  GoalZone.cs               — per-player goal trigger, win check
  WinUI.cs                  — show/hide win panel
  Editor/
    DebugDuoSceneBuilder.cs — one-click scene builder (menu)
```

## Notes / extending
- Movement collision uses a forward raycast against any object tagged `Blocker`. The builder adds the tag automatically.
- Door becomes non-blocking once open by changing its tag.
- Add more buttons/doors by linking them in the inspector — the systems are independent.
- Tweak `moveDistance`, `moveDuration`, `turnDuration`, `waitDuration` in the `PlayerCommandExecutor` inspector.
