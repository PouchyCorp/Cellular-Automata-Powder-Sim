using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

public class Spider : Element, ILife, ISolid
{
	public int sleepTime = 5; // ticks to sleep between actions, to slow down the spider
	private (int, int) buildingDirection;
	private Queue<(int, int)> recentPositions = new Queue<(int, int)>();
	private const int MAX_POSITION_HISTORY = 3; // Keep track of last 3 positions to prevent backtracking
	private int lastMeaningfulStateChangeTick = 0; // To prevent rapid state changes

	private int lastFlyEatTick = 0; // ticks until can eat another fly
	private int flyEatingCooldown = 60 * 60; // ticks between eating flies
	private Web onWeb; 
	private SpiderFSM currentState = SpiderFSM.FALLING;
	public enum SpiderFSM
	{
		FALLING,
		WANDERING_ON_WEB,
		WANDERING_TO_BUILD_SITE,
		BUILDING
	}
	public Spider() : base()
	{
		density = 10;
		color = Colors.Violet;
		flammability = 1f;
	}

	/// <summary>
	/// No bounds checking, make sure to call only with valid coordinates
	/// </summary>
	private bool isValidBuildDestination(int x, int y, Element[,] oldElementArray, int maxX, int maxY)
	{
		if (x == 0 || x == maxX - 1 || y == 0 || y == maxY - 1) return true; // bounds are valid
		if (oldElementArray[x, y] != null
		&& (oldElementArray[x, y] is Web
		|| oldElementArray[x, y] is ISolid)) return true; // valid solid cell or web
		return false;
	}
	private (int, int) getBuildDirection(int x, int y, Element[,] oldElementArray, int maxX, int maxY)
	{
		// Check all 8 directions for a solid cell to build from
		var directions = new (int, int)[]
		{
			(0, 1), (1, 1), (1, 0), (1, -1),
			(0, -1), (-1, -1), (-1, 0), (-1, 1)
		};

		// Raycast in all directions to see the first solid cell in valid range
		foreach ((int, int) dir in directions)
		{
			bool valid = true;
			int adjacentWebsAndSolids = 0;
			for (int dist = 1; dist <= 15; dist++) // max build distance of 15
			{
				int checkX = x + dir.Item1 * dist;
				int checkY = y + dir.Item2 * dist;
				if (checkX < 0 || checkX >= maxX || checkY < 0 || checkY >= maxY) break; // out of bounds
				if (dist < 4 && isValidBuildDestination(checkX, checkY, oldElementArray, maxX, maxY)) break;

				// Check for adjacent solids along the build path (perpendicular directions)
				int perpX = -dir.Item2, perpY = dir.Item1;

				// Check both perpendicular neighbors at this step
				for (int p = -1; p <= 1; p += 2)
				{
					int adjX = checkX + perpX * p;
					int adjY = checkY + perpY * p;
					if (adjX >= 0 && adjX < maxX && adjY >= 0 && adjY < maxY)
					{
						Element e = oldElementArray[adjX, adjY];
						if (e != null) // including webs
						{
							adjacentWebsAndSolids += 1;
						}
					}
				}
				if (adjacentWebsAndSolids > dist / 4) // allow 1 adjacent solid for every 4 distance units
				{
					valid = false; // too close to solid, can't build here
				}
				if (!valid) break;

				if (isValidBuildDestination(checkX, checkY, oldElementArray, maxX, maxY)) return dir; // found a valid build direction
			}
		}
		return (0, 0); // No valid direction found
	}

	private int getScore((int, int) direction, (int, int) wanderingDirection)
	{
		int maxDistance = 4;
		int alignmentCost = Math.Abs(direction.Item1 - wanderingDirection.Item1) + Math.Abs(direction.Item2 - wanderingDirection.Item2) * 2;
		return maxDistance - alignmentCost; // the more aligned with wandering direction, the higher the score
	}

	// Helper methods for position history management
	private void addToPositionHistory(int x, int y)
	{
		recentPositions.Enqueue((x, y));
		if (recentPositions.Count > MAX_POSITION_HISTORY)
		{
			recentPositions.Dequeue();
		}
	}

	private bool isRecentPosition(int x, int y)
	{
		return recentPositions.Contains((x, y));
	}

	private List<T> filterRecentPositions<T>(List<T> cells, System.Func<T, (int, int)> getPosition)
	{
		return cells.FindAll(cell => !isRecentPosition(getPosition(cell).Item1, getPosition(cell).Item2));
	}

