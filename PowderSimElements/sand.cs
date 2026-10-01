using System;
using Godot;

public class Sand : Element, IPowder, ISolid
{
	public Sand()
	{
		density = 20;
		color = Colors.Yellow;
		modulateColor(0.2f);
	}

	public override void modulateColor(float intensity = 0.05F)
	{
		float w = Random.Shared.NextSingle() * intensity;
		color = color.Lightened(w);
		float z = Random.Shared.NextSingle() * intensity;
		color = color.Darkened(z);
	}

	public override void update(Element[,] oldGrid, int x, int y, int maxX, int maxY, int T)
	{
		PowderBehavior.Update(this, oldGrid, x, y, maxX, maxY, T);
	}
}
