using System;
using Ioc233;

public class Consumer
{
    public static int Main()
    {
        Container233 container = new Container233();
        Clock clock = new Clock();
        Service service = new Service();
        container.Provide(clock, "clock");
        container.Provide(service, "service");
        container.StartUp();
        if (!Object.ReferenceEquals(container.Get(typeof(Service), "service"), service)) return 1;
        if (!Object.ReferenceEquals(service.Clock, clock) || !service.Ready) return 2;
        Console.WriteLine("PASS Ioc233 package consumer");
        return 0;
    }
    public class Clock { }
    public class Service : IObject
    {
        [Autowired(Name = "clock")] public Clock Clock;
        public bool Ready;
        public void OnInjectComplete() { Ready = Clock != null; }
    }
}
