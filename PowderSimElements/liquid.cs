using System;

public interface ILiquid
{
    public int directionX { get; set; }
    public int lifetime { get; set; }
    public int maxLifetime { get; set; }
}

public static class LiquidBehavior
{
    public static bool CanMoveDownOnElement(Element elementWhereMovement)
    {
        return elementWhereMovement == null || elementWhereMovement is IGas || (elementWhereMovement is ILiquid );
    }

    public static bool CanMoveSideOnElement(Element elementWhereMovement)
    {
        return elementWhereMovement == null || elementWhereMovement is IGas || elementWhereMovement is ILiquid;
    }

    public static void update(this ILiquid self, Element[,] oldElementArray, int x, int y, int maxX, int maxY, int T){
        if (y + 2 < maxY && oldElementArray[x, y + 1] is Leaf && oldElementArray[x, y + 2] == null)
		{
			MoveManager.Instance.AttemptMove(oldElementArray, x, y, 0, 2, maxX, maxY);
			return;
		}

		if (MoveManager.Instance.AttemptMove(oldElementArray, x, y, 0, 1, maxX, maxY)) { self.lifetime = self.maxLifetime; return; }

		float randomFloat = Random.Shared.NextSingle();
		if (randomFloat < 0.5f)
		{
			if (MoveManager.Instance.AttemptMove(oldElementArray, x, y, -1, 1, maxX, maxY)) { self.lifetime = self.maxLifetime; return; }
			if (MoveManager.Instance.AttemptMove(oldElementArray, x, y, 1, 1, maxX, maxY)) { self.lifetime = self.maxLifetime; return; }
		}
		else
		{
			if (MoveManager.Instance.AttemptMove(oldElementArray, x, y, -1, 1, maxX, maxY)) { self.lifetime = self.maxLifetime; return; }
			if (MoveManager.Instance.AttemptMove(oldElementArray, x, y, 1, 1, maxX, maxY)) { self.lifetime = self.maxLifetime; return; }
		}

		if (randomFloat < 0.1f)
		{
			self.directionX *= -1;
		}

		if (MoveManager.Instance.AttemptMove(oldElementArray, x, y, self.directionX, 0, maxX, maxY)) { self.lifetime--; return; }
		else
		{
			self.directionX *= -1;
		}
    }
}