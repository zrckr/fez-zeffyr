using Godot;
using Coroutine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace Zeffyr
{
    public static class NodeExtensions
    {
        private const BindingFlags Flags = BindingFlags.DeclaredOnly | BindingFlags.Public |
                                          BindingFlags.NonPublic | BindingFlags.Instance;

        public static void InjectNodes(this Node node, Type baseType = null)
        {
            var type = baseType ?? node.GetType();
            FieldInfo[] ownFields = type.GetFields(Flags);
            PropertyInfo[] ownProperties = type.GetProperties(Flags);

            var memberInfos = ownFields.Concat<MemberInfo>(ownProperties);
            foreach (var member in memberInfos)
            {
                Attribute[] attributes = Attribute.GetCustomAttributes(member);
                if (attributes.Length == 0) continue;
                
                Node nodeInstance = null;
                foreach (Attribute attribute in attributes)
                {
                    switch (attribute)
                    {
                        case NodeAttribute nodeAttr:
                            string path = string.IsNullOrEmpty(nodeAttr.NodePath) 
                                ? member.Name 
                                : nodeAttr.NodePath;

                            nodeInstance = (nodeAttr.CanBeNull)
                                ? node.GetNodeOrNull(path)
                                : node.GetNode(path);
                            break;
                        
                        case RootNodeAttribute rootAttr:
                            path = "/root/" + (string.IsNullOrEmpty(rootAttr.AutoloadName)
                                ? member.Name
                                : rootAttr.AutoloadName);
                            nodeInstance = node.GetNodeOrNull(path);
                            break;
                        
                        case CameraHelperAttribute cameraAttr:
                            Node parent = node.GetViewport().GetCamera()?.GetParent();
                            if (parent?.GetType() == cameraAttr.HelperType)
                                nodeInstance =  parent;
                            break;
                    }
                }

                if (nodeInstance != null)
                {
                    switch (member)
                    {
                        case FieldInfo fieldInfo:
                            fieldInfo.SetValue(node, nodeInstance);
                            break;
                        case PropertyInfo propertyInfo:
                            propertyInfo.SetValue(node, nodeInstance, null);
                            break;
                        default:
                            throw new ArgumentException("MemberInfo must be if type FieldInfo or PropertyInfo",
                                nameof(member));
                    }
                }
            }

            if (type.BaseType != typeof(Node))
                InjectNodes(node, type.BaseType);
        }

        public static void RegisterCommands(this Node node, Type baseType = null)
        {
            var type = baseType ?? node.GetType();
            MethodInfo[] ownMethods = type.GetMethods(Flags);

            var console = node.GetNodeOrNull<CanvasLayer>("/root/Console");
            foreach (var methodInfo in ownMethods)
            {
                var command =
                    Attribute.GetCustomAttribute(methodInfo,
                        typeof(ConsoleCommandAttribute)) as ConsoleCommandAttribute;
                var desc =
                    Attribute.GetCustomAttribute(methodInfo, typeof(ConsoleDescriptionAttribute)) as
                        ConsoleDescriptionAttribute;
                var args = Attribute.GetCustomAttributes(methodInfo, typeof(ConsoleArgumentAttribute))
                    .OfType<ConsoleArgumentAttribute>().ToArray();

                if (command == null && (desc != null || args.Length != 0))
                    throw new NullReferenceException($"Cannot find ConsoleCommand attribute!");
                if (command == null)
                    continue;

                var ref1 = console.Call("add_command", command.Command, node, methodInfo.Name) as Reference;
                ref1 = ref1?.Call("set_description", desc?.Description) as Reference;

                foreach (ConsoleArgumentAttribute arg in args)
                    ref1 = ref1?.Call("add_argument", arg.Argument, arg.Type) as Reference;

                ref1?.Call("register");
            }

            if (type.BaseType != typeof(Node))
                RegisterCommands(node, type.BaseType);
        }
        
        public static List<Node> GetNodesInGroups(this Node node, params string[] groups)
        {
            List<Node> result = new List<Node>();
            foreach (string group in groups)
            {
                IEnumerable<Node> temp = node.GetTree().GetNodesInGroup(group).Cast<Node>();
                result.AddRange(temp);
            }
            return result;
        }

        public static List<T> GetChildrenList<T>(this Node node)
        {
            if (node is null)
                return new List<T>();
            return node.GetChildren().OfType<T>().ToList();
        }

        public static IEnumerable<T> GetChildrenRecursive<T>(this Node node)
        {
            foreach (Node child in node.GetChildren())
            {
                if (child is T t1)
                {
                    yield return t1;
                }

                if (child.GetChildCount() > 0)
                {
                    foreach (T t2 in child.GetChildrenRecursive<T>())
                    {
                        yield return t2;
                    }
                }
            }
        }

        public static ActiveCoroutine Initialize(this CoroutineHandlerInstance handler)
        {
            IEnumerator<Wait> emptyWait = Enumerable.Empty<Wait>().GetEnumerator();
            return handler.Start(emptyWait);
        }

        public static void AdoptChild(Node node, Spatial child)
        {
            Transform transform = child.GlobalTransform;
            child.GetParent().RemoveChild(child);
            node.AddChild(child);
            child.GlobalTransform = transform;
        }
    }
}