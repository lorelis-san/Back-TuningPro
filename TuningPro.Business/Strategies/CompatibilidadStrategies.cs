using TuningPro.Data.Entities;
using TuningPro.Data.Repositories;

namespace TuningPro.Business.Strategies;

public record ResultadoValidacion(bool EsCompatible, string Mensaje);

public interface ICompatibilidadStrategy
{
    string Categoria { get; }
    Task<ResultadoValidacion> ValidarAsync(Vehiculo vehiculo, Pieza pieza);
}

public abstract class CompatibilidadStrategyBase : ICompatibilidadStrategy
{
    private readonly ICompatibilidadRepository _repo;
    protected CompatibilidadStrategyBase(ICompatibilidadRepository repo) => _repo = repo;

    public abstract string Categoria { get; }

    public virtual async Task<ResultadoValidacion> ValidarAsync(Vehiculo v, Pieza p)
    {
        var ok = await _repo.ExisteAsync(v.Id, p.Id);
        return ok
            ? new ResultadoValidacion(true, "Pieza compatible con el vehículo.")
            : new ResultadoValidacion(false,
                $"'{p.Nombre}' no está registrada como compatible con {v.Marca} {v.Modelo} ({v.Placa}).");
    }
}

public class FrenosStrategy : CompatibilidadStrategyBase
{
    public FrenosStrategy(ICompatibilidadRepository repo) : base(repo) { }
    public override string Categoria => "Frenos";
}

public class SuspensionStrategy : CompatibilidadStrategyBase
{
    public SuspensionStrategy(ICompatibilidadRepository repo) : base(repo) { }
    public override string Categoria => "Suspensión";
}

// Regla extra: los turbos solo para vehículos 1995 o posteriores
public class TurboStrategy : CompatibilidadStrategyBase
{
    public TurboStrategy(ICompatibilidadRepository repo) : base(repo) { }
    public override string Categoria => "Turbo";

    public override async Task<ResultadoValidacion> ValidarAsync(Vehiculo v, Pieza p)
    {
        if (v.Anio < 1995)
            return new ResultadoValidacion(false,
                $"El turbo requiere un vehículo 1995 o posterior (año actual: {v.Anio}).");
        return await base.ValidarAsync(v, p);
    }
}

public class GeneralStrategy : CompatibilidadStrategyBase
{
    public GeneralStrategy(ICompatibilidadRepository repo) : base(repo) { }
    public override string Categoria => "General";
}