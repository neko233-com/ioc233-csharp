# ioc233-csharp

面向 C# 游戏框架的轻量 IoC 容器，目标为 .NET Standard 2.1，无外部包依赖。提供显式实例注册、类型 / 名称注入、可选依赖以及启动生命周期。

本版参考 [ioc233-go](https://github.com/neko233-com/ioc233-go) 的 `Provide`、`StartUp` 和回调命名。多个实例匹配同一接口时抛出异常，使用名称消除歧义。

## 使用

将 `Ioc233.csproj` 加入工程并添加 ProjectReference，或执行 `dotnet pack` 生成本地 NuGet 包。尚未发布到 NuGet.org。

```csharp
using Ioc233;

var container = new Container233();
container.Provide(new GameClock(), "clock");
container.Provide(new BattleService());
container.StartUp();
var battle = container.Get<BattleService>();

public interface IGameClock { }
public sealed class GameClock : IGameClock { }

public sealed class BattleService : IObject
{
    [Autowired(Name = "clock")]
    public IGameClock Clock { get; private set; } = null!;

    [Autowired(Required = false)]
    public System.IDisposable Metrics { get; private set; } = null!;

    public void OnInjectComplete() { /* 全部服务注入完成后初始化业务。 */ }
}
```

`[Autowired]` 支持字段、属性和基类的私有成员。成员必须是可写的实例引用类型；静态、readonly、索引器和值类型不允许注入。可选依赖不存在时保留原值，名称存在但类型错误仍然失败。

## 生命周期

1. `Provide` 注册对象后调用该对象的 `IProvideAfter.OnProvideAfter()`。
2. `StartUp` 验证所有注入目标和依赖；验证失败不会执行字段赋值或启动回调。
3. 调用全部 `IInjectBefore.OnInjectBefore()`，然后执行全部依赖赋值。
4. 调用全部 `IInjectAfter.OnInjectAfter()`。
5. 调用全部 `IObject.OnInjectComplete()`。

成功启动后，重复 `StartUp` 直接返回，注册关闭。回调或属性 setter 失败会将容器标记为失败；已发生的业务副作用不会回滚，此时应创建新容器。显式提供的对象可形成循环引用，因为对象创建不依赖注入过程。

容器注册和启动由宿主在同一线程完成。启动后可并发查询，业务对象本身的线程安全由宿主负责。容器不扫描程序集、不创建全局单例、不自动调用 Dispose；对象创建和释放由调用方管理。

## 验证

需要 .NET 10 SDK：

```sh
dotnet build Ioc233.csproj -c Release
dotnet run --project Tests/Ioc233.Tests.csproj -c Release
dotnet pack Ioc233.csproj -c Release --no-build -o artifacts
```

测试覆盖注入匹配、回调顺序、缺失 / 歧义 / 重复注册、循环依赖和非法目标。Unity Editor / IL2CPP 尚未验证；裁剪环境需保留带注入属性的成员。

MIT License.
