using Godot;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;


public partial class CellularAutomataEngine : Node2D
{

	// --- Private element instantiation --- //
	private Element[,] elementArray;
	private int cellWidth;
	private int cellHeight;
	private int gridWidth;
	private int gridHeight;

	private float zoomFactor = 1.0f;
	private Vector2 baseCellSize = new Vector2(4, 4);
	private (int, int) zoomedWindowOrigin = (0, 0);
	private int zoomedWindowWidth;
	private int zoomedWindowHeight;

	private DrawingState _drawingState = DrawingState.None;

	// Elements that should only be placed once per click, not continuously
	private readonly string[] singleClickElements = { "Seed", "Worm", "Snail" };

	private ButtonGroup buttonGroup;
	public string selectedElement; // TODO idk how to do differently

	private Slider gameSpeedSlider;
	public float gameSpeed = 1;

	private Slider brushSizeSlider;
	public int brushSize = 1;
	float gameSpeedCounter = 0f;
	public int tick = 0;
	private int pendingSingleSteps = 0;

	private readonly HashSet<(int, int)> textureDirtyCells = new();
	private HashSet<(int, int)> activeNeedUpdateCells = new();

	private ColorRect gridRenderer;
	private ShaderMaterial gridShaderMaterial;
	private Image gridColorImage;
	private ImageTexture gridColorTexture;


	// --- Public (exported) element instantiation --- //
	[ExportCategory("Simulation Size")]
	[Export]
	public Vector2 cellSize { get; set; } = new Vector2(2 * 2, 2 * 2);
	[Export]
	public Vector2 gridSize { get; set; } = new Vector2(288, 162);

	// --- Methods --- //
	public override void _Ready()
	{
		base._EnterTree();
		cellWidth = (int)cellSize.X;
		cellHeight = (int)cellSize.Y;
		baseCellSize = cellSize;
		gridWidth = (int)gridSize.X;
		gridHeight = (int)gridSize.Y;

		zoomedWindowWidth = (int)gridSize.X;
		zoomedWindowHeight = (int)gridSize.Y;

		Button firstButton = GetNode<Button>("%Sand");
		buttonGroup = firstButton.ButtonGroup;

		brushSizeSlider = GetNode<Slider>("%BrushSize");
		gameSpeedSlider = GetNode<Slider>("%GameSpeed");

		elementArray = new Element[gridWidth, gridHeight];
		SetupGridRenderer();
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		UiHandler();
		PlacementHandler();
		while (pendingSingleSteps > 0)
		{
			AdvanceSimulationStep();
			pendingSingleSteps--;
		}
		gameSpeedCounter += gameSpeed;
		while (Math.Floor(gameSpeedCounter) > 0) // game speed just skips steps
		{
			AdvanceSimulationStep();
			gameSpeedCounter--;
		}

		if (textureDirtyCells.Count > 0)
		{
			LazyRefreshGridTextures();
		}
	}

