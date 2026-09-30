using System;

public interface IPowder
{
	// No specific properties for powders (everything is already in Element class)
}

public static class PowderBehavior
{
	public static void Update(IPowder self, Element[,] oldElementArray, int x, int y, int maxX, int maxY, int T)
	{

		if (MoveRequest.CanMove(oldElementArray, x, y, 0, 1, maxX, maxY))
		{
			MoveManager.Instance.AddMoveRequest(new MoveRequest(x, y, 0, 1), maxX, maxY);
			return;
		}

		if (T % 2 == 0) // Alternate the order of diagonal movement to avoid bias (not using RNG to avoid performance issues)
		{
			if (MoveRequest.CanMove(oldElementArray, x, y, 1, 1, maxX, maxY))
			{
				MoveManager.Instance.AddMoveRequest(new MoveRequest(x, y, 1, 1), maxX, maxY);
				return;
			}
			if (MoveRequest.CanMove(oldElementArray, x, y, -1, 1, maxX, maxY))
			{
				MoveManager.Instance.AddMoveRequest(new MoveRequest(x, y, -1, 1), maxX, maxY);
				return;
			}
		}
		else
		{
			if (MoveRequest.CanMove(oldElementArray, x, y, -1, 1, maxX, maxY))
			{
				MoveManager.Instance.AddMoveRequest(new MoveRequest(x, y, -1, 1), maxX, maxY);
				return;
			}
			if (MoveRequest.CanMove(oldElementArray, x, y, 1, 1, maxX, maxY))
			{
				MoveManager.Instance.AddMoveRequest(new MoveRequest(x, y, 1, 1), maxX, maxY);
				return;
			}
		}
	}
}
