using System;
using System.Collections.Generic;
using Ioc233;
using Xunit;

public sealed class InheritanceTests
{
    public class Parent
    {
        [Autowired] public virtual IClock? Clock { get; set; }
    }
    public sealed class Child : Parent
    {
        public int SetCount;
        [Autowired] public override IClock? Clock { get => base.Clock; set { SetCount++; base.Clock = value; } }
    }
    [Fact]
    public void OverriddenPropertyIsInjectedExactlyOnce()
    {
        var container = new Container233(); var child = new Child();
        container.Provide(new Clock()); container.Provide(child); container.StartUp();
        Assert.Equal(1, child.SetCount); Assert.NotNull(child.Clock);
    }
    [Fact]
    public void NonGenericLookupPreservesTypeChecking()
    {
        var container = new Container233(); var clock = new Clock(); container.Provide(clock, "clock");
        Assert.Same(clock, container.Get(typeof(IClock), "clock"));
        Assert.Throws<InvalidOperationException>(() => container.Get(typeof(IDisposable), "clock"));
        Assert.Throws<ArgumentNullException>(() => container.Get(null!));
    }
    public sealed class BadLifecycle : IObject
    {
        public void OnInjectComplete() => throw new InvalidOperationException("startup failed");
    }
    [Fact]
    public void FailedStartupClosesRegistrationAndLookup()
    {
        var container = new Container233(); container.Provide(new BadLifecycle());
        Assert.Throws<InvalidOperationException>(() => container.StartUp());
        Assert.Throws<InvalidOperationException>(() => container.Get<BadLifecycle>());
        Assert.Throws<InvalidOperationException>(() => container.Provide(new Clock()));
    }
    public sealed class ReadOnlyProperty { [Autowired] public IClock Clock => new Clock(); }
    [Fact]
    public void ReadOnlyPropertiesFailBeforeLifecycle()
    {
        var container = new Container233(); container.Provide(new Clock()); container.Provide(new ReadOnlyProperty());
        Assert.Throws<InvalidOperationException>(() => container.StartUp());
    }
}
