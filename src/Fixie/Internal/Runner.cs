﻿namespace Fixie.Internal
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;

    class Runner
    {
        readonly Bus bus;
        readonly string[] customArguments;

        public Runner(Bus bus)
            : this(bus, new string[] {}) { }

        public Runner(Bus bus, string[] customArguments)
        {
            this.bus = bus;
            this.customArguments = customArguments;
        }

        public ExecutionSummary RunAssembly(Assembly assembly)
        {
            return Run(assembly, assembly.GetTypes());
        }

        public ExecutionSummary RunNamespace(Assembly assembly, string ns)
        {
            return Run(assembly, assembly.GetTypes().Where(type => type.IsInNamespace(ns)).ToArray());
        }

        public ExecutionSummary RunType(Assembly assembly, Type type)
        {
            return Run(assembly, GetTypeAndNestedTypes(type).ToArray());
        }

        public ExecutionSummary RunTypes(Assembly assembly, Discovery discovery, Execution execution, params Type[] types)
        {
            return Run(assembly, discovery, execution, types);
        }

        public ExecutionSummary RunTests(Assembly assembly, Test[] tests)
        {
            var types = GetTypes(assembly, tests);

            var methods = GetMethods(types, tests);

            return Run(assembly, types.Values.ToArray(), methods.Contains);
        }

        public ExecutionSummary RunMethod(Assembly assembly, MethodInfo method)
        {
            return Run(assembly, new[] { method.ReflectedType }, m => m == method);
        }

        static IEnumerable<Type> GetTypeAndNestedTypes(Type type)
        {
            yield return type;

            foreach (var nested in type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic).SelectMany(GetTypeAndNestedTypes))
                yield return nested;
        }

        static Dictionary<string, Type> GetTypes(Assembly assembly, Test[] tests)
        {
            var types = new Dictionary<string, Type>();

            foreach (var test in tests)
                if (!types.ContainsKey(test.Class))
                    types.Add(test.Class, assembly.GetType(test.Class));

            return types;
        }

        static MethodInfo[] GetMethods(Dictionary<string, Type> classes, Test[] tests)
        {
            return tests
                .SelectMany(test =>
                    classes[test.Class]
                        .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                        .Where(m => m.Name == test.Method)).ToArray();
        }

        ExecutionSummary Run(Assembly assembly, Type[] candidateTypes, Func<MethodInfo, bool> methodCondition = null)
        {
            new BehaviorDiscoverer(assembly, customArguments)
                .GetBehaviors(out var discovery, out var execution);

            try
            {
                if (methodCondition != null)
                    discovery.Methods.Where(methodCondition);

                return Run(assembly, discovery, execution, candidateTypes);
            }
            finally
            {
                discovery.Dispose();

                if (execution != discovery)
                    execution.Dispose();
            }
        }

        ExecutionSummary Run(Assembly assembly, Discovery discovery, Execution execution, Type[] candidateTypes)
        {
            bus.Publish(new AssemblyStarted(assembly));

            var assemblySummary = new ExecutionSummary();
            var stopwatch = Stopwatch.StartNew();

            Run(discovery, execution, candidateTypes, assemblySummary);

            stopwatch.Stop();
            bus.Publish(new AssemblyCompleted(assembly, assemblySummary, stopwatch.Elapsed));

            return assemblySummary;
        }

        void Run(Discovery discovery, Execution execution, Type[] candidateTypes, ExecutionSummary assemblySummary)
        {
            var classDiscoverer = new ClassDiscoverer(discovery);
            var classRunner = new ClassRunner(bus, discovery, execution);

            var testClasses = classDiscoverer.TestClasses(candidateTypes);

            bool isOnlyTestClass = testClasses.Count == 1;

            foreach (var testClass in testClasses)
            {
                var classSummary = classRunner.Run(testClass, isOnlyTestClass);
                assemblySummary.Add(classSummary);
            }
        }
    }
}