using Godot;
using System;
using System.Data;
using System.IO;
using System.Linq.Expressions;
public partial class CellularAutomataEngine : Node2D
{

	// --- Private element instantiation --- //
	private Element[,] elementArray;
	private int cellWidth;
	private int cellHeight;
	private int gridWidth;
	private int gridHeight;

	private DrawingState _drawingState = DrawingState.None;

	// Elements that should only be placed once per click, not continuously
	private readonly string[] singleClickElements = {"Seed", "Worm", "Snail"};

	private ButtonGroup buttonGroup;
	public string selectedElement; // TODO idk how to do differently

	private Slider gameSpeedSlider;
	public float gameSpeed = 1;

	private Slider brushSizeSlider;
	public int brushSize = 1;
	float gameSpeedCounter = 0f;
	public int tick = 0;


	// --- Public (exported) element instantiation --- //
	[ExportCategory("Simulation Size")]
	[Export]
	public Vector2 cellSize { get; set; } = new Vector2(2*2, 2*2);
	[Export]
	public Vector2 gridSize { get; set; } = new Vector2(288, 162);

	// --- Methods --- //
	public override void _Ready()
	{
		base._EnterTree();
		cellWidth = (int)cellSize.X;
		cellHeight = (int)cellSize.Y;
		gridWidth = (int)gridSize.X;
		gridHeight = (int)gridSize.Y;

		Button firstButton = GetNode<Button>("%Sand");
		buttonGroup = firstButton.ButtonGroup;

		brushSizeSlider = GetNode<Slider>("%BrushSize");
		gameSpeedSlider = GetNode<Slider>("%GameSpeed");

		elementArray = new Element[gridWidth, gridHeight];
	}


	public override void _Draw()
	{
		base._Draw();

		// Draw outline
		DrawRect(new Rect2(Vector2.Zero, cellSize * gridSize), Colors.SlateGray, false);

		// Draw every cell
		Rect2 cellRect = new Rect2(Vector2.Zero, cellSize);
		for (int x = 0; x < gridWidth; x++)
		{
			for (int y = 0; y < gridHeight; y++)
			{
				if (elementArray[x, y] != null)
				{
					// If you want to override to have a draw method inside the Element Class, you can,
					// But I am concerned with slight optimisation issues tho
					// Making a shader could be a better solution
					cellRect.Position = cellSize * new Vector2(x, y);
					DrawRect(cellRect, elementArray[x, y].color);
				}
			}
		}
	}

	public override void _Process(double delta)
	{
		GD.Print($"Processing frame at tick {tick}.");
		tick ++;
		base._Process(delta);
		UiHandler();
		PlacementHandler();
		gameSpeedCounter += gameSpeed;
		while (Math.Floor(gameSpeedCounter) > 0) // game speed just skips steps
		{
			CellUpdateHandler();
			gameSpeedCounter--;
		}

		QueueRedraw();
		GD.Print($"Finished tick {tick}.");
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
			}
		}
	}

	//Those inputs are always called
	public override void _Input(InputEvent @event)
	{
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
			Vector2 pos = GetViewport().GetMousePosition() / cellSize;
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
						continue;
					}
					switch (selectedElement) // ugly but was the only thing on my mind
					{

						case "Nutrient":
							if (elementArray[x, y] is Soil soil)
							{
								soil.nutrient = Math.Min(soil.nutrient + 1f, soil.maxNutrient);
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
		Vector2 pos = GetViewport().GetMousePosition() / cellSize;
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
			default:
				elementArray[x, y] = (Element)Activator.CreateInstance(Type.GetType(elementType));
				break;
		}
	}

	private void createElement(int x, int y, string elementType, string state)
	{
		elementArray[x, y] = (Element)Activator.CreateInstance(Type.GetType(elementType));
		elementArray[x, y].setState(state);
	}

	private void CellUpdateHandler()
	{
		Element[,] oldGrid = (Element[,])elementArray.Clone();

		// Process elements in random order
		for (int x = 0; x < gridWidth; x++)
		{
			for (int y = 0; y < gridHeight; y++)
			{
				if (oldGrid[x, y] == null) continue;

				oldGrid[x, y].update(oldGrid, x, y, gridWidth, gridHeight, tick);
			}
		}

		GridManager.Instance.ProcessDeletions(elementArray, gridWidth, gridHeight);
		GridManager.Instance.ProcessSpawns(elementArray, gridWidth, gridHeight);
		NutrientManager.Instance.ProcessNutrientRequests(oldGrid, elementArray);
		NutrientManager.Instance.ProcessWetnessRequests(oldGrid, elementArray);
		FireManager.Instance.ProcessIgnitionRequests(elementArray, gridWidth, gridHeight);
		MoveManager.Instance.ProcessMoveRequests(oldGrid, elementArray, gridWidth, gridHeight);
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
							createElement(x, y, storedElement[0], storedElement[1]);
						}
						else createElement(x, y, line[y]);
					}
				}
			}
		}
	}

	public string GetCellInfoAtCursor()
	{
		Vector2 mousePos = GetViewport().GetMousePosition();
		Vector2 gridPos = mousePos / cellSize;
		
		int x = (int)gridPos.X;
		int y = (int)gridPos.Y;
		
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
		for (int i = 0; i < 10000; i++)
		{
			CellUpdateHandler();
		}
	}
	
	private enum DrawingState
	{
		None,
		Drawing,
		Erasing
	}

}
