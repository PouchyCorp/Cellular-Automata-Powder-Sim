using System;
using Godot;
public abstract class Element
{
	public Color color { get; set; }
	public bool needUpdate { get; set; } = true;
	public Color baseColor { get; protected set; }
	public double density { get; protected set; }

	public virtual bool canMoveDownOnElement(Element elementWhereMovement)
	{
		return elementWhereMovement == null || elementWhereMovement.density < density;
	}

	public virtual bool canMoveUpOnElement(Element elementWhereMovement)
	{
		return elementWhereMovement == null || elementWhereMovement.density > density;
	}

	public virtual bool canMoveSideOnElement(Element elementWhereMovement)
	{
		return elementWhereMovement == null || elementWhereMovement.density < density;
	}

	public virtual bool move(Element[,] oldElementArray, Element[,] currentElementArray, int x, int y, int maxX, int maxY, int movementX, int movementY)
	{
		int newX = x + movementX, newY = y + movementY;

		if (newX >= maxX || newX < 0 ||
			newY >= maxY || newY < 0) return false;

		if (
				(movementY > 0 &&
				canMoveDownOnElement(oldElementArray[newX, newY]))
			||
				(movementY < 0 &&
				canMoveUpOnElement(oldElementArray[newX, newY]))
			||
				(movementY == 0 &&
				canMoveSideOnElement(oldElementArray[newX, newY]))
			)
		{
			currentElementArray[x, y] = currentElementArray[newX, newY];
			currentElementArray[newX, newY] = this;
			return true;
		}


		return false;
	}
	public virtual void update(Element[,] oldElementArray, int x, int y, int maxX, int maxY, int T)
	{
	}

	/// <summary>
	/// To call at the beginning of the possible override function
	/// </summary>
	public virtual void updateColor(int T)
	{
		// check if baseColor is initialized, should only happen once (i hope)
		if (baseColor == default)
		{
			baseColor = color;
		}
		color = baseColor; // reset to base color before applying effects

		if (burning)
		{
			float lerpIntensity = Math.Max(0, (float)Math.Sin((T + (int)rng.Seed) / 10));
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
		if (flammability > 0) return burning + ";" + burningLifetime;
		else return null;
	}

	virtual public void modulateColor(float intensity = 0.05f){
		float z = Random.Shared.NextSingle() * intensity;
		color = color.Darkened(z);
	}

	virtual public int setState(string state)
	{
		int i = 0;
		string[] stateArgs = state.Split(";", false);
		burning = stateArgs[i++] == "True";
		burningLifetime = stateArgs[i++].ToInt();
		return i;
	}

	virtual public string inspectInfo()
	{
		string attributes = $"  Flammability: {flammability}\n";
		attributes += $"  Wetness: {wetness:F3}\n";
		attributes += $"  Burning: {burning}\n";

		if (burning)
		{
			attributes += $"  Burning Lifetime: {burningLifetime}\n";
		}
		return attributes;
	}

}
