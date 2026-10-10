using System;
using System.ComponentModel;
using Godot;



class FireParticle : Element
{
    public int lifetime { get; set; } // ticks

    public FireParticle()
    {
        density = 0.1f;
        color = Colors.OrangeRed;
        lifetime = Random.Shared.Next(300, 600); // random lifetime between 30 and 60 ticks
        modulateColor(0.2f);
    }
    public override void update(Element[,] oldGrid, int x, int y, int maxX, int maxY, int T)
    {
        UpdateManager.Instance.RequestUpdateNextFrame(x, y); // request an update for the fire particle every frame

        lifetime--;
        if (lifetime <= 0)
        {
            if (Random.Shared.NextDouble() < 0.5)
            {
                GridManager.Instance.RequestDeletion(x, y, maxX, maxY, new Smoke());
            }
            else
            {
                GridManager.Instance.RequestDeletion(x, y, maxX, maxY);
            }
            return;
        }

        // move upwards, if blocked, delete the fire particle
        MoveManager.Instance.AttemptMove(oldGrid, x, y, 0, -1, maxX, maxY);
        if (y - 1 >= 0)
        {
            if (oldGrid[x, y-1] is not (IGas or FireParticle)){
                lifetime = 0; // set lifetime to 0 to trigger deletion
            }
        } else {
            lifetime = 0; // on the top edge, set lifetime to 0 to trigger deletion
        }

        updateColor(T, x, y);
    }
}