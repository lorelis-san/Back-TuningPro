using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using TuningPro.Data.Entities;
namespace TuningPro.Data.Repositories;

public class ClienteRepository : IClienteRepository
{
    private readonly TuningProDbContext _db;
    public ClienteRepository(TuningProDbContext db) => _db = db;

    public Task<List<Usuario>> ListarAsync(string rol, string? q)
    {
        var query = _db.Usuarios.AsNoTracking().Where(u => u.Activo && u.Rol == rol);
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(u => u.NombreCompleto.Contains(q) || u.NombreUsuario.Contains(q));
        return query.OrderBy(u => u.NombreCompleto).ToListAsync();
    }

    public Task<List<Vehiculo>> ListarVehiculosAsync(int usuarioId) =>
        _db.Vehiculos.AsNoTracking().Where(v => v.UsuarioId == usuarioId).OrderBy(v => v.Placa).ToListAsync();

    public Task<bool> VehiculoPerteneceAsync(int vehiculoId, int usuarioId) =>
        _db.Vehiculos.AnyAsync(v => v.Id == vehiculoId && v.UsuarioId == usuarioId);
}