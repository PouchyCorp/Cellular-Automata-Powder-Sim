using System;
using Godot;

public interface ILiquid
{
    public int directionX { get; set; }
    public int lifetime { get; set; }
    public int maxLifetime { get; set; }
}

public static class LiquidBehavior
{
    public static void update(this ILiquid self, Element[,] oldGrid, int x, int y, int maxX, int maxY, int T){
        if (y + 2 < maxY && oldGrid[x, y + 1] is Leaf && oldGrid[x, y + 2] is IGas or null)
		{
			MoveManager.Instance.AttemptMove(oldGrid, x, y, 0, 2, maxX, maxY);
			return;
		}

		if (MoveManager.Instance.AttemptMove(oldGrid, x, y, 0, 1, maxX, maxY)) {return; }

		float randomFloat = Random.Shared.NextSingle();
		if (randomFloat < 0.5f)
		{
			if (MoveManager.Instance.AttemptMove(oldGrid, x, y, -1, 1, maxX, maxY)) {return; }
			if (MoveManager.Instance.AttemptMove(oldGrid, x, y, 1, 1, maxX, maxY)) {return; }
		}
		else
		{
			if (MoveManager.Instance.AttemptMove(oldGrid, x, y, 1, 1, maxX, maxY)) {return; }
			if (MoveManager.Instance.AttemptMove(oldGrid, x, y, -1, 1, maxX, maxY)) {return; }
		}

		if (randomFloat < 0.1f)
		{
			self.directionX *= -1;
		}

		if (MoveManager.Instance.AttemptMove(oldGrid, x, y, self.directionX, 0, maxX, maxY)) { return; }
		else
		{
			self.directionX *= -1;
		}
    }
}