	// State transition functions
	private void transitionToFalling(int T)
	{
		currentState = SpiderFSM.FALLING;
		lastMeaningfulStateChangeTick = T;
	}

	private void transitionToWanderingOnWeb(int T)
	{
		currentState = SpiderFSM.WANDERING_ON_WEB;
		lastMeaningfulStateChangeTick = T;
	}

	private void transitionToWanderingToBuildSite(int T)
	{
		currentState = SpiderFSM.WANDERING_TO_BUILD_SITE;
		lastMeaningfulStateChangeTick = T;
		// No directional preference - spider will move randomly while avoiding recent positions
	}

	private void transitionToBuilding(int T, (int, int) buildDir)
	{
		currentState = SpiderFSM.BUILDING;
		lastMeaningfulStateChangeTick = T;
		buildingDirection = buildDir;
	}

	private void handleFallingState(Element[,] oldElementArray, Element[,] currentElementArray, int x, int y, int maxX, int maxY, int T)
	{
		bool solidNeighbor = false;
		for (int nx = Math.Max(0, x - 1); nx <= Math.Min(x + 1, maxX - 1); nx++) // including diagonals
		{
			for (int ny = Math.Max(0, y - 1); ny <= Math.Min(y + 1, maxY - 1); ny++)
			{
				if ((nx, ny) == (x, y))
				{
					continue;
				}
				if (oldElementArray[nx, ny] != null && oldElementArray[nx, ny].density >= density && oldElementArray[nx, ny] is not Spider)
				{
					solidNeighbor = true;
					break;
				}
			}
			if (solidNeighbor) break;
		}
		if (solidNeighbor)
		{
			transitionToWanderingToBuildSite(T);
		}
		else
		{
			// Continue falling
			if (y + 1 < maxY && (currentElementArray[x, y + 1] == null || currentElementArray[x, y + 1] is Web))
			{
				specialMove(oldElementArray, currentElementArray, x, y, maxX, maxY, 0, 1);
				if (onWeb != null)
				{
					// Fell onto a web, transition to wandering on web
					transitionToWanderingOnWeb(T);
				}
			}
		}
	}

	private void handleWanderingOnWebState(Element[,] oldElementArray, Element[,] currentElementArray, int x, int y, int maxX, int maxY, int T)
	{
		if (T - lastMeaningfulStateChangeTick > 10 * 60 && Random.Shared.NextSingle() < 0.01f) // After 10 seconds, small chance to start wandering to build site
		{
			transitionToWanderingToBuildSite(T);
			return;
		}

		// Collect all adjacent web cells where we can move
		var availableWebCells = new List<(int, int)>();
		for (int nx = Math.Max(0, x - 1); nx <= Math.Min(x + 1, maxX - 1); nx++) // including diagonals
		{
			for (int ny = Math.Max(0, y - 1); ny <= Math.Min(y + 1, maxY - 1); ny++)
			{
				if ((nx, ny) == (x, y)) { continue; }

				if (oldElementArray[nx, ny] is Fly fly && T - lastFlyEatTick > flyEatingCooldown) // can eat a fly every so often
				{
					lastFlyEatTick = T;
					// eat the fly
					currentElementArray[nx, ny] = new Web();
				}

				// Check if there's a web in the old array (what we're reading from)
				if (oldElementArray[nx, ny] is Web web)
				{
					web.resetLifetime(); // the spider is taking care of adjacent webs (awww so cute)
										 // Check if the current array position is either empty or has a web (safe to move)
					Element currentTarget = currentElementArray[nx, ny];
					if (currentTarget == null || currentTarget is Web)
					{
						availableWebCells.Add((nx, ny));
					}
				}
			}
		}

		if (availableWebCells.Count == 0)
		{
			// No adjacent web found, start falling (epic fail)
			transitionToFalling(T);
			return;
		}

		if (Random.Shared.NextSingle() < 0.4f) // 40% chance to move each tick
		{
			// Avoid recent positions if we have other options
			var validCells = filterRecentPositions(availableWebCells, cell => cell);
			if (validCells.Count == 0)
			{
				validCells = availableWebCells; // If no other options, use all available cells
			}

			if (validCells.Count > 0)
			{
				int randomIndex = rng.RandiRange(0, validCells.Count - 1);
				(int, int) targetCell = validCells[randomIndex];
				specialMove(oldElementArray, currentElementArray, x, y, maxX, maxY, targetCell.Item1 - x, targetCell.Item2 - y);
			}
		}
	}

