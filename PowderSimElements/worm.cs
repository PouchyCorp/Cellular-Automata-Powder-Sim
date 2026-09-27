using System;
using System.ComponentModel.DataAnnotations;
using Godot;

// TODO : Make the worm a snake
public class Worm : Element, ILife, ISolid
{
	public float nutrient { get; set; } = 0f;
	public float maxNutrient => 10f;
	public float wetness { get; set; } = 0f;
	public float maxWetness => 1f;

	int lastActivity = 0;
	int activityInterval = 10;

	// Direction and movement properties
	private (int, int) currentDirection = (1, 0); // Start moving right
	private int directionChangeTimer = 0;
	private int directionChangeInterval = 10; // Change direction every 10 ticks (on average)
	private int obstacleHitCooldown = 0; // Prevent immediate oscillation after hitting obstacle

	private float wetnessBuffer = 0.0f; // to store excess wetness before transferring to soil

	Soil inSoil = null;

	enum WormState
	{
		Moving,
		Falling,
	}
	private WormState wormState = WormState.Falling;

	public Worm()
	{
		density = 30;
		color = Colors.Pink;
	}

	public override void update(Element[,] oldElementArray, int x, int y, int maxX, int maxY, int T)
	{
		if (T - lastActivity < activityInterval)
		{
			// Not time to act yet
			updateColor(T, x, y);
			return;
		}
		else
		{
			lastActivity = T;
		}

		switch (wormState)
		{
			case WormState.Falling:
				// check if can fall down
				if (y + 1 < maxY && (oldElementArray[x, y + 1] == null || oldElementArray[x, y + 1] is ILiquid))
				{
					// fall down
					MoveManager.Instance.AttemptMove(oldElementArray, x, y, 0, 1, maxX, maxY);
					break;
				}

				if (y + 1 >= maxY || oldElementArray[x, y + 1] is Soil)
				{
					// landed on solid ground
					wormState = WormState.Moving;
					break;
				}
				{
					wormState = WormState.Moving; // landed
				}

				break;

			case WormState.Moving:

				// Update timers
				directionChangeTimer++;
				if (obstacleHitCooldown > 0)
					obstacleHitCooldown--;

				// Random direction change
				if (directionChangeTimer >= directionChangeInterval)
				{
					changeDirection();
					directionChangeTimer = Random.Shared.Next(0, directionChangeInterval / 2); // reset timer to a random value to avoid synchronized direction changes
				}

				// Try to move in current direction
				(int, int) nearbyBiomass = findNearbyBiomass(oldElementArray, x, y, maxX, maxY);
				if (nearbyBiomass != (-1, -1))
				{
					// Move towards biomass
					int dx = nearbyBiomass.Item1 - x;
					int dy = nearbyBiomass.Item2 - y;
					if (moveInSoil(oldElementArray, x, y, maxX, maxY, dx, dy))
					{
						// Successfully moved to biomass
						break;
					}
				}
				
				if (moveInSoil(oldElementArray, x, y, maxX, maxY, currentDirection.Item1, currentDirection.Item2))
				{
					// Successfully moved in current direction
					break;
				}
				else if (obstacleHitCooldown == 0)
				{
					// Hit obstacle, change direction
					obstacleHitCooldown = 3;
					changeDirection();
					directionChangeTimer = 0;
				}

				break;
		}
		updateColor(T, x, y);
	}
	private void changeDirection()
	{
		// Simple direction change with upward bias
		float rand = Random.Shared.NextSingle();

		// 40% chance to go up, 20% each for other directions
		if (rand < 0.4f)
			currentDirection = (0, -1); // up
		else if (rand < 0.6f)
			currentDirection = (-1, 0); // left
		else if (rand < 0.8f)
			currentDirection = (1, 0); // right
		else
			currentDirection = (0, 1); // down
	}

	public (int, int) findNearbyBiomass(Element[,] oldElementArray, int x, int y, int maxX, int maxY)
	{
		foreach ((int dx, int dy) in new (int, int)[] { (0, -1), (0, 1), (-1, 0), (1, 0) })
		{
			int nx = x + dx;
			int ny = y + dy;

			if (nx < 0 || nx >= maxX || ny < 0 || ny >= maxY)
				continue; // Out of bounds

			if (oldElementArray[nx, ny] is Biomass)
			{
				return (nx, ny); // Return the position of the eaten biomass
			}
		}
		return (-1, -1); // No biomass found
	}

