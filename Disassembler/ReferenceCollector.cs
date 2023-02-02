using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Disassembler
{
    /**
     * This class is responsible for collecting references from top level types and adding them
     * to the SourceType.
     * It handles the resursion in to some types and also drives the 
     * processing of the IL code in ProcessMethodBody.
     */
    internal class ReferenceCollector
    {
        private readonly SourceType sourceType;
        private readonly IErrorReporter errorReporter;

        public ReferenceCollector(SourceType type, IErrorReporter errorReporter)
        {
            sourceType = type;
            this.errorReporter = errorReporter;
        }

        /**
         * Reads the IL code from the method body and processes it for references
         * with the OpCodeReader class.
         */
        private void ProcessMethodBody(MethodBase method, MethodBody body, Module module)
        {
            byte[]? il = body.GetILAsByteArray();
            if (il != null)
            {
                OpCodeReader reader = new(method, il, module, this, errorReporter);
                reader.Read();
            }
        }

        /**
         * Given a type, it reads the generic arguments and adds references from them
         */
        private void GetReferencesFromType(Type type)
        {
            foreach (var arg in type.GenericTypeArguments)
            {
                ReferenceTypeAndArgs(arg);
            }
        }

        /**
         * Given a type, add a reference to it and it's generic arguments
         */
        public void ReferenceTypeAndArgs(Type type)
        {
            ReferenceType(type);
            GetReferencesFromType(type);

        }

        /**
         * Given a type, if it is not a well known System or .NET support package, 
         * add a reference to it.
         */
        public void ReferenceType(Type type)
        {
            if (type.FullName != null && !type.FullName.StartsWith("Microsoft.Net.Http") && !type.FullName.StartsWith("System.") && !type.FullName.StartsWith("Microsoft.AspNetCore."))
            {
                sourceType.TypeReferences.Add(type);
            }
        }
        /**
         * Given a method, add references for it's 
         * return type, parameters, and generic arguments.
         * It then reads the method body's IL code for further
         * references.
         */
        public void ProcessMethod(MethodInfo method, Type mainType)
        {
            if (method.DeclaringType != mainType)
            {
                return;
            }
            ReferenceTypeAndArgs(method.ReturnType);
            foreach (var generic in method.GetGenericArguments())
            {
                ReferenceTypeAndArgs(generic);
            }
            foreach (var parameter in method.GetParameters())
            {
                ReferenceTypeAndArgs(parameter.ParameterType);
            }
            var body = method.GetMethodBody();
            if (body != null)
            {
                ProcessMethodBody(method, body, method.Module);
            }
        }

        /**
         * Private version of CollectReferences used to recurse in to types
         * where appropriate.  In general we don't want to drill in to base classes,
         * as that would result in transitive dependencies.  We are however interested
         * in any generic arguments they have, as well as having them as a top level dependency.
         */
        private void DoCollectReferences(Type type)
        {
            GetReferencesFromType(type);
            if (type.BaseType != null)
            {
                ReferenceTypeAndArgs(type.BaseType);
            }
            var interfaces = type.GetInterfaces();
            foreach (var iface in interfaces)
            {
                ReferenceTypeAndArgs(iface);
            }
            /*
             * Get all public and non public static and instance member of the top level type only.  Don't use inherited members.
             */
            var members = type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (var member in members)
            {
                switch (member.MemberType)
                {
                    case MemberTypes.Field:
                        var fieldInfo = (FieldInfo)member;
                        if (fieldInfo.DeclaringType == type)
                        {
                            ReferenceTypeAndArgs(fieldInfo.FieldType);
                        }
                        break;
                    case MemberTypes.Property:
                        var propertyInfo = (PropertyInfo)member;
                        //Make sure it is a top level definition, not a base member
                        if (propertyInfo.DeclaringType == type)
                        {
                            ReferenceTypeAndArgs(propertyInfo.PropertyType);
                            //process the getters and setters of properties
                            //as methods.
                            var setter = propertyInfo.GetSetMethod();
                            if (setter != null)
                            {
                                ProcessMethod(setter, type);
                            }
                            var getter = propertyInfo.GetGetMethod();
                            if (getter != null)
                            {
                                ProcessMethod(getter, type);
                            }
                        }
                        break;
                    case MemberTypes.Event:
                        var eventInfo = (EventInfo)member;
                        //Make sure it is a top level definition, not a base member
                        if (eventInfo.DeclaringType == type)
                        {
                            //These are the possible methods attached to 
                            //an event type.
                            if (eventInfo.RaiseMethod != null)
                            {
                                ProcessMethod(eventInfo.RaiseMethod, type);
                            }
                            if (eventInfo.AddMethod != null)
                            {
                                ProcessMethod(eventInfo.AddMethod, type);
                            }
                            if (eventInfo.RemoveMethod != null)
                            {
                                ProcessMethod(eventInfo.RemoveMethod, type);
                            }
                            if (eventInfo.EventHandlerType != null)
                            {
                                ReferenceTypeAndArgs(eventInfo.EventHandlerType);
                            }
                        }
                        break;
                    case MemberTypes.NestedType:
                        //Nested types can't be referenced outside of the type
                        //ignore them.
                        break;
                    case MemberTypes.Method:
                        var methodInfo = (MethodInfo)member;
                        ProcessMethod(methodInfo, type);
                        break;
                    case MemberTypes.Constructor:
                        var constructorInfo = (ConstructorInfo)member;
                        if (constructorInfo.DeclaringType == type)
                        {
                            foreach (var parameter in constructorInfo.GetParameters())
                            {
                                ReferenceTypeAndArgs(parameter.ParameterType);
                            }
                            var body = constructorInfo.GetMethodBody();
                            if (body != null)
                            {
                                ProcessMethodBody(constructorInfo, body, constructorInfo.Module);
                            }
                        }
                        break;
                }
            }
        }

        /**
         * Collects references for the sourceType provided to the constructor.
         * Recurses in to nested types and all members.
         */
        public void CollectReferences()
        {
            if (sourceType.AssemblyType != null)
            {
                DoCollectReferences(sourceType.AssemblyType);
            }

        }

    }
}
