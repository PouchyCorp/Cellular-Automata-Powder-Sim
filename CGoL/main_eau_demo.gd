extends Control

@export var cell_size:Vector2
var cell_amount:Vector2

var grid:Array[Array]
var auto_process:bool = false
var last_tick = 0;

# Arrow display system
var arrow_texture:Texture2D
var movement_arrows:Array[Dictionary] = []
var arrow_display_time:float = 3.0
var arrow_timer:float = 0.0

func _ready() -> void:
	print(size)
	cell_amount = size / cell_size
	cell_amount.x = int(cell_amount.x)
	cell_amount.y = int(cell_amount.y)
	initiate_grid()
	
	# Load arrow texture
	arrow_texture = load("res://assets/red_arrow.png")

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
			draw_rect(cell_rect, Color.GRAY, false)
			if grid[x][y] == true:
				draw_rect(cell_rect, Color.BLUE, true)
	
	# Draw movement arrows if they should be visible
	if arrow_timer > 0.0 && arrow_texture:
		for arrow_data in movement_arrows:
			draw_movement_arrow(arrow_data)

func draw_movement_arrow(arrow_data: Dictionary) -> void:
	var from_pos = arrow_data["from"] * cell_size + cell_size * 0.5
	var to_pos = arrow_data["to"] * cell_size + cell_size * 0.5
	var direction = arrow_data["direction"]
	
	# Calculate arrow position (middle of the movement path)
	var arrow_pos = (from_pos + to_pos) * 0.5
	
	# Calculate rotation based on direction
	var rotation_angle = 0.0
	if direction == Vector2(0, 1):  # Down
		rotation_angle = 0.0
	elif direction == Vector2(1, 0):  # Right
		rotation_angle = -PI / 2
	elif direction == Vector2(-1, 0):  # Left
		rotation_angle = PI / 2
	elif direction == Vector2(1, 1):  # Diagonal down-right
		rotation_angle = -PI / 4
	elif direction == Vector2(-1, 1):  # Diagonal down-left
		rotation_angle = PI / 4
	
	# Scale the arrow to fit within the cell
	var arrow_scale = min(cell_size.x, cell_size.y) / max(arrow_texture.get_width(), arrow_texture.get_height()) * 0.8
	
	# Draw the arrow with fade effect based on remaining time
	var alpha = arrow_timer / arrow_display_time
	var arrow_color = Color(1, 1, 1, alpha)
	
	# Create a transform for rotation
	var transform = Transform2D()
	transform = transform.scaled(Vector2(arrow_scale, arrow_scale))
	transform = transform.rotated(rotation_angle)
	transform.origin = arrow_pos
	
	draw_set_transform_matrix(transform)
	draw_texture(arrow_texture, -Vector2(arrow_texture.get_width(), arrow_texture.get_height()) * 0.5, arrow_color)
	draw_set_transform_matrix(Transform2D.IDENTITY)  # Reset transform

func _process(delta: float) -> void:
	var t = Time.get_ticks_msec()
	if auto_process && t - last_tick >= 30:
		last_tick = t
		update_grid()
	
	# Update arrow timer
	if arrow_timer > 0.0:
		arrow_timer -= delta
		if arrow_timer <= 0.0:
			arrow_timer = 0.0
			movement_arrows.clear()  # Clear arrows when timer expires
		queue_redraw()  # Redraw to update arrow visibility/alpha
	
	if Input.is_action_pressed("LeftClick"):
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
	
	# Clear previous movement arrows and start tracking new ones
	movement_arrows.clear()
	arrow_timer = arrow_display_time
	
	for x in cell_amount.x:
		for y in cell_amount.y:
			# Rules
			if old_grid[x][y] == false : continue
			if y+1 == cell_amount.y: continue
			
			if old_grid[x][y+1] == false && grid[x][y+1] == false:
				grid[x][y] = false
				grid[x][y+1] = true
				# Add downward movement arrow
				movement_arrows.append({
					"from": Vector2(x, y),
					"to": Vector2(x, y+1),
					"direction": Vector2(0, 1)
				})
				continue
			
			if x+1 < cell_amount.x && x-1 >= 0 && old_grid[x+1][y+1] == false && old_grid[x-1][y+1] == false && grid[x+1][y+1] == false && grid[x-1][y+1] == false:
				if randf() > 0.5:
					grid[x][y] = false
					grid[x-1][y+1] = true
					# Add diagonal left-down movement arrow
					movement_arrows.append({
						"from": Vector2(x, y),
						"to": Vector2(x-1, y+1),
						"direction": Vector2(-1, 1)
					})
					continue
				else:
					grid[x][y] = false
					grid[x+1][y+1] = true
					# Add diagonal right-down movement arrow
					movement_arrows.append({
						"from": Vector2(x, y),
						"to": Vector2(x+1, y+1),
						"direction": Vector2(1, 1)
					})
					continue
			elif x+1 < cell_amount.x && old_grid[x+1][y+1] == false && grid[x+1][y+1] == false:
				grid[x][y] = false
				grid[x+1][y+1] = true
				# Add diagonal right-down movement arrow
				movement_arrows.append({
					"from": Vector2(x, y),
					"to": Vector2(x+1, y+1),
					"direction": Vector2(1, 1)
				})
				continue
			elif x-1 >= 0 && old_grid[x-1][y+1] == false && grid[x-1][y+1] == false:
				grid[x][y] = false
				grid[x-1][y+1] = true
				# Add diagonal left-down movement arrow
				movement_arrows.append({
					"from": Vector2(x, y),
					"to": Vector2(x-1, y+1),
					"direction": Vector2(-1, 1)
				})
				continue
			
			elif x+1 < cell_amount.x && x-1 >= 0 && grid[x-1][y] == false && grid[x+1][y] == false:
				if randf() > 0.5:
					grid[x][y] = false
					grid[x-1][y] = true
					# Add left horizontal movement arrow
					movement_arrows.append({
						"from": Vector2(x, y),
						"to": Vector2(x-1, y),
						"direction": Vector2(-1, 0)
					})
					continue
				else:
					grid[x][y] = false
					grid[x+1][y] = true
					# Add right horizontal movement arrow
					movement_arrows.append({
						"from": Vector2(x, y),
						"to": Vector2(x+1, y),
						"direction": Vector2(1, 0)
					})
					continue
			elif x+1 < cell_amount.x && grid[x+1][y] == false:
				grid[x][y] = false
				grid[x+1][y] = true
				# Add right horizontal movement arrow
				movement_arrows.append({
					"from": Vector2(x, y),
					"to": Vector2(x+1, y),
					"direction": Vector2(1, 0)
				})
				continue
			elif x-1 >= 0 && grid[x-1][y] == false:
				grid[x][y] = false
				grid[x-1][y] = true
				# Add left horizontal movement arrow
				movement_arrows.append({
					"from": Vector2(x, y),
					"to": Vector2(x-1, y),
					"direction": Vector2(-1, 0)
				})
				continue
			
	
	queue_redraw()

func set_auto_process():
	auto_process = !auto_process

func set_no_auto_process():
	auto_process = false
