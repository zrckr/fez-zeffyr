using System;
using Godot;

namespace Zeffyr
{
    public class NodeAttribute : Attribute
    {
        public readonly string NodePath;

        public readonly bool CanBeNull;

        public NodeAttribute(string nodePath, bool canBeNull)
            => (NodePath, CanBeNull) = (nodePath, canBeNull);

        public NodeAttribute(string nodePath) : this(nodePath, false) { }
        
        public NodeAttribute(bool canBeNull) : this(null, canBeNull) { }
        
        public NodeAttribute() : this(null, false) { }
    }

    public class RootNodeAttribute : Attribute
    {
        public readonly string AutoloadName;

        public RootNodeAttribute(string name) => AutoloadName = name;
        
        public RootNodeAttribute() : this(null) { }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class CameraHelperAttribute : Attribute
    {
        public readonly Type HelperType; 
        
        public CameraHelperAttribute(Type helper) => HelperType = helper;
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class ConsoleCommandAttribute : Attribute
    {
        public readonly string Command;
        public ConsoleCommandAttribute(string command) => Command = command;
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class ConsoleDescriptionAttribute : Attribute
    {
        public readonly string Description;
        public ConsoleDescriptionAttribute(string desc) => Description = desc;
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class ConsoleArgumentAttribute : Attribute
    {
        public readonly string Argument;
        public readonly Variant.Type Type;

        public ConsoleArgumentAttribute(string arg, Variant.Type type = Variant.Type.Object)
            => (Argument, Type) = (arg, type);
    }
}