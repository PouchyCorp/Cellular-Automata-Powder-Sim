using System;
using System.Collections.Generic;

public interface IGas
{
    public int cloudLineY { get; set; }
}

public static class GasBehavior
{
    public static bool canMoveUpOnElement(this Element self, Element elementWhereMovement)
	{
		return elementWhereMovement == null || elementWhereMovement.density < self.density || elementWhereMovement is IGas;
	}
    public static void update(this IGas self, Element[,] oldElementArray, int x, int y, int maxX, int maxY, int T){
        float decision = Random.Shared.NextSingle();
		int distFromCloudLine = Math.Abs(self.cloudLineY - y) + 1;

		if (decision < 0.25f)
		{
			MoveManager.Instance.AttemptMove(oldElementArray, x, y, -1, 0, maxX, maxY);
		}
		else if (decision < 0.5f)
		{
			MoveManager.Instance.AttemptMove(oldElementArray, x, y, 1, 0, maxX, maxY);
		}
		else // if (decision >= 0.5f)
		{
			int dir = 1;
			if (self.cloudLineY - y >= 0) // always move up if below cloud line, otherwise move down
			{
				dir = -1;
			}

			if (decision < (1f / distFromCloudLine))
			{
				MoveManager.Instance.AttemptMove(oldElementArray, x, y, 0, dir, maxX, maxY);
			}
			else if (decision > 0.9f) // small chance to move in the opposite direction of the cloud line
			{
				MoveManager.Instance.AttemptMove(oldElementArray, x, y, 0, -dir, maxX, maxY);
			}
		}
    }
}