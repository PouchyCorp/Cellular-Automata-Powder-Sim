using Godot;
using System;
using System.Collections.Generic;

// TODO : Make the fly split into two flies when it's nutrient reach two. and make it die (dropping biomass) when nutrient reach 0. Also make it eat fruit to gain nutrient.
public class Fly : Element, ILife, ISolid, IFlammable
{

	const float BASE_NUTRIENT_COST = 1.0f;
	int lastActivity = 0;
	public int flammability { get; set; } = 3;
	public bool burning { get; set; } = false;
	public int burningLifetime { get; set; }

	public float maxNutrient => BASE_NUTRIENT_COST * 2.0f;
	public float nutrient = BASE_NUTRIENT_COST;
	public float wetness = 0.0f;
	public float maxWetness => 0.0f;


	int lifetime = 300 * 60; // ticks
	int activityInterval = 5;

	// Direction and movement properties
	private (int, int) currentDirection = (1, 0); // Start moving right
	private int directionChangeTimer = 0;
	private int directionChangeInterval = 6; // Change direction every 6 activity ticks (on average)

	public bool stuckInWeb = false;
	private int stuckInWebDuration = 20 * 60; // ticks
	private int stuckInWebTime = 0;
	public Fly()
	{
		density = 30;
		color = Colors.Black;
	}


	public override void update(Element[,] oldElementArray, int x, int y, int maxX, int maxY, int T)
	{
		lifetime--;
		if (lifetime <= 0)
		{
			GridManager.Instance.RequestDeletion(x, y, maxX, maxY);
			return;
		}

		if (T - lastActivity < activityInterval)
		{
			// Not time to act yet
			FlammableBehavior.burn(this, oldElementArray, x, y, maxX, maxY, T);
			updateColor(T, x, y);
			return;
		}
		else
		{
			lastActivity = T;
		}

		if (stuckInWeb)
		{
			if (T - stuckInWebTime > stuckInWebDuration)
			{
				stuckInWeb = false; // free from web after duration
				tryMoveInDirection(oldElementArray, x, y, maxX, maxY, 0, -1, T); // try to move up out of web
			}
			FlammableBehavior.burn(this, oldElementArray, x, y, maxX, maxY, T);
			updateColor(T, x, y);
			return; // can't move while stuck
		}

		// move randomly, but with a slight bias to go down
		directionChangeTimer++;
		if (directionChangeTimer >= directionChangeInterval)
		{
			directionChangeTimer = Random.Shared.Next(0, directionChangeInterval - 1); // reset timer with some randomness
			changeDirection();
		}
		if (!tryMoveInDirection(oldElementArray, x, y, maxX, maxY, currentDirection.Item1, currentDirection.Item2, T))
		{
			// Try to change direction if blocked
			changeDirection();
			tryMoveInDirection(oldElementArray, x, y, maxX, maxY, currentDirection.Item1, currentDirection.Item2, T);
		}

		for (int dx = -1; dx <= 1; dx++)
		{
			for (int dy = -1; dy <= 1; dy++)
			{
				if (dx == 0 && dy == 0) continue;
				int nx = x + dx;
				int ny = y + dy;
				if (nx >= 0 && nx < maxX && ny >= 0 && ny < maxY)
				{
					if (oldElementArray[nx, ny] is Fruit fruit)
					{
						if (pollinateFruit(fruit))
						{
							TakeNutrientRequest request = new TakeNutrientRequest(x, y, nx, ny, BASE_NUTRIENT_COST);
							NutrientManager.Instance.AddTakeNutrientRequest(request, maxX, maxY); // Transfer nutrient to the fruit
						}
					}
				}
			}
		}

		if (nutrient == maxNutrient)
		{
			reproduce(oldElementArray, x, y, maxX, maxY);
			nutrient = 1.0f; // Reset nutrient after reproduction
		}



		// just move around in the dirt
		FlammableBehavior.burn(this, oldElementArray, x, y, maxX, maxY, T);
		updateColor(T, x, y);

	}

	private void changeDirection()
	{
		// Simple direction change with downwards bias
		float rand = Random.Shared.NextSingle();

		// 16% chance to go down, 12% each for other directions (ugly but works)
		if (rand < 0.16f)
			currentDirection = (0, 1); // down
		else if (rand < 0.28f)
			currentDirection = (-1, 0); // left
		else if (rand < 0.4f)
			currentDirection = (1, 0); // right
		else if (rand < 0.52f)
			currentDirection = (0, -1); // up
		else if (rand < 0.64f)
			currentDirection = (-1, -1); // up-left
		else if (rand < 0.76f)
			currentDirection = (1, -1); // up-right
		else if (rand < 0.88f)
			currentDirection = (-1, 1); // down-left
		else
			currentDirection = (1, 1); // down-right
	}

	private bool tryMoveInDirection(Element[,] oldElementArray, int x, int y, int maxX, int maxY, int dirX, int dirY, int T)
	{
		if (x + dirX < 0 || x + dirX >= maxX || y + dirY < 0 || y + dirY >= maxY)
			return false; // out of bounds

		if (oldElementArray[x + dirX, y + dirY] is Web)
		{
			stuckInWeb = true;
			stuckInWebTime = T;
			GridManager.Instance.RequestDeletion(x, y, x + dirX, y + dirY); // remove the fly from the grid
			return true; // can't move into web
		}

		if (oldElementArray[x + dirX, y + dirY] == null || oldElementArray[x + dirX, y + dirY] is IGas)
		{
			MoveManager.Instance.AttemptMove(oldElementArray, x, y, dirX, dirY, maxX, maxY);
			return true;
		}
		return false;
	}

	public void reproduce(Element[,] oldElementArray, int x, int y, int maxX, int maxY)
	{
		List<(int, int)> directions = new List<(int, int)> { (0, 1), (1, 0), (0, -1), (-1, 0) };
		foreach (var dir in directions)
		{
			int nx = x + dir.Item1;
			int ny = y + dir.Item2;
			if (nx > 0 && nx < maxX && ny > 0 && ny < maxY)
			{
				if (oldElementArray[nx, ny] == null)
				{
					oldElementArray[nx, ny] = new Fly();
					return; // Only try to reproduce in one direction
				}
			}
		}
	}

	public bool pollinateFruit(Fruit fruit)
	{
		if (!fruit.pollinated)
		{
			fruit.pollinated = true;
			return true;
		}
		return false;
	}

	override public string getState()
	{
		return base.getState() + ";" +
			lastActivity + ";" +
			lifetime + ";" +
			currentDirection.Item1 + ";" +
			currentDirection.Item2 + ";" +
			directionChangeTimer + ";" +
			directionChangeInterval + ";" +
			stuckInWeb + ";" +
			stuckInWebTime;
	}

	override public int setState(string state)
	{
		int i = base.setState(state);
		string[] stateArgs = state.Split(";", false);
		lastActivity = stateArgs[i++].ToInt();
		lifetime = stateArgs[i++].ToInt();
		currentDirection.Item1 = stateArgs[i++].ToInt();
		currentDirection.Item1 = stateArgs[i++].ToInt();
		directionChangeTimer = stateArgs[i++].ToInt();
		directionChangeInterval = stateArgs[i++].ToInt();
		stuckInWeb = stateArgs[i++] == "True";
		stuckInWebTime = stateArgs[i++].ToInt();
		return i;
	}

}
