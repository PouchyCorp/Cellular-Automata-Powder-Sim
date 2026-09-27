using System;

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

		int movementX = 0;
		int movementY = 0;

		if (decision < 0.25f)
		{
			movementX = -1;
			movementY = 0;
		}
		else if (decision < 0.5f)
		{
			movementX = 1;
			movementY = 0;
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
				movementX = 0;
				movementY = dir;
			}
			else if (decision > 0.9f) // small chance to move in the opposite direction of the cloud line
			{
				movementX = 0;
				movementY = -dir;
			}
		}

		if (movementX != 0 || movementY != 0){
			if (oldElementArray[x + movementX, y + movementY] is Web)
			{
				GridManager.Instance.RequestDeletion(x, y, maxX, maxY);
			}

			MoveManager.Instance.AttemptMove(oldElementArray, x, y, movementX, movementY, maxX, maxY);
		}
			
    }
}