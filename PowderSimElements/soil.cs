using Godot;
using System;
using System.Data;


public class Soil : Element, IPowder, ISolid, INutrient
{
	int lastActivity = 0;
	bool needsUpdate = true;
	int activityInterval = 15;
	public (int, int)[] cardinals = [(0, 1), (1, 0), (0, -1), (-1, 0)];
	new private Color baseColor = Color.FromHtml("#8d7267ff");
	private Color wetColor = Color.FromHtml("#3a1008ff");
	private Color richColor = Color.FromHtml("#394e35ff");

	public Soil()
	{
		density = 20;
		color = baseColor;
		flammability = 0;
		wetness = 0.0f;
		nutrient = 0.0f;
		modulateColor();
	}

	public override bool canMoveDownOnElement(Element elementWhereMovement)
	{
		return PowderBehavior.CanMoveDownOnElement(elementWhereMovement);
	}

	public override bool canMoveSideOnElement(Element elementWhereMovement)
	{
		return PowderBehavior.CanMoveSideOnElement(elementWhereMovement);
	}

	public override void modulateColor(float intensity = 0.05F)
	{
		float z = rng.RandfRange(0.0f, intensity);
		baseColor = baseColor.Darkened(z);
	}

	override public void updateColor(int T)
	{
		base.updateColor(T);

		Color nutriHue = baseColor.Lerp(richColor, Math.Min(nutrient, 2)); // more nutrient = darker color
		Color wetHue = baseColor.Lerp(wetColor, wetness); // more wet = darker color

		color = nutriHue.Lerp(wetHue, 0.5f); // blend both effects TODO: adjust colors
	}

	public void ChangeNutrient(float change, Element[,] currentElementArray, int x, int y, int maxX, int maxY)
	// Method to change nutrient level, the change is the amount to add or subtract from the current nutrient level
	{
		nutrient += change;
		foreach ((int nx, int ny) in cardinals)
		{
			int neighborX = x + nx;
			int neighborY = y + ny;

			if (neighborX >= 0 && neighborX < maxX && neighborY >= 0 && neighborY < maxY)
			{   
				// using currentElementArray as read and write here is not a problem because needsUpdate is not a state that can have consequences to the simulation order.
				if (currentElementArray[neighborX, neighborY] is Soil neighborSoil){
					neighborSoil.needsUpdate = true; // Mark neighbor soil for update
				}
			}
		}
	}

	public void ChangeWetness(float change, Element[,] currentElementArray, int x, int y, int maxX, int maxY)
	// Method to change wetness level, the change is the amount to add or subtract from the current wetness level
	{
		wetness += change;
		foreach ((int nx, int ny) in cardinals)
		{
			int neighborX = x + nx;
			int neighborY = y + ny;

			if (neighborX >= 0 && neighborX < maxX && neighborY >= 0 && neighborY < maxY)
			{   
				// using currentElementArray as read and write here is not a problem because needsUpdate is not a state that can have consequences to the simulation order.
				if (currentElementArray[neighborX, neighborY] is Soil neighborSoil){
					neighborSoil.needsUpdate = true; // Mark neighbor soil for update
				}
			}
		}
	}

	((int, int)[], int) getNeighborsIndices(Element[,] currentElementArray, int x, int y, int maxX, int maxY)
	{
		(int, int)[] neighbors = new (int, int)[4];
		int count = 0;
		for (int i = 0; i < 4; i++)
		{
			int nx = x + cardinals[i].Item1;
			int ny = y + cardinals[i].Item2;

			if (nx >= 0 && nx < maxX && ny >= 0 && ny < maxY && currentElementArray[nx, ny] is Soil)
			{
				neighbors[count++] = (nx, ny);
			}
		}
		return (neighbors, count);
	}

	override public void update(Element[,] oldElementArray, Element[,] currentElementArray, int x, int y, int maxX, int maxY, int T)
	{
		if (T - lastActivity < activityInterval && !needsUpdate) // Skip update if not enough time has passed and no external change has occurred
		{
			PowderBehavior.Update(this, oldElementArray, currentElementArray, x, y, maxX, maxY, T); // still needs to update for other base behaviors, but we skip the soil-specific updates
			return;
		}

		lastActivity = T;
		needsUpdate = false;
		
		// this is for optimisation purposes, all the indices are valid.
		((int, int)[] neighborsIndices, int count) = getNeighborsIndices(currentElementArray, x, y, maxX, maxY);

		// Handle nutrient diffusion
		for (int i = 0; i < count; i++)
		{
			int nx = neighborsIndices[i].Item1;
			int ny = neighborsIndices[i].Item2;
		
			// Again using the newElementArray as read is a bit problematic for a deterministic simulation, but fake it until you make it as they say.
			float nutriDiff = (currentElementArray[nx, ny] as Soil).nutrient - nutrient;
			if (nutriDiff > 0.1f) // Only transfer if significant difference
			{
				float transferAmount = nutriDiff * 0.1f; // Slower transfer rate
				float maxTransfer = Math.Min(transferAmount, nutrient * 0.3f); // Limit how much can be transferred

				nutrient -= maxTransfer;
				(currentElementArray[nx, ny] as Soil).nutrient += maxTransfer;
				(currentElementArray[nx, ny] as Soil).needsUpdate = true; // Mark neighbor soil for update
			}

		}

		if (wetness > 0.3f) // Only propagate if we have significant wetness
		{
			for (int i = 0; i < count; i++)
			{
				int nx = neighborsIndices[i].Item1;
				int ny = neighborsIndices[i].Item2;
			
				float wetnessDiff = wetness - (currentElementArray[nx, ny] as Soil).wetness;
				if (wetnessDiff > 0.1f) // Only transfer if significant difference and if wetness greater than neighbor's
				{
					float transferAmount = wetnessDiff * 0.1f; // Slower transfer rate
					float maxTransfer = Math.Min(transferAmount, wetness * 0.3f); // Limit how much can be transferred

					wetness -= maxTransfer;
					(currentElementArray[nx, ny] as Soil).wetness += maxTransfer;
					(currentElementArray[nx, ny] as Soil).needsUpdate = true; // Mark neighbor soil for update
				}
			}
		}

		// Handle water absorption from adjacent and above water elements
		(int, int)[] waterNeighbors =  { (0, 1), (1, 0), (-1, 0)};
		foreach ((int nx, int ny) in waterNeighbors)
		{
			int neighborX = x + nx;
			int neighborY = y + ny;
			if (neighborX >= 0 && neighborX < maxX && neighborY >= 0 && neighborY < maxY)
			{
				if (oldElementArray[neighborX, neighborY] is Element water && water is ILiquid)
				{
					float wetnessCap = Math.Min(water.wetness, 1 - wetness); // only absorb what we can take
					water.wetness -= wetnessCap;
					wetness += wetnessCap;
				}
			}
		}

		updateColor(T);
		PowderBehavior.Update(this, oldElementArray, currentElementArray, x, y, maxX, maxY, T); // keep at the end because of returns contained in base method
	}

	override public string getState()
	{
		return base.getState() + ";" + wetness + ";" + nutrient;
	}

	override public int setState(string state)
	{
		int i = 0;
		string[] stateArgs = state.Split(";", false);
		wetness = stateArgs[i++].ToFloat();
		nutrient = stateArgs[i++].ToFloat();
		return i;
	}

	override public string inspectInfo()
	{
		return base.inspectInfo() + $"  Wetness: {wetness:F3}\n  Nutrient: {nutrient:F3}\n";
	}
}
