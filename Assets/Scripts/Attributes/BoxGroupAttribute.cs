using UnityEngine;
using System;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class BoxGroupAttribute : PropertyAttribute
{
    public string GroupName;

    public BoxGroupAttribute(string groupName)
    {
        GroupName = groupName;
    }
}