	public Soil getDirtFromBiomass(Element[,] oldElementArray, int x, int y, int maxX, int maxY)
	{
		Biomass biomass = oldElementArray[x, y] as Biomass; // checks are done before calling this function
		float biomassNutrient = biomass.nutrient;
		float biomassWetness = biomass.wetness;

		// Create soil with the nutrient and wetness of the biomass
		Soil newSoil = new Soil();
		newSoil.nutrient = biomassNutrient;

		if (biomassWetness < 1) // empty buffer if biomass wetness < 1
		{
			float wetnessToTransfer = Math.Min(wetnessBuffer, 1 - biomassWetness);
			biomassWetness += wetnessToTransfer;
			wetnessBuffer -= wetnessToTransfer;
		}

		float wetnessToTransferToSoil = Math.Min(biomassWetness, 1f);
		newSoil.wetness = wetnessToTransferToSoil;    // max wetness is 1, there may be a problem here if biomass wetness > 1 (this comment was right ...)
		wetnessBuffer += biomassWetness - wetnessToTransferToSoil; // store excess wetness in buffer

		return newSoil;
	}

	public bool moveInSoil(Element[,] oldElementArray, int x, int y, int maxX, int maxY, int movementX, int movementY)
	{
		int targetX = x + movementX;
		int targetY = y + movementY;

		// Bounds check
		if (targetX < 0 || targetX >= maxX || targetY < 0 || targetY >= maxY)
			return false;

		// Check if target cell is occupied by something other than a soil
		Element targetElem = oldElementArray[targetX, targetY];
		if (targetElem != null && targetElem is not Soil)
			return false;

		// Move worm to new position
		GridManager.Instance.RequestDeletion(x, y, maxX, maxY, inSoil); // leave the stored soil behind by replacing the worm

		// If moving onto a soil, "pick it up"
		if (oldElementArray[targetX, targetY] is Soil soil)
		{
			inSoil = soil; // store the soil the worm is moving onto
		}

		if (oldElementArray[targetX, targetY] is Biomass)
		{
			// If moving onto a biomass, convert it to soil, store its nutrient and wetness into the soil, and "pick it up"
			inSoil = getDirtFromBiomass(oldElementArray, targetX, targetY, maxX, maxY);
		}
		
		GridManager.Instance.RequestDeletion(targetX, targetY, maxX, maxY, this); // move the worm to the new position (the soil is stored in inSoil don't worry)

		return true;
	}

	public override string inspectInfo()
	{
		return base.inspectInfo() + $"\nState: {wormState} \nDirection: ({currentDirection.Item1}, {currentDirection.Item2}) \nDirection Timer: {directionChangeTimer}/{directionChangeInterval} \nObstacle Cooldown: {obstacleHitCooldown}";
	}

	public override string getState()
	{
		return base.getState()
			+ ";" + (int)wormState
			+ ";" + currentDirection.Item1
			+ ";" + currentDirection.Item2
			+ ";" + directionChangeTimer
			+ ";" + obstacleHitCooldown
			+ ";" + (inSoil != null ? inSoil.nutrient.ToString("F3") : "null")
			+ ";" + (inSoil != null ? inSoil.wetness.ToString("F3") : "null")
			+ ";" + wetnessBuffer.ToString("F3");
	}

	public override int setState(string state)
	{
		string[] parts = state.Split(';');
		base.setState(string.Join(";", parts[0], parts[1])); // base state has 2 parts

		if (parts.Length >= 9)
		{
			wormState = (WormState)int.Parse(parts[2]);
			int dirX = int.Parse(parts[3]);
			int dirY = int.Parse(parts[4]);
			currentDirection = (dirX, dirY);
			directionChangeTimer = int.Parse(parts[5]);
			obstacleHitCooldown = int.Parse(parts[6]);

			// Recreate inSoil if data is available
			if (parts[7] != "null" && parts[8] != "null")
			{
				inSoil = new Soil();
				inSoil.nutrient = float.Parse(parts[7]);
				inSoil.wetness = float.Parse(parts[8]);
			}
			else
			{
				inSoil = null;
			}

			if (parts.Length >= 10)
			{
				wetnessBuffer = float.Parse(parts[9]);
			}
			else
			{
				wetnessBuffer = 0.0f;
			}
		}
		return parts.Length;
	}


}
