using System;

[Flags]
public enum WalkableType : byte
{
    Walkable = 0,
    Obstacle = 1 << 0,
    Roof = 1 << 1,
    Air = 1 << 2
}