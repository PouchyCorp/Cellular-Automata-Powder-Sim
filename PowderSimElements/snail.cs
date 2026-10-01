using Godot;
using System;
using System.Collections.Generic;
public class Snail : Element, ILife, ISolid
{
	private int moveInterval = 30; // ticks
	private int lastMoveTick = 0;
	private List<(int, int)> lastPositions = new List<(int, int)>(); // to avoid going back and forth

	public float maxNutrient => 100000000000.0f;
	public float nutrient = 1.0f;
	public float wetness = 0.0f;
	public float maxWetness => 10000000000.0f;

	// Eating behavior
	private int eatingCooldown = 0;
	private const int EATING_WAIT_TIME = 10; // frames to wait after eating

	enum SnailState
	{
		Falling,
		Moving,
		Idle,
		Eating
	}
	private SnailState snailState = SnailState.Falling;
	public Snail()
	{
		density = 60;
		color = Colors.Beige;
	}
	public override void update(Element[,] oldGrid, int x, int y, int maxX, int maxY, int T)
	{
		// Handle eating cooldown
		if (eatingCooldown > 0)
		{
			eatingCooldown--;
			updateColor(T, x, y);
			return;
		}

		// Check for adjacent surface biomass to eat
		if (checkAndEatSurfaceBiomass(oldGrid, x, y, maxX, maxY))
		{
			snailState = SnailState.Eating;
			eatingCooldown = EATING_WAIT_TIME;
			updateColor(T, x, y);
			return;
		}

		// Transfer nutrients to soil if available
		transferNutrientsToSoil(oldGrid, x, y, maxX, maxY);

		// Validate current state - if we're not falling but have no solid surface, start falling
		if (snailState != SnailState.Falling && !hasAdjacentSolidSurface(x, y, oldGrid, maxX, maxY))
		{
			snailState = SnailState.Falling;
		}

		switch (snailState)
		{
			case SnailState.Falling:
				handleFallingState(oldGrid, x, y, maxX, maxY, T);
				break;
			case SnailState.Idle:
				handleIdleState(oldGrid, x, y, maxX, maxY, T);
				break;
			case SnailState.Moving:
				handleMovingState(oldGrid, x, y, maxX, maxY, T);
				break;
			case SnailState.Eating:
				// Already handled above
				snailState = SnailState.Idle;
				break;
		}

		updateColor(T, x, y);
	}

	private bool checkAndEatSurfaceBiomass(Element[,] oldGrid, int x, int y, int maxX, int maxY)
	{
		// Check all 8 adjacent cells for surface biomass
		for (int nx = Math.Max(0, x - 1); nx <= Math.Min(x + 1, maxX - 1); nx++)
		{
			for (int ny = Math.Max(0, y - 1); ny <= Math.Min(y + 1, maxY - 1); ny++)
			{
				if (nx == x && ny == y) continue;

				if (oldGrid[nx, ny] is SurfBiomass surfBiomass)
				{
					// Eat the surface biomass
					NutrientManager.Instance.AddTakeNutrientRequest(new TakeNutrientRequest(x, y, x, y, surfBiomass.nutrient), maxX, maxY);
					NutrientManager.Instance.AddTakeNutrientRequest(new TakeNutrientRequest(x, y, x, y, surfBiomass.nutrient), maxX, maxY);
					return true;
				}
			}
		}
		return false;
	}

	private void transferNutrientsToSoil(Element[,] oldGrid, int x, int y, int maxX, int maxY)
	{
		if (nutrient <= 0 && wetness <= 0) return;

		// Check if snail is on soil
		Element below = (y + 1 < maxY) ? oldGrid[x, y + 1] : null;
		if (below is Soil soil)
		{
			NutrientManager.Instance.AddGiveNutrientRequest(new GiveNutrientRequest(x, y, x, y + 1, nutrient), maxX, maxY);
			NutrientManager.Instance.AddGiveWetnessRequest(new GiveWetnessRequest(x, y, x, y + 1, wetness), maxX, maxY);			
			return;
		}
	}

	private void handleFallingState(Element[,] oldGrid, int x, int y, int maxX, int maxY, int T)
	{
		// Check if we can continue falling down
		bool canFall = y + 1 < maxY;

		if (canFall)
		{
			Element below = oldGrid[x, y + 1];
			if (below is not ISolid)
			{
				MoveManager.Instance.AttemptMove(oldGrid, x, y, 0, 1, maxX, maxY);
				return;
			}
		}

		snailState = SnailState.Idle;
	}

