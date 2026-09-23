# IMPORTANT : This is just an example, not used in the game

extends Node2D

@export var grid_dimention:Vector2 = Vector2(1000, 175)
@export var cell_size:Vector2 = Vector2(1,1)
var grid:Array[Array]

func _ready() -> void:
	init_grid()

func init_grid() -> void:
	for x in grid_dimention.x:
		grid.append([])
		for y in grid_dimention.y:
			grid[x].append(0)

func _draw() -> void:
	# Draw outline rect
	draw_rect(Rect2(Vector2.ZERO, grid_dimention * cell_size), Color.DIM_GRAY, false)
	
	# Draw little rects
	for x in grid_dimention.x:
		for y in grid_dimention.y:
			if grid[x][y]:
				draw_rect(Rect2(Vector2(x,y) * cell_size, cell_size), Color.LIGHT_GOLDENROD, true)

func _process(_delta: float) -> void:
	#
	$Control/Label.text = str(Engine.get_frames_per_second())

	
	# Process adding sand
	if Input.is_action_pressed("LeftClick"):
		var pos:Vector2 = get_viewport().get_mouse_position() / cell_size
		if (pos.x < grid_dimention.x && pos.y < grid_dimention.y):
			grid[int(pos.x)][int(pos.y)] = 1
	
	# Process existing sand
	var grid2 = grid.duplicate(true)
	for x in grid_dimention.x:
		for y in grid_dimention.y - 1: # No use updating the last row for the sand
			if grid2[x][y]:
				# Rules
				if not grid2[x][y + 1]:
					grid[x][y] = 0
					grid[x][y + 1] = 1
				elif x-1 >= 0 && not grid2[x - 1][y + 1]:
					grid[x][y] = 0
					grid[x - 1][y + 1] = 1
				elif x+1 < grid_dimention.x && not grid2[x + 1][y + 1]:
					grid[x][y] = 0
					grid[x + 1][y + 1] = 1
	
	queue_redraw()
