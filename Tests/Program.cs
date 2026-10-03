using Ioc233;

var passed = 0;
void Check(string name, Action test) { test(); passed++; Console.WriteLine("PASS " + name); }
void Assert(bool value) { if (!value) throw new Exception("Assertion failed."); }
void Throws<T>(Action action) where T : Exception
{
    try { action(); } catch (T) { return; }
    throw new Exception("Expected " + typeof(T).Name);
}

Check("interface, name, private base field and optional injection", () =>
{
    var container = new Container233(); var clock = new Clock(); var service = new Service();
    container.Provide(clock, "clock"); container.Provide(service);
    container.StartUp();
    Assert(ReferenceEquals(service.Clock, clock) && ReferenceEquals(service.BaseClock, clock));
    Assert(service.Optional == null && service.Ready && ReferenceEquals(container.Get<IClock>(), clock));
});

Check("lifecycle order and idempotent startup", () =>
{
    var container = new Container233(); var service = new Service();
    container.Provide(new Clock(), "clock"); container.Provide(service); container.StartUp(); container.StartUp();
    Assert(string.Join(",", service.Events) == "provide,before,after,complete");
    Throws<InvalidOperationException>(() => container.Provide(new Clock()));
});

Check("all dependencies validated before assignments", () =>
{
    var container = new Container233(); var service = new Service();
    container.Provide(new Clock(), "clock"); container.Provide(service); container.Provide(new Missing());
    Throws<InvalidOperationException>(() => container.StartUp());
    Assert(service.Clock == null && service.Events.Count == 1);
    Throws<InvalidOperationException>(() => container.StartUp());
});

Check("ambiguous interface requires an explicit name", () =>
{
    var container = new Container233(); container.Provide(new Clock(), "a"); container.Provide(new Clock(), "b");
    Throws<InvalidOperationException>(() => container.Get<IClock>());
    Assert(container.Get<IClock>("a") != null);
});

Check("duplicate names and duplicate instances rejected", () =>
{
    var container = new Container233(); var clock = new Clock(); container.Provide(clock, "a");
    Throws<ArgumentException>(() => container.Provide(clock, "b"));
    Throws<ArgumentException>(() => container.Provide(new Clock(), "a"));
});

Check("cycles between provided instances resolve before complete callbacks", () =>
{
    var container = new Container233(); var a = new A(); var b = new B();
    container.Provide(a); container.Provide(b); container.StartUp();
    Assert(ReferenceEquals(a.Other, b) && ReferenceEquals(b.Other, a));
});

Check("readonly and static injection targets fail at startup", () =>
{
    var container = new Container233(); container.Provide(new Clock()); container.Provide(new ReadonlyTarget());
    Throws<InvalidOperationException>(() => container.StartUp());
    var other = new Container233(); other.Provide(new StaticTarget());
    Throws<InvalidOperationException>(() => other.StartUp());
});
Console.WriteLine($"Ioc233: {passed} checks passed.");

public interface IClock { }
public sealed class Clock : IClock { }
public class BaseService
{
    [Autowired] private IClock? _clock = null;
    public IClock? BaseClock => _clock;
}
public sealed class Service : BaseService, IProvideAfter, IInjectBefore, IInjectAfter, IObject
{
    [Autowired(Name = "clock")] public IClock? Clock { get; private set; }
    [Autowired(Required = false)] public IDisposable? Optional { get; private set; }
    public bool Ready;
    public List<string> Events { get; } = new();
    public void OnProvideAfter() => Events.Add("provide");
    public void OnInjectBefore() => Events.Add("before");
    public void OnInjectAfter() { if (Clock == null) throw new Exception("Injection order"); Events.Add("after"); }
    public void OnInjectComplete() { Ready = true; Events.Add("complete"); }
}
public sealed class Missing { [Autowired] public IDisposable? Required { get; set; } }
public sealed class A { [Autowired] public B? Other { get; set; } }
public sealed class B { [Autowired] public A? Other { get; set; } }
public sealed class ReadonlyTarget { [Autowired] public readonly IClock? Clock = null; }
public sealed class StaticTarget { [Autowired] public static IClock? Clock { get; set; } }
