using System;
using System.ComponentModel.DataAnnotations;
using Godot;
public class Worm : Element, ILife, ISolid
{
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
		ashCreationPercentage = 0.0f;
		density = 30;
		color = Colors.Pink;
		flammability = 3;
	}

	public override void update(Element[,] oldElementArray, Element[,] currentElementArray, int x, int y, int maxX, int maxY, int T)
	{
		if (T - lastActivity < activityInterval)
		{
			// Not time to act yet
			burn(oldElementArray, currentElementArray, x, y, maxX, maxY, T);
			updateColor(T);
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
				if (y + 1 < maxY && (currentElementArray[x, y + 1] == null || currentElementArray[x, y + 1] is ILiquid || currentElementArray[x, y + 1] is Soil))
				{
					// fall down
					specialMove(currentElementArray, x, y, maxX, maxY, 0, 1);
				}

				if (inSoil != null)
				{
					wormState = WormState.Moving; // landed
				}

				break;

			case WormState.Moving:
				// Try to eat nearby biomass first
				if (eatNearbyBiomass(currentElementArray, x, y, maxX, maxY))
				{
					// Successfully ate biomass and moved
					break;
				}

				// Update timers
				directionChangeTimer++;
				if (obstacleHitCooldown > 0)
					obstacleHitCooldown--;

				// Random direction change
				if (directionChangeTimer >= directionChangeInterval)
				{
					changeDirection();
					directionChangeTimer = rng.RandiRange(0, directionChangeInterval / 2); // reset timer to a random value to avoid synchronized direction changes
				}

				// Try to move in current direction
				if (tryMoveInDirection(currentElementArray, x, y, maxX, maxY, currentDirection.Item1, currentDirection.Item2))
				{
					// Successfully moved
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

		// just move around in the dirt
		burn(oldElementArray, currentElementArray, x, y, maxX, maxY, T);
		updateColor(T);
	}

	private bool eatNearbyBiomass(Element[,] currentElementArray, int x, int y, int maxX, int maxY)
	{
		// Check adjacent cells for biomass
		foreach ((int dx, int dy) in new (int, int)[] { (0, -1), (0, 1), (-1, 0), (1, 0) })
		{
			int nx = x + dx;
			int ny = y + dy;

			if (nx < 0 || nx >= maxX || ny < 0 || ny >= maxY)
				continue; // Out of bounds

			if (currentElementArray[nx, ny] is Biomass)
			{
				consumeBiomass(currentElementArray, nx, ny);
				specialMove(currentElementArray, x, y, maxX, maxY, dx, dy); // Move into the eaten biomass spot
				return true; // Eat only one biomass per update
			}
		}
		return false;
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

	private bool tryMoveInDirection(Element[,] currentElementArray, int x, int y, int maxX, int maxY, int dirX, int dirY)
	{
		if (x + dirX < 0 || x + dirX >= maxX || y + dirY < 0 || y + dirY >= maxY)
			return false; // out of bounds

		// Try to move onto soil
		if (currentElementArray[x + dirX, y + dirY] is Soil)
		{
			return specialMove(currentElementArray, x, y, maxX, maxY, dirX, dirY);
		}

		return false;
	}

	public void consumeBiomass(Element[,] currentElementArray, int x, int y)
	{
		Biomass biomass = currentElementArray[x, y] as Biomass; // checks are done before calling this function
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

		currentElementArray[x, y] = newSoil;
	}

	public bool specialMove(Element[,] currentElementArray, int x, int y, int maxX, int maxY, int movementX, int movementY)
	{
		// Safety: Only move if this worm is still at (x, y)
		if (currentElementArray[x, y] != this)
			return false;

		int targetX = x + movementX;
		int targetY = y + movementY;

		// Bounds check
		if (targetX < 0 || targetX >= maxX || targetY < 0 || targetY >= maxY)
			return false;

		// Check if target cell is occupied by something other than a soil
		Element targetElem = currentElementArray[targetX, targetY];
		if (targetElem != null && targetElem is not Soil)
			return false;

		// Handle leaving a soil behind if moving off a soil
		if (inSoil != null && (movementX != 0 || movementY != 0))
		{
			currentElementArray[x, y] = inSoil;
			inSoil = null;
		}
		else
		{
			currentElementArray[x, y] = null;
		}

		// If moving onto a soil, "pick it up"
		if (currentElementArray[targetX, targetY] is Soil soil)
		{
			inSoil = soil;
		}
		else
		{
			inSoil = null;
		}

		// Move worm to new position
		currentElementArray[targetX, targetY] = this;

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
