using Godot;
class Stone : Element, ISolid
{
    public Stone()
    {
        density = 10;
        color = Colors.DarkGray;
        modulateColor(0.05f);
    }
}