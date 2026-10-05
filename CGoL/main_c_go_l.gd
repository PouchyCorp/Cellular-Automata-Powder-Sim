extends Control

@export var cell_size:Vector2
var cell_amount:Vector2

var grid:Array[Array]
var neighbours:Array[Vector2] = [
	Vector2(-1, -1),
	Vector2(-1, 0),
	Vector2(-1, 1),
	Vector2(0, -1),
	Vector2(0, 1),
	Vector2(1, -1),
	Vector2(1, 0),
	Vector2(1, 1)
]
var auto_process:bool = false
var last_tick = 0

func _ready() -> void:
	cell_amount = size / cell_size
	cell_amount.x = int(cell_amount.x)
	cell_amount.y = int(cell_amount.y)
	initiate_grid()

func initiate_grid() -> void:
	grid = []
	for x in cell_amount.x:
		grid.append([])
		for y in cell_amount.y:
			grid[x].append(false)

func _draw() -> void:
	for x in cell_amount.x:
		for y in cell_amount.y:
			var cell_rect = Rect2(Vector2(x,y) * cell_size, cell_size)
			draw_rect(cell_rect, Color.WHITE, grid[x][y])

func _process(delta: float) -> void:
	var t = Time.get_ticks_msec()
	if auto_process && t - last_tick >= 120:
		last_tick = t
		update_grid()
	elif Input.is_action_pressed("LeftClick"):
		var mouse_pos = get_local_mouse_position() / cell_size
		if mouse_pos.x < cell_amount.x && mouse_pos.y < cell_amount.y:
			grid[int(mouse_pos.x)][int(mouse_pos.y)] = true
			queue_redraw()
	elif Input.is_action_pressed("RightClick"):
		var mouse_pos = get_local_mouse_position() / cell_size
		if mouse_pos.x < cell_amount.x && mouse_pos.y < cell_amount.y:
			grid[int(mouse_pos.x)][int(mouse_pos.y)] = false
			queue_redraw()
	
	if Input.is_action_just_pressed("ToggleAutoProcess"):
		set_auto_process()
	
	if Input.is_action_just_pressed("ClearGrid"):
		initiate_grid()
		queue_redraw()

	if Input.is_action_just_pressed("RandomizeGrid"):
		for x in cell_amount.x:
			for y in cell_amount.y:
				grid[x][y] = randf() > 0.5
		queue_redraw()

	if Input.is_action_just_pressed("UpdateGrid"):
		update_grid()

func update_grid() -> void:
	var old_grid = grid.duplicate_deep(2)
	var new_grid = old_grid.duplicate_deep(2)
	
	for x in cell_amount.x:
		for y in cell_amount.y:
			var alive_neighbours = get_alive_neighbour_count(Vector2(x,y), old_grid)
			# Rules
			if old_grid[x][y]: # if was alive
				if (alive_neighbours < 2 || alive_neighbours > 3): new_grid[x][y] = false # underpopulation or overpopulation
			else:
				if alive_neighbours == 3: new_grid[x][y] = true # reproduction
	grid = new_grid
	 
	queue_redraw()

func get_alive_neighbour_count(pos:Vector2, source_grid:Array[Array]) -> int:
	var result:int = 0
	
	for neighbour in neighbours:
		var npos = neighbour + pos
		if 0 <= npos.x && npos.x < cell_amount.x && \
			0 <= npos.y && npos.y < cell_amount.y && \
			source_grid[npos.x][npos.y]:
				result += 1
	
	return result

func set_auto_process():
	auto_process = !auto_process
