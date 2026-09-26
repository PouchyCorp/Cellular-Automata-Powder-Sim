using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
public class Snail : Element, ILife, ISolid
{
	private int moveInterval = 30; // ticks
	private int lastMoveTick = 0;
	private List<(int, int)> lastPositions = new List<(int, int)>(); // to avoid going back and forth

	// Infinite storage buffers for consumed nutrients and wetness
	private float storedNutrient = 0.0f;
	private float storedWetness = 0.0f;

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
		ashCreationPercentage = 0.0f;
		density = 60;
		color = Colors.Beige;
		flammability = 3;
	}
	public override void update(Element[,] oldElementArray, Element[,] currentElementArray, int x, int y, int maxX, int maxY, int T)
	{
		// Handle eating cooldown
		if (eatingCooldown > 0)
		{
			eatingCooldown--;
			burn(oldElementArray, currentElementArray, x, y, maxX, maxY, T);
			updateColor(T);
			return;
		}

		// Check for adjacent surface biomass to eat
		if (checkAndEatSurfaceBiomass(oldElementArray, currentElementArray, x, y, maxX, maxY))
		{
			snailState = SnailState.Eating;
			eatingCooldown = EATING_WAIT_TIME;
			burn(oldElementArray, currentElementArray, x, y, maxX, maxY, T);
			updateColor(T);
			return;
		}

		// Transfer nutrients to soil if available
		transferNutrientsToSoil(currentElementArray, x, y, maxX, maxY);

		// Validate current state - if we're not falling but have no solid surface, start falling
		if (snailState != SnailState.Falling && !hasAdjacentSolidSurface(x, y, oldElementArray, maxX, maxY))
		{
			snailState = SnailState.Falling;
		}

		switch (snailState)
		{
			case SnailState.Falling:
				handleFallingState(oldElementArray, currentElementArray, x, y, maxX, maxY, T);
				break;
			case SnailState.Idle:
				handleIdleState(oldElementArray, currentElementArray, x, y, maxX, maxY, T);
				break;
			case SnailState.Moving:
				handleMovingState(oldElementArray, currentElementArray, x, y, maxX, maxY, T);
				break;
			case SnailState.Eating:
				// Already handled above
				snailState = SnailState.Idle;
				break;
		}

		burn(oldElementArray, currentElementArray, x, y, maxX, maxY, T);
		updateColor(T);
	}

	private bool checkAndEatSurfaceBiomass(Element[,] oldElementArray, Element[,] currentElementArray, int x, int y, int maxX, int maxY)
	{
		// Check all 8 adjacent cells for surface biomass
		for (int nx = Math.Max(0, x - 1); nx <= Math.Min(x + 1, maxX - 1); nx++)
		{
			for (int ny = Math.Max(0, y - 1); ny <= Math.Min(y + 1, maxY - 1); ny++)
			{
				if (nx == x && ny == y) continue;

				if (oldElementArray[nx, ny] is SurfBiomass surfBiomass)
				{
					// Eat the surface biomass
					storedNutrient += surfBiomass.nutrient;
					storedWetness += surfBiomass.wetness;

					// Remove the surface biomass and move to its location
					currentElementArray[nx, ny] = this;
					currentElementArray[x, y] = null;

					return true;
				}
			}
		}
		return false;
	}

	private void transferNutrientsToSoil(Element[,] currentElementArray, int x, int y, int maxX, int maxY)
	{
		if (storedNutrient <= 0 && storedWetness <= 0) return;

		// Check if snail is on soil
		Element below = (y + 1 < maxY) ? currentElementArray[x, y + 1] : null;
		if (below is Soil soil)
		{
			soil.ChangeNutrient(storedNutrient, currentElementArray, x, y + 1, maxX, maxY);
			float transferableWetness = Math.Min(storedWetness, 1f - soil.wetness); // ensure soil wetness does not exceed 1
																					// for now, allow snail to transfer more wetness than it has stored (to be balanced later if needed)
			soil.ChangeWetness(transferableWetness, currentElementArray, x, y + 1, maxX, maxY);
			storedNutrient = 0;
			storedWetness = -transferableWetness;
			return;
		}
	}

	private void handleFallingState(Element[,] oldElementArray, Element[,] currentElementArray, int x, int y, int maxX, int maxY, int T)
	{
		// Check if we can continue falling down
		bool canFall = y + 1 < maxY && (currentElementArray[x, y + 1] == null || currentElementArray[x, y + 1] is ILiquid);

		if (canFall)
		{
			// Continue falling
			currentElementArray[x, y + 1] = this;
			currentElementArray[x, y] = null;
		}
		else
		{
			// Can't fall anymore, check if we have solid surfaces to climb on
			if (hasAdjacentSolidSurface(x, y, oldElementArray, maxX, maxY))
			{
				snailState = SnailState.Idle; // Found solid surface, can climb
			}
			else
			{
				// No solid surface to climb on, try to find one by checking nearby
				bool foundSurface = false;
				for (int nx = Math.Max(0, x - 1); nx <= Math.Min(x + 1, maxX - 1); nx++)
				{
					for (int ny = Math.Max(0, y - 1); ny <= Math.Min(y + 1, maxY - 1); ny++)
					{
						if (nx == x && ny == y) continue;

						Element target = currentElementArray[nx, ny];
						if ((target == null || target is ILiquid) && hasAdjacentSolidSurface(nx, ny, oldElementArray, maxX, maxY))
						{
							// Found a valid position to move to
							currentElementArray[nx, ny] = this;
							currentElementArray[x, y] = null;
							snailState = SnailState.Idle;
							foundSurface = true;
							break;
						}
					}
					if (foundSurface) break;
				}

				if (!foundSurface)
				{
					// Still no solid surface, remain in falling state but don't move
					snailState = SnailState.Idle; // Give up and become idle
				}
			}
		}
	}

	private void handleIdleState(Element[,] oldElementArray, Element[,] currentElementArray, int x, int y, int maxX, int maxY, int T)
	{
		// First check if we should be falling instead of being idle
		if (!hasAdjacentSolidSurface(x, y, oldElementArray, maxX, maxY))
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

	private void handleMovingState(Element[,] oldElementArray, Element[,] currentElementArray, int x, int y, int maxX, int maxY, int T)
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

				Element target = oldElementArray[nx, ny];

				// Can only move to empty spaces or through webs
				if (target == null || target is Web || target is ILiquid)
				{
					// CRITICAL: Must have at least one solid neighbor to climb on
					if (hasAdjacentSolidSurface(nx, ny, oldElementArray, maxX, maxY) && !lastPositions.Contains((nx, ny)))
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
		int randomIndex = rng.RandiRange(0, availableCells.Count - 1);
		(int, int) targetCell = availableCells[randomIndex];

		// Move to the target cell
		int newX = targetCell.Item1;
		int newY = targetCell.Item2;

		// Destroy web if moving through one
		if (oldElementArray[newX, newY] is Web)
		{
			// Web is destroyed, snail moves through
		}


		currentElementArray[x, y] = currentElementArray[newX, newY];
		currentElementArray[newX, newY] = this;
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
			if (neighbor != null && !(neighbor is ILiquid) && !(neighbor is Web) && !(neighbor is IGas)
				&& !(neighbor is Snail) && !(neighbor is Fly))
			{
				return true; // Found a solid surface that can support climbing
			}
		}
		return false;
	}

	public override string inspectInfo()
	{
		return base.inspectInfo() + $"\nState: {snailState}\n" +
			$"Stored Nutrient: {storedNutrient:F3}\n" +
			$"Stored Wetness: {storedWetness:F3}\n" +
			$"Eating Cooldown: {eatingCooldown}\n";
	}

	public override string getState()
	{
		return base.getState() + ";" +
			(int) snailState + ";" +
			storedNutrient + ";" +
			storedWetness + ";" +
			eatingCooldown + ";" +
			lastMoveTick + ";";
	}

	public override int setState(string state)
	{
		int i = base.setState(state);
		string[] stateArgs = state.Split(";", false);
		snailState = (SnailState)stateArgs[i++].ToInt();
		storedNutrient = stateArgs[i++].ToFloat();
		storedWetness = stateArgs[i++].ToFloat();
		eatingCooldown = stateArgs[i++].ToInt();
		return i;
	}

}
