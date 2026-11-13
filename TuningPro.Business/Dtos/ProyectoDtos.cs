using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TuningPro.Business.Dtos;

public record ProyectoCrearDto(
    int ClienteId,
    int VehiculoId,
    List<int> ProblemaIds,
    string? OtrosProblema,
    string? NotasRecepcion);

public record ProyectoNuevoInfoDto(int Numero, string Codigo, DateTime FechaIngreso, string Recepcionista);

public record ProyectoDto(
    int Id, string Codigo, DateTime FechaIngreso,
    int? ClienteId, string Cliente,
    int VehiculoId, string Vehiculo, string Placa,
    string Recepcionista, List<string> Problemas,
    string? OtrosProblema, string? NotasRecepcion,
    string Estado,
    decimal Total,
    int? OrdenId, string? OrdenCodigo, string? OrdenEstado);

public record ProblemaDto(int Id, string Nombre);
public record ClienteItemDto(int Id, string Nombre);
public record VehiculoItemDto(int Id, string Placa, string Marca, string Modelo, int Anio);