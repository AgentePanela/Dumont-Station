zlevel-add-command-description = Creates an empty map and links it as the z-level above or below your current map.
zlevel-add-command-help = Usage: zlevel_add <up|down>. Just creates the linked level; use zlevel_addgrid to put a floor on it.
zlevel-add-command-arg-path = [gridPath]
zlevel-add-command-success = Created and linked z-level map { $map }.

zlevel-addgrid-command-description = Adds a floor grid on the z-level above or below the grid you're standing on, creating the level if needed.
zlevel-addgrid-command-help = Usage: zlevel_addgrid <up|down> [gridPath]. Without a grid path, spawns a 3x3 lattice platform aligned and linked with your grid. If your grid is a station, the new floor is flagged to join it too.
zlevel-addgrid-command-success = Added a { $direction } z-level grid.

zlevel-move-command-description = Moves your controlled entity one z-level up or down, keeping its position.
zlevel-move-command-help = Usage: zlevel_move <up|down>
zlevel-move-command-success = Moved { $direction } one z-level.

zlevel-savemap-command-description = Saves a map that has z-levels. Only use it on maps built with z-levels.
zlevel-savemap-command-help = Usage: savezmap <mapId> <path>.
zlevel-savemap-command-arg-mapid = <base map id>
zlevel-savemap-command-success = Saved z-map to { $path } (base + floors).

zlevel-loadmap-command-description = Loads a saved z-map (base + its floors) uninitialized, for editing.
zlevel-loadmap-command-help = Usage: loadzmap <mapId> <path>
zlevel-loadmap-command-success = Loaded z-map from { $path }.

zlevel-initmap-command-description = Map-initializes every level of a z-stack.
zlevel-initmap-command-help = Usage: mapzinit [mapId]. Without an id, inits the stack you're standing in.
zlevel-initmap-command-success = Initialized the z-stack.

zlevel-command-no-entity = You need to be attached to an entity to use this command.
zlevel-command-no-map = Your entity is not on a valid map.
zlevel-command-no-grid = You need to be standing on a grid to use this command.

zlevel-error-level-exists = A z-level already exists in that direction.
zlevel-error-grid-load = Failed to load grid from { $path }.
zlevel-error-no-levels = The current map has no z-levels.
zlevel-error-no-level-in-direction = There is no z-level in that direction.
zlevel-error-invalid-mapid = No map with id { $id }.

zlevel-save-failed = Something went wrong while saving the z-map files.
zlevel-load-no-manifest = No z-map manifest found in { $path }.