	private void handleWanderingToBuildSiteState(Element[,] oldElementArray, Element[,] currentElementArray, int x, int y, int maxX, int maxY, int T)
	{
		if (T - lastMeaningfulStateChangeTick > 15)
		{
			// The spider wandered enough, start building
			(int, int) buildDir = getBuildDirection(x, y, oldElementArray, maxX, maxY);
			if (buildDir != (0, 0))
			{
				transitionToBuilding(T, buildDir);
			}
			else
			{
				transitionToWanderingToBuildSite(T); // No valid build direction, restart wandering
			}
			return;
		}

		// get all available cells with webs or that are empty
		var availableCellsList = new List<(int, int)>();
		for (int nx = Math.Max(0, x - 1); nx <= Math.Min(x + 1, maxX - 1); nx++)
		{
			for (int ny = Math.Max(0, y - 1); ny <= Math.Min(y + 1, maxY - 1); ny++)
			{
				if (oldElementArray[nx, ny] is Web || (oldElementArray[nx, ny] == null))
				{
					availableCellsList.Add((nx, ny));
				}
			}
		}
		// remove non web cells that are not adjacent to solid cells
		foreach ((int, int) cell in availableCellsList.ToArray()) // iterate over a copy since we may remove items
		{
			if (oldElementArray[cell.Item1, cell.Item2] == null)
			{
				bool solidNeighbor = false;
				// for each empty cell, check if it has a solid neighbor to climb on, otherwise remove it from the list
				for (int nnx = Math.Max(0, cell.Item1 - 1); nnx <= Math.Min(cell.Item1 + 1, maxX - 1); nnx++) // including diagonals
				{
					for (int nny = Math.Max(0, cell.Item2 - 1); nny <= Math.Min(cell.Item2 + 1, maxY - 1); nny++)
					{
						if ((nnx, nny) == (cell.Item1, cell.Item2)) continue;
						if (oldElementArray[nnx, nny] != null && oldElementArray[nnx, nny].density > density)
						{
							solidNeighbor = true; // found a solid neighbor, keep this cell
							break;
						}
					}
					if (solidNeighbor) break;
				}
				if (!solidNeighbor)
				{
					availableCellsList.Remove(cell);
				}
			}
		}

		if (availableCellsList.Count == 0)
		{
			// No available cells, start falling, poor thing :(
			transitionToFalling(T);
			return;
		}

		// Select next cell randomly while avoiding recent positions
		if (availableCellsList.Count == 1)
		{
			(int, int) nextCell = availableCellsList[0];
			move(oldElementArray, currentElementArray, x, y, maxX, maxY, nextCell.Item1 - x, nextCell.Item2 - y);
			return;
		}

		// Prefer cells that are not in recent position history
		var validCells = filterRecentPositions(availableCellsList, cell => cell);
		if (validCells.Count == 0)
		{
			validCells = availableCellsList; // If all cells are recent, use them anyway
		}
		
		// Choose completely randomly from valid cells
		int randomIndex = rng.RandiRange(0, validCells.Count - 1);
		(int, int) bestCell = validCells[randomIndex];

		specialMove(oldElementArray, currentElementArray, x, y, maxX, maxY, bestCell.Item1 - x, bestCell.Item2 - y);
	}

	private void handleBuildingState(Element[,] oldElementArray, Element[,] currentElementArray, int x, int y, int maxX, int maxY, int T)
	{
		// Place a web in the building direction if the cell is empty
		int targetX = x + buildingDirection.Item1;
		int targetY = y + buildingDirection.Item2;
		if (targetX < 0 || targetX >= maxX || targetY < 0 || targetY >= maxY)
		{
			// Out of bounds, stop building and transition to wandering on web
			transitionToWanderingOnWeb(T);
			return;
		}
		if (oldElementArray[targetX, targetY] != null)
		{
			// Can't build there anymore, transition to wandering on web
			transitionToWanderingOnWeb(T);
			return;
		}
		Web web = new Web();
		currentElementArray[targetX, targetY] = web;

		// Move onto the newly built web
		bool moveSuccess = specialMove(oldElementArray, currentElementArray, x, y, maxX, maxY, buildingDirection.Item1, buildingDirection.Item2);

		if (!moveSuccess)
		{
			// If we couldn't move, transition to wandering on web anyway
			transitionToWanderingOnWeb(T);
			return;
		}

		if (currentElementArray[x, y] == null)
		{
			// Fix any holes left behind by placing a web
			currentElementArray[x, y] = new Web();
		}
	}

