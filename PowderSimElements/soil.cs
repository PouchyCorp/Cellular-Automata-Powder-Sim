using Godot;
using System;
using System.Data;


public class Soil : Element, IPowder, ISolid, ILife
{
	int lastActivity = 0;
	int activityInterval = 30;
	public float nutrient { get; set; } = 0f;
	public float maxNutrient => 1000f;
	public float wetness { get; set; } = 0f;


	public (int, int)[] cardinals = [(0, 1), (1, 0), (0, -1), (-1, 0)];
	new private Color baseColor = Color.FromHtml("#8d7267ff");
	private Color wetColor = Color.FromHtml("#3a1008ff");
	private Color richColor = Color.FromHtml("#394e35ff");

	public Soil()
	{
		density = 20;
		color = baseColor;
		modulateColor();
	}

	public override void modulateColor(float intensity = 0.05F)
	{
		float z = Random.Shared.NextSingle() * intensity;
		baseColor = baseColor.Darkened(z);
	}

	override public void updateColor(int T, int x, int y)
	{
		base.updateColor(T, x, y);

		Color nutriHue = baseColor.Lerp(richColor, Math.Min(nutrient, 2)); // more nutrient = darker color
		Color wetHue = baseColor.Lerp(wetColor, wetness); // more wet = darker color

		color = nutriHue.Lerp(wetHue, 0.5f); // blend both effects TODO: adjust colors
	}

	((int, int)[], int) getNeighborsIndices(Element[,] currentGrid, int x, int y, int maxX, int maxY)
	{
		(int, int)[] neighbors = new (int, int)[4];
		int count = 0;
		for (int i = 0; i < 4; i++)
		{
			int nx = x + cardinals[i].Item1;
			int ny = y + cardinals[i].Item2;

			if (nx >= 0 && nx < maxX && ny >= 0 && ny < maxY && currentGrid[nx, ny] is Soil)
			{
				neighbors[count++] = (nx, ny);
			}
		}
		return (neighbors, count);
	}

	override public void update(Element[,] oldGrid, int x, int y, int maxX, int maxY, int T)
	{
		if (T - lastActivity < activityInterval) // Skip update if not enough time has passed and no external change has occurred
		{
			updateColor(T, x, y);
			PowderBehavior.Update(this, oldGrid, x, y, maxX, maxY, T); // still needs to update for other base behaviors, but we skip the soil-specific updates
			return;
		}

		lastActivity = T;
		
		// this is for optimisation purposes, all the indices are valid.
		((int, int)[] neighborsIndices, int count) = getNeighborsIndices(oldGrid, x, y, maxX, maxY);

		// Handle nutrient diffusion
		for (int i = 0; i < count; i++)
		{
			int nx = neighborsIndices[i].Item1;
			int ny = neighborsIndices[i].Item2;
		
			// Again using the newElementArray as read is a bit problematic for a deterministic simulation, but fake it until you make it as they say.
			float nutriDiff = (oldGrid[nx, ny] as Soil).nutrient - nutrient;
			if (nutriDiff < -0.1f)
			{
				float transferAmount = Math.Abs(nutriDiff) * 0.1f;

				if (nutriDiff < 0)
				{
					// Current soil is richer: give nutrients to the poorer neighbor.
					NutrientManager.Instance.AddGiveNutrientRequest(new GiveNutrientRequest(x, y, nx, ny, transferAmount), maxX, maxY);
				}
			}

		}

		if (wetness > 0.3f) // Only propagate if we have significant wetness
		{
			for (int i = 0; i < count; i++)
			{
				int nx = neighborsIndices[i].Item1;
				int ny = neighborsIndices[i].Item2;
			
				float wetnessDiff = (oldGrid[nx, ny] as Soil).wetness - wetness;
				if (wetnessDiff < -0.1f)
				{
					float transferAmount = Math.Abs(wetnessDiff) * 0.1f;

					if (wetnessDiff < 0)
					{
						// Current soil is wetter: give wetness to the drier neighbor.
						NutrientManager.Instance.AddGiveWetnessRequest(new GiveWetnessRequest(x, y, nx, ny, transferAmount), maxX, maxY);
					}
				}
			}
		}

		// Handle water absorption from adjacent and above water elements
		(int, int)[] waterNeighbors =  { (0, -1), (1, 0), (-1, 0)};
		foreach ((int nx, int ny) in waterNeighbors)
		{
			int neighborX = x + nx;
			int neighborY = y + ny;
			if (neighborX >= 0 && neighborX < maxX && neighborY >= 0 && neighborY < maxY)
			{
				if (oldGrid[neighborX, neighborY] is Water)
				{
					NutrientManager.Instance.AddTakeWetnessRequest(new TakeWetnessRequest(x, y, neighborX, neighborY, 1.0f), maxX, maxY);
				}
			}
		}

		updateColor(T, x, y);
		PowderBehavior.Update(this, oldGrid, x, y, maxX, maxY, T); // keep at the end because of returns contained in base method
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
