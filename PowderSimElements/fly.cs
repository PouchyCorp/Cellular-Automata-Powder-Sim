using Godot;
using System.Collections.Generic;
public class Fly : Element, ILife, ISolid
{
	int lastActivity = 0;

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
		ashCreationPercentage = 0.0f;
		density = 30;
		color = Colors.Black;
		flammability = 3;
	}
	public override void update(Element[,] oldElementArray, Element[,] currentElementArray, int x, int y, int maxX, int maxY, int T)
	{
		lifetime--;
		if (lifetime <= 0 && currentElementArray[x, y] == this)
		{
			currentElementArray[x, y] = null;
			return;
		}

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
		if (stuckInWeb)
		{
			if (T - stuckInWebTime > stuckInWebDuration)
			{
				stuckInWeb = false; // free from web after duration
				tryMoveInDirection(currentElementArray, oldElementArray, x, y, maxX, maxY, 0, -1, T); // try to move up out of web
			}
			burn(oldElementArray, currentElementArray, x, y, maxX, maxY, T);
			updateColor(T);
			return; // can't move while stuck
		}

		// move randomly, but with a slight bias to go down
		directionChangeTimer++;
		if (directionChangeTimer >= directionChangeInterval)
		{
			directionChangeTimer = rng.RandiRange(0, directionChangeInterval - 1); // reset timer with some randomness
			changeDirection();
		}
		if (!tryMoveInDirection(currentElementArray, oldElementArray, x, y, maxX, maxY, currentDirection.Item1, currentDirection.Item2, T))
		{
			// Try to change direction if blocked
			changeDirection();
			tryMoveInDirection(currentElementArray, oldElementArray, x, y, maxX, maxY, currentDirection.Item1, currentDirection.Item2, T);
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
					if (currentElementArray[nx, ny] is Fruit fruit)
					{
						if (pollinateFruit(fruit))
						{
							reproduce(currentElementArray, x, y, maxX, maxY);
							reproduce(currentElementArray, x, y, maxX, maxY);
						}
					}
				}
			}
		}

		// just move around in the dirt
		burn(oldElementArray, currentElementArray, x, y, maxX, maxY, T);
		updateColor(T);

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

	private bool tryMoveInDirection(Element[,] currentElementArray, Element[,] oldElementArray, int x, int y, int maxX, int maxY, int dirX, int dirY, int T)
	{
		if (x + dirX < 0 || x + dirX >= maxX || y + dirY < 0 || y + dirY >= maxY)
			return false; // out of bounds
		if (currentElementArray[x + dirX, y + dirY] is Web)
		{
			stuckInWeb = true;
			stuckInWebTime = T;
			currentElementArray[x + dirX, y + dirY] = this;
			currentElementArray[x, y] = null;
			return true; // can't move into web
		}

		if (currentElementArray[x + dirX, y + dirY] == null || currentElementArray[x + dirX, y + dirY] is IGas)
		{
			currentElementArray[x, y] = currentElementArray[x + dirX, y + dirY];
			currentElementArray[x + dirX, y + dirY] = this;
			return true;
		}
		return false;
	}

	public void reproduce(Element[,] currentElementArray, int x, int y, int maxX, int maxY)
	{
		List<(int, int)> directions = new List<(int, int)> { (0, 1), (1, 0), (0, -1), (-1, 0) };
		foreach (var dir in directions)
		{
			int nx = x + dir.Item1;
			int ny = y + dir.Item2;
			if (nx > 0 && nx < maxX && ny > 0 && ny < maxY)
			{
				if (currentElementArray[nx, ny] == null)
				{
					currentElementArray[nx, ny] = new Fly();
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