	public override void update(Element[,] oldElementArray, Element[,] currentElementArray, int x, int y, int maxX, int maxY, int T)
	{
		if (sleepTime > 0)
		{
			sleepTime--;
			return;
		}
		else
		{
			sleepTime = 5; // Sleep every 5 ticks to slow down the spider
		}

		switch (currentState)
		{
			case SpiderFSM.FALLING:
				handleFallingState(oldElementArray, currentElementArray, x, y, maxX, maxY, T);
				break;
			case SpiderFSM.WANDERING_ON_WEB:
				handleWanderingOnWebState(oldElementArray, currentElementArray, x, y, maxX, maxY, T);
				break;
			case SpiderFSM.WANDERING_TO_BUILD_SITE:
				handleWanderingToBuildSiteState(oldElementArray, currentElementArray, x, y, maxX, maxY, T);
				break;
			case SpiderFSM.BUILDING:
				handleBuildingState(oldElementArray, currentElementArray, x, y, maxX, maxY, T);
				break;
		}

		burn(oldElementArray, currentElementArray, x, y, maxX, maxY, T);
		updateColor(T);
	}

	/// <summary>
	/// SOME CHECKS ARE NOT DONE TO SEE IF THE MOVE IS VALID, please do so before calling this function
	/// </summary>
	public bool specialMove(Element[,] oldElementArray, Element[,] currentElementArray, int x, int y, int maxX, int maxY, int movementX, int movementY)
	{
		// Safety: Only move if this spider is still at (x, y)
		if (currentElementArray[x, y] != this)
			return false;

		int targetX = x + movementX;
		int targetY = y + movementY;

		// Bounds check
		if (targetX < 0 || targetX >= maxX || targetY < 0 || targetY >= maxY)
			return false;

		// Check if target cell is occupied by something other than a web
		Element targetElem = currentElementArray[targetX, targetY];
		if (targetElem != null && targetElem is not Web)
			return false;

		// Handle leaving a web behind if moving off a web
		if (onWeb != null && (movementX != 0 || movementY != 0))
		{
			currentElementArray[x, y] = onWeb;
			onWeb = null;
		}
		else
		{
			currentElementArray[x, y] = null;
		}

		// If moving onto a web, "pick it up"
		if (currentElementArray[targetX, targetY] is Web web)
		{
			onWeb = web;
		}
		else
		{
			onWeb = null;
		}

		// Move spider to new position
		currentElementArray[targetX, targetY] = this;
		addToPositionHistory(x, y); // Add the old position to history

		return true;
	}

	override public string inspectInfo()
	{
		return base.inspectInfo() + $"State: {currentState}\n" +
			$"Ticks since last state change: {lastMeaningfulStateChangeTick}\n" +
			$"On Web: {(onWeb != null ? "Yes" : "No")}\n" +
			$"Building Direction: ({buildingDirection.Item1}, {buildingDirection.Item2})\n";
	}
	override public string getState()
	{
		string posHistoryStr = "";
		foreach ((int, int) childLeaf in recentPositions) {
			posHistoryStr += childLeaf.Item1 + ":" + childLeaf.Item2 + ",";
		}
		if (posHistoryStr == "") posHistoryStr = "none";
		
		return base.getState() + ";"
		+ (int) currentState + ";"
		+ lastMeaningfulStateChangeTick + ";"
		+ posHistoryStr + ";"
		+ buildingDirection.Item1 + ";"
		+ buildingDirection.Item2;
	}

	override public int setState(string state)
	{
		int i = base.setState(state);
		string[] stateArgs = state.Split(";", false);
		// Beginning at 2, because spider is flammable
		currentState = (SpiderFSM)stateArgs[i++].ToInt();
		lastMeaningfulStateChangeTick = stateArgs[i++].ToInt();
		string posHistoryStr = stateArgs[i++];
		buildingDirection.Item1 = stateArgs[i++].ToInt();
		buildingDirection.Item2 = stateArgs[i++].ToInt();

		//Handle pos history
		if (posHistoryStr != "none")
		{
			string[] posHistoryList = posHistoryStr.Split(",", false);
			foreach (string pos in posHistoryList.Reverse()) // Reading in reverse to simulate the 
			{
				string[] coos = pos.Split(":", false);
				recentPositions.Enqueue((coos[0].ToInt(), coos[1].ToInt()));
			}
		}

		return i;
	}
}
