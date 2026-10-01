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

		if (T + x * y % 2 == 0) // Alternate the order of diagonal movement to avoid bias (not using RNG to avoid performance issues)
		{
			if (MoveManager.Instance.AttemptMove(oldGrid, x, y, 1, 1, maxX, maxY))
			{
				return;
			}
			if (MoveManager.Instance.AttemptMove(oldGrid, x, y, -1, 1, maxX, maxY))
			{
				return;
			}
		}
		else
		{
			if (MoveManager.Instance.AttemptMove(oldGrid, x, y, -1, 1, maxX, maxY))
			{
				return;
			}
			if (MoveManager.Instance.AttemptMove(oldGrid, x, y, 1, 1, maxX, maxY))
			{
				return;
			}
		}
	}
}
