using System;
using System.Collections.Generic;
using System.Reflection;

namespace Ioc233
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class AutowiredAttribute : Attribute
    {
        public string? Name { get; set; }
        public bool Required { get; set; } = true;
    }

    public interface IProvideAfter { void OnProvideAfter(); }
    public interface IInjectBefore { void OnInjectBefore(); }
    public interface IInjectAfter { void OnInjectAfter(); }
    public interface IObject { void OnInjectComplete(); }

    /// <summary>Explicit instance registration and startup injection. Startup belongs to the owning thread.</summary>
    public sealed class Container233
    {
        private enum Phase { Registering, Starting, Started, Faulted }
        private Phase _phase;
        private readonly Dictionary<string, object> _objects = new Dictionary<string, object>(StringComparer.Ordinal);

        public void Provide(object instance, string? name = null)
        {
            if (_phase != Phase.Registering) throw new InvalidOperationException("Container registration is closed.");
            if (instance is null) throw new ArgumentNullException(nameof(instance));
            if (instance.GetType().IsValueType) throw new ArgumentException("Register reference type instances.", nameof(instance));
            name = name ?? instance.GetType().Name;
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A service name is required.", nameof(name));
            foreach (var value in _objects.Values)
                if (ReferenceEquals(value, instance)) throw new ArgumentException("Instance already registered.", nameof(instance));
            _objects.Add(name, instance);
            try { (instance as IProvideAfter)?.OnProvideAfter(); }
            catch { _phase = Phase.Faulted; throw; }
        }

        public T Get<T>(string? name = null) where T : class
        {
            if (_phase == Phase.Faulted) throw new InvalidOperationException("Container startup failed; create a new container.");
            return (T)Resolve(typeof(T), name, true)!;
        }

        private object? Resolve(Type type, string? name, bool required)
        {
            object? result = null;
            if (name != null)
            {
                if (_objects.TryGetValue(name, out result) && !type.IsInstanceOfType(result))
                    throw new InvalidOperationException($"Service '{name}' is not assignable to {type.FullName}.");
            }
            else
            {
                foreach (var instance in _objects.Values)
                {
                    if (!type.IsInstanceOfType(instance)) continue;
                    if (result != null) throw new InvalidOperationException($"Multiple services match {type.FullName}; specify a name.");
                    result = instance;
                }
            }
            if (result is null && required) throw new InvalidOperationException($"Missing service: {name ?? type.FullName}.");
            return result;
        }

        public void StartUp()
        {
            if (_phase == Phase.Started) return;
            if (_phase != Phase.Registering) throw new InvalidOperationException("Container cannot be started in its current state.");
            _phase = Phase.Starting;
            try
            {
                // Validate every dependency before assigning any field or executing startup callbacks.
                var assignments = new List<Action>();
                foreach (var instance in _objects.Values) PlanInjection(instance, assignments);
                foreach (var instance in _objects.Values) (instance as IInjectBefore)?.OnInjectBefore();
                foreach (var assign in assignments) assign();
                foreach (var instance in _objects.Values) (instance as IInjectAfter)?.OnInjectAfter();
                foreach (var instance in _objects.Values) (instance as IObject)?.OnInjectComplete();
                _phase = Phase.Started;
            }
            catch { _phase = Phase.Faulted; throw; }
        }

        private void PlanInjection(object instance, List<Action> assignments)
        {
            for (Type? type = instance.GetType(); type != null && type != typeof(object); type = type.BaseType)
            {
                const BindingFlags flags = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
                foreach (var field in type.GetFields(flags))
                {
                    var attr = field.GetCustomAttribute<AutowiredAttribute>();
                    if (attr is null) continue;
                    if (field.IsStatic || field.IsInitOnly || field.FieldType.IsValueType)
                        throw new InvalidOperationException($"Autowired field {field.Name} must be a writable instance reference.");
                    var value = Resolve(field.FieldType, attr.Name, attr.Required);
                    if (value != null) assignments.Add(() => field.SetValue(instance, value));
                }
                foreach (var property in type.GetProperties(flags))
                {
                    var attr = property.GetCustomAttribute<AutowiredAttribute>();
                    if (attr is null) continue;
                    var setter = property.GetSetMethod(true);
                    if (setter is null || setter.IsStatic || property.GetIndexParameters().Length != 0 || property.PropertyType.IsValueType)
                        throw new InvalidOperationException($"Autowired property {property.Name} must be a writable instance reference.");
                    var value = Resolve(property.PropertyType, attr.Name, attr.Required);
                    if (value != null) assignments.Add(() => property.SetValue(instance, value));
                }
            }
        }
    }
}
