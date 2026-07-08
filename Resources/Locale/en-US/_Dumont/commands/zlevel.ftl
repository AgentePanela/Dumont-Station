zlevel-add-command-description = Creates a new map and links it as the z-level above or below your current map.
zlevel-add-command-help = Usage: zlevel_add <up|down> [gridPath]. Without a grid path, spawns a 3x3 lattice platform aligned and linked with the grid you're standing on.
zlevel-add-command-arg-path = [gridPath]
zlevel-add-command-success = Created and linked z-level map { $map }.

zlevel-move-command-description = Moves your controlled entity one z-level up or down, keeping its position.
zlevel-move-command-help = Usage: zlevel_move <up|down>
zlevel-move-command-success = Moved { $direction } one z-level.

zlevel-command-no-entity = You need to be attached to an entity to use this command.
zlevel-command-no-map = Your entity is not on a valid map.

zlevel-error-level-exists = A z-level already exists in that direction.
zlevel-error-grid-load = Failed to load grid from { $path }.
zlevel-error-no-levels = The current map has no z-levels.
zlevel-error-no-level-in-direction = There is no z-level in that direction.
