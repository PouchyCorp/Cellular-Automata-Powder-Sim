using System;

public interface IPowder
{
	// No specific properties for powders (everything is already in Element class)
}

public static class PowderBehavior
{
	public static void Update(IPowder self, Element[,] oldGrid, int x, int y, int maxX, int maxY, int T)
	{
		if (MoveManager.Instance.AttemptMove(oldGrid, x, y, 0, 1, maxX, maxY))
		{
			return;
		}

		if (Random.Shared.NextSingle() < 0.5f) 
		{
			if (MoveManager.Instance.AttemptMove(oldGrid, x, y, 1, 1, maxX, maxY))
			{
				return;
			} 
			else 
			{
				MoveManager.Instance.AttemptMove(oldGrid, x, y, -1, 1, maxX, maxY);
			}

		}
		else
		{
			if (MoveManager.Instance.AttemptMove(oldGrid, x, y, -1, 1, maxX, maxY))
			{
				return;
			}
			else
			{
				MoveManager.Instance.AttemptMove(oldGrid, x, y, 1, 1, maxX, maxY);
			}
		}
	}
}
