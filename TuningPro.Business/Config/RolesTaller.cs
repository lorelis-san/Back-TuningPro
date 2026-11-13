namespace TuningPro.Business.Config;

public static class RolesTaller
{
    public const string Administrador = "Administrador";
    public const string Tecnico = "Tecnico";
    public const string Auxiliar = "Auxiliar";
    public const string Recepcionista = "Recepcionista";
    public const string Cliente = "Cliente";

    // Combinaciones para [Authorize(Roles = ...)]
    public const string Personal = "Administrador,Tecnico,Auxiliar,Recepcionista";
    public const string SoloAdmin = "Administrador";
    public const string Inventario = "Administrador,Tecnico,Auxiliar";
    public const string AdminTecnico = "Administrador,Tecnico";
    public const string AdminAuxiliar = "Administrador,Auxiliar";
    public const string AdminRecepcion = "Administrador,Recepcionista";
    public const string GestionProyectos = "Administrador,Tecnico,Recepcionista";
}