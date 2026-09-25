using System;

public interface IPowder
{
	public bool canMoveDownOnElement(Element elementWhereMovement)
	{
		return elementWhereMovement == null || elementWhereMovement is IGas || elementWhereMovement is ILiquid;
	}

	public bool canMoveSideOnElement(Element elementWhereMovement)
	{
		return elementWhereMovement == null || elementWhereMovement is IGas || elementWhereMovement is ILiquid;
	}
}

public static class PowderBehavior
{
	public static bool CanMoveDownOnElement(Element elementWhereMovement)
	{
		return elementWhereMovement == null || elementWhereMovement is IGas || elementWhereMovement is ILiquid;
	}

	public static bool CanMoveSideOnElement(Element elementWhereMovement)
	{
		return elementWhereMovement == null || elementWhereMovement is IGas || elementWhereMovement is ILiquid;
	}

	public static void Update(Element self, Element[,] oldElementArray, Element[,] currentElementArray, int x, int y, int maxX, int maxY, int T)
	{
		if (currentElementArray[x, y] != self) return; // Return if a movement has already been done

		if (MoveRequest.CanMove(oldElementArray, x, y, 0, 1))
		{
			MoveManager.Instance.AddMoveRequest(new MoveRequest(x, y, 0, 1), maxX, maxY);
			return;
		}

		if (T % 2 == 0) // Alternate the order of diagonal movement to avoid bias (not using RNG to avoid performance issues)
		{
			if (MoveRequest.CanMove(oldElementArray, x, y, 1, 1))
			{
				MoveManager.Instance.AddMoveRequest(new MoveRequest(x, y, 1, 1), maxX, maxY);
				return;
			}
			if (MoveRequest.CanMove(oldElementArray, x, y, -1, 1))
			{
				MoveManager.Instance.AddMoveRequest(new MoveRequest(x, y, -1, 1), maxX, maxY);
				return;
			}
		}
		else
		{
			if (MoveRequest.CanMove(oldElementArray, x, y, -1, 1))
			{
				MoveManager.Instance.AddMoveRequest(new MoveRequest(x, y, -1, 1), maxX, maxY);
				return;
			}
			if (MoveRequest.CanMove(oldElementArray, x, y, 1, 1))
			{
				MoveManager.Instance.AddMoveRequest(new MoveRequest(x, y, 1, 1), maxX, maxY);
				return;
			}
		}

		self.burn(oldElementArray, currentElementArray, x, y, maxX, maxY, T);
		self.updateColor(T);
	}
}
