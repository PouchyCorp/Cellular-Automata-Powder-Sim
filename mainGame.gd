extends Node

func _process(_delta: float) -> void:
	%FPS.text = str(Engine.get_frames_per_second())

func load_file():
	$FileDialog.file_mode = FileDialog.FILE_MODE_OPEN_FILE
	$FileDialog.show()
	# Disconnecting everything
	for connection in $FileDialog.file_selected.get_connections(): 
		$FileDialog.file_selected.disconnect(connection.callable)
	# Connect new signal
	$FileDialog.file_selected.connect($CellularAutomataEngine.LoadGridFromFile)


func save_file():
	$FileDialog.file_mode = FileDialog.FILE_MODE_SAVE_FILE
	$FileDialog.show()
	# Disconnecting everything
	for connection in $FileDialog.file_selected.get_connections(): 
		$FileDialog.file_selected.disconnect(connection.callable)
	# Connect new signal
	$FileDialog.file_selected.connect($CellularAutomataEngine.SaveGridToFile)


func _on_brush_size_value_changed(value: float) -> void:
	%BrushSizeLabel.text = "Taille de la brosse : " + str(%BrushSize.value)


func _on_game_speed_value_changed(value: float) -> void:
	%GameSpeedLabel.text = "Vitesse du jeu : " + str(%GameSpeed.value)
