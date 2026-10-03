using Godot;

using System;

public class Biomass : Element, IPowder, ISolid, ILife
{
	// biomass can be created from anything, so it can have high wetness and nutrient to not have any loss
	public float wetness { get; set; }
	public float maxWetness => 10000.0f; 
	public float nutrient { get; set; }
	public float maxNutrient => 10000.0f;
	public Biomass(float startingWetness, float startingNutrient)
	{
		density = 20;
		color = Colors.Khaki;
		wetness = startingWetness;
		nutrient = startingNutrient;
		modulateColor();
	}
	
	public override void update(Element[,] oldGrid, int x, int y, int maxX, int maxY, int T)
	{
		if (nutrient <= 0.0f && wetness <= 0.0f)
		{
			GridManager.Instance.RequestDeletion(x, y, maxX, maxY);
			return;
		}
		PowderBehavior.Update(this, oldGrid, x, y, maxX, maxY, T);
	}

	override public string getState()
	{
		return base.getState() + ";" + wetness + ";" + nutrient;
	}

	override public int setState(string state)
	{
		int i = base.setState(state);
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