	private void handleIdleState(Element[,] oldGrid, int x, int y, int maxX, int maxY, int T)
	{
		// First check if we should be falling instead of being idle
		if (!hasAdjacentSolidSurface(x, y, oldGrid, maxX, maxY))
		{
			snailState = SnailState.Falling;
			return;
		}

		if (T - lastMoveTick >= moveInterval)
		{
			snailState = SnailState.Moving;
			lastMoveTick = T;
		}
	}

	private void handleMovingState(Element[,] oldGrid, int x, int y, int maxX, int maxY, int T)
	{

		// small chance to stay idle instead of moving
		if (Random.Shared.NextSingle() < 0.02f)
		{
			snailState = SnailState.Idle;
			lastPositions.Clear(); // reset history when choosing to stay
			return;
		}



		// Get all available cells where snail can move (empty or webs only)
		var availableCells = new List<(int, int)>();

		for (int nx = Math.Max(0, x - 1); nx <= Math.Min(x + 1, maxX - 1); nx++)
		{
			for (int ny = Math.Max(0, y - 1); ny <= Math.Min(y + 1, maxY - 1); ny++)
			{
				if (nx == x && ny == y) continue;

				Element target = oldGrid[nx, ny];

				// Can only move to empty spaces or through webs
				if (target == null || target is Web || target is ILiquid)
				{
					// CRITICAL: Must have at least one solid neighbor to climb on
					if (hasAdjacentSolidSurface(nx, ny, oldGrid, maxX, maxY) && !lastPositions.Contains((nx, ny)))
					{
						availableCells.Add((nx, ny));
					}
				}
			}
		}

		if (availableCells.Count == 0)
		{
			// No valid moves available
			lastPositions.Clear(); // reset history when stuck


			snailState = SnailState.Idle; // couldn't move but still on solid surface
			return;
		}

		// Choose a random valid cell to move to
		int randomIndex = Random.Shared.Next(0, availableCells.Count - 1);
		(int, int) targetCell = availableCells[randomIndex];

		// Move to the target cell
		int newX = targetCell.Item1;
		int newY = targetCell.Item2;

		// Destroy web if moving through one
		if (oldGrid[newX, newY] is Web)
		{
			GridManager.Instance.RequestDeletion(newX, newY, maxX, maxY);
		}


		MoveManager.Instance.AttemptMove(oldGrid, x, y, newX - x, newY - y, maxX, maxY);
		lastPositions.Add((x, y));

		// Keep position history manageable
		if (lastPositions.Count > 5)
		{
			lastPositions.RemoveAt(0);
		}

		snailState = SnailState.Idle;
	}

	private bool hasAdjacentSolidSurface(int x, int y, Element[,] elementArray, int maxX, int maxY)
	{
		// Check if this position is adjacent to any solid surface that can support climbing
		// Similar to spider implementation
		// checks only cardinal directions
		foreach ((int dx, int dy) in new (int, int)[] { (-1, 0), (1, 0), (0, -1), (0, 1) })
		{
			int nx = x + dx;
			int ny = y + dy;
			if (nx < 0 || nx >= maxX || ny < 0 || ny >= maxY) continue;
			if (nx == x && ny == y) continue;

			Element neighbor = elementArray[nx, ny];
			if (neighbor is ISolid)
			{
				return true; // Found a solid surface that can support climbing
			}
		}
		return false;
	}

	public override string inspectInfo()
	{
		return base.inspectInfo() + $"\nState: {snailState}\n" +
			$"Stored Nutrient: {nutrient:F3}\n" +
			$"Stored Wetness: {wetness:F3}\n" +
			$"Eating Cooldown: {eatingCooldown}\n";
	}

	public override string getState()
	{
		return base.getState() + ";" +
			(int) snailState + ";" +
			nutrient + ";" +
			wetness + ";" +
			eatingCooldown + ";" +
			lastMoveTick + ";";
	}

	public override int setState(string state)
	{
		int i = base.setState(state);
		string[] stateArgs = state.Split(";", false);
		snailState = (SnailState)stateArgs[i++].ToInt();
		nutrient = stateArgs[i++].ToFloat();
		wetness = stateArgs[i++].ToFloat();
		eatingCooldown = stateArgs[i++].ToInt();
		return i;
	}

}
