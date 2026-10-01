using System;
using Godot;
public abstract class Element
{
	public Color color { get; set; }
	public bool needUpdate { get; set; } = true;
	public Color baseColor { get; protected set; }
	public double density { get; protected set; }
	
	public virtual void update(Element[,] oldGrid, int x, int y, int maxX, int maxY, int T)
	{
	}

	/// <summary>
	/// To call at the beginning of the possible override function
	/// </summary>
	public virtual void updateColor(int T, int x, int y)
	{
		// check if baseColor is initialized, should only happen once (i hope)
		if (baseColor == default)
		{
			baseColor = color;
		}
		color = baseColor; // reset to base color before applying effects

		if (this is IFlammable burningElement)
		{
			float lerpIntensity = Math.Max(0, (float)Math.Sin((T + x * y) / 10));
			Color fireHue = Colors.Red.Lerp(Colors.Orange, lerpIntensity);
			//fireHue = fireHue.Lerp(Colors.DarkOrange, Math.Max(1, 200 / (float)burningLifetime)); // longer burning -> darker fire color
			color = baseColor.Lerp(fireHue, 0.4f); // blend both effects
		}
	}

	virtual public string getState()
	{
		// This function gives a String, that are needed to create
		// a perfect copy of the Element
		// If it is overidden (and it outputs a String)
		// setState must also be overwritten.
		// The strings cannot use either a "space" or a "|"
		return null;
	}

	virtual public void modulateColor(float intensity = 0.05f){
		float z = Random.Shared.NextSingle() * intensity;
		color = color.Darkened(z);
	}

	virtual public int setState(string state)
	{
		int i = 0;
		string[] stateArgs = state.Split(";", false);
		return i;
	}

	virtual public string inspectInfo()
	{
		string output = $"Density: {density}\n";
		if (this is IFlammable burningElement)
		{
			output += $"  Flammability: {burningElement.flammability}\n  Burning: {burningElement.burning}\n  Burning Lifetime: {burningElement.burningLifetime}\n";
		}
		
		if (this is ILife lifeElement)
		{
			output += $"  Nutrient: {lifeElement.nutrient}\n  Wetness: {lifeElement.wetness}\n";
		}
		
		return output;
	}

}