	//Those inputs may be ignored by filters
	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton { Pressed: true } eventMouseButton)
		{
			switch (eventMouseButton.ButtonIndex)
			{
				case MouseButton.Left:
					// Check if this is a single-click element
					if (IsSingleClickElement(selectedElement))
					{
						PlaceSingleElement();
					}
					else
					{
						_drawingState = DrawingState.Drawing;
					}
					break;
				case MouseButton.Right:
					_drawingState = DrawingState.Erasing;
					break;
				case MouseButton.Middle:
					{
						Vector2 mousePos = GetViewport().GetMousePosition();
						Vector2 viewportSize = GetViewportRect().Size;
						if (viewportSize.X > 0 && viewportSize.Y > 0)
						{
							Vector2 relativeMouse = mousePos / viewportSize;
							int targetCellX = (int)Math.Clamp(relativeMouse.X * zoomedWindowWidth + zoomedWindowOrigin.Item1, 0, gridWidth - 1);
							int targetCellY = (int)Math.Clamp(relativeMouse.Y * zoomedWindowHeight + zoomedWindowOrigin.Item2, 0, gridHeight - 1);
							zoomedWindowOrigin = (
								Math.Clamp(targetCellX - zoomedWindowWidth / 2, 0, Math.Max(0, gridWidth - zoomedWindowWidth)),
								Math.Clamp(targetCellY - zoomedWindowHeight / 2, 0, Math.Max(0, gridHeight - zoomedWindowHeight))
							);
							LazyRefreshGridTextures();
						}
					}
					break;
				case MouseButton.WheelUp:
					zoomIn(GetViewport().GetMousePosition());
					break;
				case MouseButton.WheelDown:
					zoomOut(GetViewport().GetMousePosition());
					break;
			}
		}
	}
	private void updateZoom(){
		(int, int) mouseGridPos = GetGridPositionFromMouse();

		Vector2 viewportSize = GetViewportRect().Size;
		zoomedWindowWidth = Math.Max(1, (int)MathF.Ceiling(viewportSize.X / cellSize.X));
		zoomedWindowHeight = Math.Max(1, (int)MathF.Ceiling(viewportSize.Y / cellSize.Y));

		int desiredOriginX = mouseGridPos.Item1 - zoomedWindowWidth / 2;
		int desiredOriginY = mouseGridPos.Item2 - zoomedWindowHeight / 2;

		zoomedWindowOrigin = (
			Math.Clamp(desiredOriginX, 0, Math.Max(0, gridWidth - zoomedWindowWidth)),
			Math.Clamp(desiredOriginY, 0, Math.Max(0, gridHeight - zoomedWindowHeight))
		);

		if (gridShaderMaterial != null)
		{
			gridShaderMaterial.SetShaderParameter("grid_size", gridSize);
			gridShaderMaterial.SetShaderParameter("cell_size", cellSize);
			gridShaderMaterial.SetShaderParameter("zoomed_window_origin", new Vector2(zoomedWindowOrigin.Item1, zoomedWindowOrigin.Item2));
			gridShaderMaterial.SetShaderParameter("zoomed_window_size", new Vector2(zoomedWindowWidth, zoomedWindowHeight));
		}

		if (gridRenderer != null)
		{
			gridRenderer.Size = GetViewportRect().Size;
		}

		GD.Print($"Zoom Factor: {zoomFactor}, Zoomed Window Origin: {zoomedWindowOrigin}, Zoomed Window Size: ({zoomedWindowWidth}, {zoomedWindowHeight})");
	}
	private void zoomIn(Vector2? mousePosition = null){
		Vector2 anchor = mousePosition ?? GetViewport().GetMousePosition();
		Vector2 viewportSize = GetViewportRect().Size;
		Vector2 relativeMouse = viewportSize.X > 0 && viewportSize.Y > 0 ? anchor / viewportSize : Vector2.Zero;
		int mouseCellX = (int)Math.Clamp(relativeMouse.X * zoomedWindowWidth + zoomedWindowOrigin.Item1, 0, gridWidth - 1);
		int mouseCellY = (int)Math.Clamp(relativeMouse.Y * zoomedWindowHeight + zoomedWindowOrigin.Item2, 0, gridHeight - 1);

		float nextZoomFactor = Math.Clamp(zoomFactor * 1.2f, 1.0f, 10.0f);
		zoomFactor = nextZoomFactor;
		cellSize = new Vector2(
			Math.Clamp(baseCellSize.X * zoomFactor, 1f, 100f),
			Math.Clamp(baseCellSize.Y * zoomFactor, 1f, 100f)
		);
		updateZoom();

		int desiredOriginX = mouseCellX - (int)(relativeMouse.X * zoomedWindowWidth);
		int desiredOriginY = mouseCellY - (int)(relativeMouse.Y * zoomedWindowHeight);
		zoomedWindowOrigin = (
			Math.Clamp(desiredOriginX, 0, Math.Max(0, gridWidth - zoomedWindowWidth)),
			Math.Clamp(desiredOriginY, 0, Math.Max(0, gridHeight - zoomedWindowHeight))
		);
		LazyRefreshGridTextures();
	}

	private void zoomOut(Vector2? mousePosition = null){
		Vector2 anchor = mousePosition ?? GetViewport().GetMousePosition();
		Vector2 viewportSize = GetViewportRect().Size;
		Vector2 relativeMouse = viewportSize.X > 0 && viewportSize.Y > 0 ? anchor / viewportSize : Vector2.Zero;
		int mouseCellX = (int)Math.Clamp(relativeMouse.X * zoomedWindowWidth + zoomedWindowOrigin.Item1, 0, gridWidth - 1);
		int mouseCellY = (int)Math.Clamp(relativeMouse.Y * zoomedWindowHeight + zoomedWindowOrigin.Item2, 0, gridHeight - 1);

		float nextZoomFactor = Math.Clamp(zoomFactor / 1.2f, 1.0f, 10.0f);
		zoomFactor = nextZoomFactor;
		cellSize = new Vector2(
			Math.Clamp(baseCellSize.X * zoomFactor, 1f, 100f),
			Math.Clamp(baseCellSize.Y * zoomFactor, 1f, 100f)
		);
		updateZoom();

		int desiredOriginX = mouseCellX - (int)(relativeMouse.X * zoomedWindowWidth);
		int desiredOriginY = mouseCellY - (int)(relativeMouse.Y * zoomedWindowHeight);
		zoomedWindowOrigin = (
			Math.Clamp(desiredOriginX, 0, Math.Max(0, gridWidth - zoomedWindowWidth)),
			Math.Clamp(desiredOriginY, 0, Math.Max(0, gridHeight - zoomedWindowHeight))
		);
		LazyRefreshGridTextures();
	}

	//Those inputs are always called
	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey eventKey && eventKey.Pressed && !eventKey.Echo)
		{
			if (eventKey.Keycode == Key.Space)
			{
				pendingSingleSteps++;
				return;
			}

			if (eventKey.Keycode == Key.KpAdd || eventKey.Keycode == Key.Equal || eventKey.Keycode == Key.Plus)
			{
				zoomIn();
				return;
			}

			if (eventKey.Keycode == Key.KpSubtract || eventKey.Keycode == Key.Minus)
			{
				zoomOut();
				return;
			}
		}

		if (@event is InputEventMouseButton { Pressed: false } eventMouseButton)
		{
			switch (eventMouseButton.ButtonIndex)
			{
				case MouseButton.Left:
					if (_drawingState == DrawingState.Drawing)
					{
						_drawingState = DrawingState.None;
					}
					break;
				case MouseButton.Right:
					if (_drawingState == DrawingState.Erasing)
					{
						_drawingState = DrawingState.None;
					}
					break;
				case MouseButton.Middle:
					string cellInfo = GetCellInfoAtCursor();
					GD.Print(cellInfo);
					break;
			}
		}
	}

	private void UiHandler()
	{
		foreach (BaseButton button in buttonGroup.GetButtons())
		{
			if (button.ButtonPressed)
			{
				selectedElement = (string)button.GetMeta("element");
				break;
			}
		}

		gameSpeed = (float)gameSpeedSlider.Value;

		brushSize = (int)brushSizeSlider.Value;

		((Label)GetNode("%InspectLabel")).Text = GetCellInfoAtCursor();
		((Label)GetNode("%WetnessLabel")).Text = $"Total Wetness: {GetTotalWetness():F2}";
		((Label)GetNode("%NutrientLabel")).Text = $"Total Nutrient: {GetTotalNutrient():F2}";
	}

	private void PlacementHandler()
	{
		if (_drawingState != DrawingState.None)
		{
			Vector2 pos = GetLocalGridMousePosition();
			int xStart = Math.Clamp((int)pos.X - brushSize / 2, 0, gridWidth);
			int xStop = Math.Clamp((int)pos.X + brushSize / 2 + brushSize % 2, 0, gridWidth);
			int yStart = Math.Clamp((int)pos.Y - brushSize / 2, 0, gridWidth);
			int yStop = Math.Clamp((int)pos.Y + brushSize / 2 + brushSize % 2, 0, gridHeight);

			for (int x = xStart; x < xStop; x++)
			{
				for (int y = yStart; y < yStop; y++)
				{
					if (_drawingState == DrawingState.Erasing)
					{
						elementArray[x, y] = null;
						UpdateManager.Instance.UpdateNearbyCellsNextFrame(x, y, gridWidth, gridHeight); // request an update for the newly deleted element
						MarkCellTextureDirty(x, y);
						continue;
					}
					switch (selectedElement) // ugly but was the only thing on my mind
					{

						case "Nutrient":
							if (elementArray[x, y] is Soil soil)
							{
								soil.nutrient = Math.Min(soil.nutrient + 1f, soil.maxNutrient);
								UpdateManager.Instance.UpdateNearbyCellsNextFrame(x, y, gridWidth, gridHeight); // request an update for the newly added nutrient
								MarkCellTextureDirty(x, y);
							}
							break;

						case "Fire":
							if (elementArray[x, y] is IFlammable)
							{
								FireManager.Instance.RequestIgnition(x, y, gridWidth, gridHeight);
							}
							break;

						default:
							createElement(x, y, selectedElement);
							break;
					}
				}
			}
		}
	}

	private bool IsSingleClickElement(string elementName)
	{
		return System.Array.Exists(singleClickElements, element => element == elementName); // found on stackoverflow
	}

	private void PlaceSingleElement()
	{
		Vector2 pos = GetLocalGridMousePosition();
		int x = Math.Clamp((int)pos.X, 0, gridWidth - 1);
		int y = Math.Clamp((int)pos.Y, 0, gridHeight - 1);

		// Handle special cases for single-click elements
		switch (selectedElement)
		{
			default:
				// Only place if the cell is empty or we're explicitly replacing
				if (elementArray[x, y] == null)
				{
					createElement(x, y, selectedElement);
					MarkCellTextureDirty(x, y);
				}
				break;
		}
	}

	private void createElement(int x, int y, string elementType)
	{
		switch (elementType)
		{
			case "Water":
				elementArray[x, y] = new Water();
				break;
			case "Steam":
				elementArray[x, y] = new Steam(1.0f);
				break;
			case "Seed":
				elementArray[x, y] = new Seed(5,5);
				break;
			default:
				elementArray[x, y] = (Element)Activator.CreateInstance(Type.GetType(elementType));
				break;
		}

		UpdateManager.Instance.UpdateNearbyCellsNextFrame(x, y, gridWidth, gridHeight); // request an update for the newly created element
		MarkCellTextureDirty(x, y);
	}

	private void createElementWithState(int x, int y, string elementType, string state)
	{
		elementArray[x, y] = (Element)Activator.CreateInstance(Type.GetType(elementType));
		elementArray[x, y].setState(state);

		UpdateManager.Instance.UpdateNearbyCellsNextFrame(x, y, gridWidth, gridHeight); // request an update for the newly created element
		MarkCellTextureDirty(x, y);
	}

	private void CellUpdateHandler()
	{
		
		Element[,] oldGrid = (Element[,])elementArray.Clone();
		(int, int)[] cellsToUpdate = UpdateManager.Instance.GetUpdateRequests();
		HashSet<(int, int)> previousNeedUpdateCells = activeNeedUpdateCells;
		UpdateManager.Instance.ClearUpdateRequests();
		MarkTextureDirty(cellsToUpdate);

		// Process elements in random order
		foreach ((int x, int y) in cellsToUpdate)
		{
			if (oldGrid[x, y] == null) continue;
			oldGrid[x, y].update(oldGrid, x, y, gridWidth, gridHeight, tick);
		}

		GridManager.Instance.ProcessDeletions(elementArray, gridWidth, gridHeight);
		GridManager.Instance.ProcessSpawns(elementArray, gridWidth, gridHeight);
		NutrientManager.Instance.ProcessNutrientRequests(oldGrid, elementArray, gridWidth, gridHeight);
		NutrientManager.Instance.ProcessWetnessRequests(oldGrid, elementArray, gridWidth, gridHeight);
		FireManager.Instance.ProcessIgnitionRequests(elementArray, gridWidth, gridHeight);
		MoveManager.Instance.ProcessMoveRequests(oldGrid, elementArray, gridWidth, gridHeight);
		HashSet<(int, int)> nextNeedUpdateCells = UpdateManager.Instance.getHashSet();
		activeNeedUpdateCells = nextNeedUpdateCells;
	}

	private void SetupGridRenderer()
	{
		if (gridRenderer != null)
		{
			gridRenderer.QueueFree();
			gridRenderer = null;
		}

		gridColorImage = Image.CreateEmpty(gridWidth, gridHeight, false, Image.Format.Rgba8);
		gridColorTexture = ImageTexture.CreateFromImage(gridColorImage);

		gridShaderMaterial = new ShaderMaterial();
		gridShaderMaterial.Shader = GD.Load<Shader>("res://CA-engine/grid_renderer.gdshader");
		gridShaderMaterial.SetShaderParameter("grid_size", gridSize);
		gridShaderMaterial.SetShaderParameter("cell_size", cellSize);
		gridShaderMaterial.SetShaderParameter("color_texture", gridColorTexture);
		gridShaderMaterial.SetShaderParameter("zoomed_window_origin", new Vector2(zoomedWindowOrigin.Item1, zoomedWindowOrigin.Item2));
		gridShaderMaterial.SetShaderParameter("zoomed_window_size", new Vector2(zoomedWindowWidth, zoomedWindowHeight));

		gridRenderer = new ColorRect
		{
			Name = "GridRenderer",
			Material = gridShaderMaterial,
			Position = Vector2.Zero,
			Size = GetViewportRect().Size,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			FocusMode = Control.FocusModeEnum.None,
			ZIndex = 1 // if the shader is not rendering, make sure this is above the UI elements
		};

		AddChild(gridRenderer);
		MoveChild(gridRenderer, 0);
	}

	private void LazyRefreshGridTextures()
	{
		if (gridColorImage == null || gridShaderMaterial == null)
		{
			return;
		}

		if (textureDirtyCells.Count > 0)
		{
			foreach ((int x, int y) in textureDirtyCells)
			{
				Element cell = elementArray[x, y];
				if (cell == null)
				{
					gridColorImage.SetPixel(x, y, new Color(0, 0, 0, 0));
					continue;
				}

				Color cellColor = cell.color;
				gridColorImage.SetPixel(x, y, cellColor);
			}
			gridColorTexture.Update(gridColorImage);
			textureDirtyCells.Clear();
		}

		gridShaderMaterial.SetShaderParameter("grid_size", gridSize);
		gridShaderMaterial.SetShaderParameter("cell_size", cellSize);
		gridShaderMaterial.SetShaderParameter("zoomed_window_origin", new Vector2(zoomedWindowOrigin.Item1, zoomedWindowOrigin.Item2));
		gridShaderMaterial.SetShaderParameter("zoomed_window_size", new Vector2(zoomedWindowWidth, zoomedWindowHeight));
		gridRenderer.Size = GetViewportRect().Size;
	}

	private void AdvanceSimulationStep()
	{
		CellUpdateHandler();
		tick++;
	}

	private void MarkCellTextureDirty(int x, int y)
	{
		textureDirtyCells.Add((x, y));
	}

	private void MarkTextureDirty(IEnumerable<(int, int)> cells)
	{
		foreach ((int x, int y) in cells)
		{
			textureDirtyCells.Add((x, y));
		}
	}

	public void SaveGridToFile(string fileName)
	{
		using (StreamWriter writer = new StreamWriter(fileName))
		{
			// Write header line
			writer.WriteLine(gridWidth + " " + gridHeight + " " + cellWidth + " " + cellHeight);

			// Write rest of file
			for (int x = 0; x < gridWidth; x++)
			{
				for (int y = 0; y < gridHeight; y++)
				{
					if (elementArray[x, y] != null)
					{
						// Write element and state (if exists)
						string elementStoredText = elementArray[x, y].GetType().ToString();
						string elementState = elementArray[x, y].getState();
						if (elementState != null) elementStoredText += "|" + elementState;
						elementStoredText += " ";
						writer.Write(elementStoredText);
					}
					else writer.Write("- ");
				}
				writer.Write("\n");
			}
		}
	}

	public void LoadGridFromFile(string fileName)
	{
		if (File.Exists(fileName))
		{
			// Store each line in array of strings
			string[] lines = File.ReadAllLines(fileName);

			string[] header = lines[0].Split(" ", false);
			if (header.Length < 4) throw new DataException("The file header isn't formatted correctly");

			// Initializing every variable according to the headers
			gridWidth = header[0].ToInt();
			gridHeight = header[1].ToInt();
			gridSize = new Vector2(gridWidth, gridHeight);
			cellWidth = header[2].ToInt();
			cellHeight = header[3].ToInt();
			cellSize = new Vector2(cellWidth, cellHeight);

			elementArray = new Element[gridWidth, gridHeight];
			SetupGridRenderer();
			textureDirtyCells.Clear();
			activeNeedUpdateCells.Clear();
			UpdateManager.Instance.ClearUpdateRequests();

			// Read rest of file
			if (lines.Length < gridWidth) throw new DataException("The file doesn't have the correct amount of rows");

			for (int x = 0; x < gridWidth; x++)
			{
				string[] line = lines[x + 1].Split(" ", false);
				if (line.Length < gridHeight)
					throw new DataException("The file doesn't have the correct amount of lines on row " + x + " : " + line.Length + " instead of " + gridHeight);
				for (int y = 0; y < gridHeight; y++)
				{
					// If element non null
					if (line[y] != "-")
					{
						// If has state
						if (line[y].Contains("|"))
						{
							string[] storedElement = line[y].Split("|");
							createElementWithState(x, y, storedElement[0], storedElement[1]);
						}
						else createElement(x, y, line[y]);
					}
				}
			}

			LazyRefreshGridTextures();
		}
	}

	private Vector2 GetLocalGridMousePosition()
	{
		Vector2 viewportSize = GetViewportRect().Size;
		Vector2 viewportMouse = GetViewport().GetMousePosition();
		Vector2 visibleCellSize = new Vector2(
			Math.Max(1f, viewportSize.X / Math.Max(1f, zoomedWindowWidth)),
			Math.Max(1f, viewportSize.Y / Math.Max(1f, zoomedWindowHeight))
		);

		Vector2 relativeMouse = viewportMouse / viewportSize;
		return new Vector2(
			(relativeMouse.X * zoomedWindowWidth) + zoomedWindowOrigin.Item1,
			(relativeMouse.Y * zoomedWindowHeight) + zoomedWindowOrigin.Item2
		);
	}

	private (int, int) GetGridPositionFromMouse()
	{
		Vector2 gridPos = GetLocalGridMousePosition();

		int x = (int)gridPos.X;
		int y = (int)gridPos.Y;

		x = Math.Clamp(x, 0, gridWidth - 1);
		y = Math.Clamp(y, 0, gridHeight - 1);

		return (x, y);
	}

	public string GetCellInfoAtCursor()
	{
		(int x, int y) = GetGridPositionFromMouse();
		// Check if cursor is within grid bounds
		if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight)
		{
			return "Cursor outside grid bounds";
		}

		Element cell = elementArray[x, y];

		if (cell == null)
		{
			return $"Position ({x}, {y}): Empty cell";
		}

		// Get the class name
		string className = cell.GetType().Name;

		// Build attribute string
		string attributes = $"Position ({x}, {y}): {className}\n";
		attributes += cell.inspectInfo();

		return attributes.StripEdges();
	}

	public float GetTotalWetness()
	{
		float totalWetness = 0f;

		for (int x = 0; x < gridWidth; x++)
		{
			for (int y = 0; y < gridHeight; y++)
			{
				if (elementArray[x, y] is ILife element)
				{
					totalWetness += element.wetness;

				}
				else if (elementArray[x, y] is Water water)
				{
					totalWetness += water.wetness;
				}
				else if (elementArray[x, y] is Steam steam)
				{
					totalWetness += steam.wetness;
				}
			}
		}
		return totalWetness;
	}

	public float GetTotalNutrient()
	{
		float totalNutrient = 0f;

		for (int x = 0; x < gridWidth; x++)
		{
			for (int y = 0; y < gridHeight; y++)
			{
				if (elementArray[x, y] is ILife nutrient)
				{
					totalNutrient += nutrient.nutrient;
				}
			}
		}

		return totalNutrient;
	}

	public void _on_skip_time_button_pressed()
	{
		for (int i = 0; i < 1000; i++)
		{
			AdvanceSimulationStep();
		}
		LazyRefreshGridTextures();
	}

	private enum DrawingState
	{
		None,
		Drawing,
		Erasing
	}

}
