using SQLite;



namespace BarberApp.Models;



public class Barbero

{

    [PrimaryKey, AutoIncrement]

    public int Id { get; set; }



    public string Nombre { get; set; } = string.Empty;

    public string Usuario { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string Rol { get; set; } = string.Empty;

    public bool Activo { get; set; }

    public decimal ComisionPorcentaje { get; set; } = 50;

    public int SucursalId { get; set; } = 1;

}



public class Cliente

{

    [PrimaryKey, AutoIncrement]

    public int Id { get; set; }



    public string Nombre { get; set; } = string.Empty;

    public string Telefono { get; set; } = string.Empty;

    public string Notas { get; set; } = string.Empty;

    public int SucursalId { get; set; } = 1;

}



public class Servicio

{

    [PrimaryKey, AutoIncrement]

    public int Id { get; set; }



    public string Nombre { get; set; } = string.Empty;

    public decimal Precio { get; set; }

    public int DuracionMinutos { get; set; } = 30;

    public bool Activo { get; set; } = true;

    public int SucursalId { get; set; } = 1;

}



public class Cita

{

    [PrimaryKey, AutoIncrement]

    public int Id { get; set; }



    public int ClienteId { get; set; }

    public int BarberoId { get; set; }

    public int ServicioId { get; set; }

    public DateTime Fecha { get; set; }

    public string Hora { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;

    public string NombreCliente { get; set; } = string.Empty;

    public string NombreBarbero { get; set; } = string.Empty;

    public string NombreServicio { get; set; } = string.Empty;

    public decimal Monto { get; set; }

    public int DuracionMinutos { get; set; } = 30;

    public string RecurrenciaTipo { get; set; } = RecurrenciaTipos.Ninguna;

    public string RecurrenciaGrupoId { get; set; } = string.Empty;

    public int SucursalId { get; set; } = 1;

    public bool NotificacionProgramada { get; set; }

}



public class Cobro

{

    [PrimaryKey, AutoIncrement]

    public int Id { get; set; }



    public int CitaId { get; set; }

    public int BarberoId { get; set; }

    public decimal Monto { get; set; }

    public decimal ComisionMonto { get; set; }

    public string Tipo { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; }

    public string NombreBarbero { get; set; } = string.Empty;

    public string NombreServicio { get; set; } = string.Empty;

    public int SucursalId { get; set; } = 1;

}



public class Sucursal

{

    [PrimaryKey, AutoIncrement]

    public int Id { get; set; }



    public string Nombre { get; set; } = string.Empty;

    public string Direccion { get; set; } = string.Empty;

    public bool Activa { get; set; } = true;

}



public class BloqueoHorario

{

    [PrimaryKey, AutoIncrement]

    public int Id { get; set; }



    public int BarberoId { get; set; }

    public int DiaSemana { get; set; } = -1;

    public DateTime? FechaEspecifica { get; set; }

    public string HoraInicio { get; set; } = string.Empty;

    public string HoraFin { get; set; } = string.Empty;

    public string Motivo { get; set; } = string.Empty;

    public int SucursalId { get; set; } = 1;

}



public class Producto

{

    [PrimaryKey, AutoIncrement]

    public int Id { get; set; }



    public string Nombre { get; set; } = string.Empty;

    public decimal Precio { get; set; }

    public int Stock { get; set; }

    public bool Activo { get; set; } = true;

    public int SucursalId { get; set; } = 1;

}



public class VentaProducto

{

    [PrimaryKey, AutoIncrement]

    public int Id { get; set; }



    public int ProductoId { get; set; }

    public string NombreProducto { get; set; } = string.Empty;

    public int Cantidad { get; set; }

    public decimal Monto { get; set; }

    public string TipoPago { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; }

    public string NombreVendedor { get; set; } = string.Empty;

    public int SucursalId { get; set; } = 1;

}



public class AuditoriaLog

{

    [PrimaryKey, AutoIncrement]

    public int Id { get; set; }



    public DateTime Timestamp { get; set; }

    public int UsuarioId { get; set; }

    public string UsuarioNombre { get; set; } = string.Empty;

    public string Accion { get; set; } = string.Empty;

    public string Detalle { get; set; } = string.Empty;

    public int SucursalId { get; set; } = 1;

}



public class SyncQueueItem

{

    [PrimaryKey, AutoIncrement]

    public int Id { get; set; }



    public string Entidad { get; set; } = string.Empty;

    public int EntidadId { get; set; }

    public string Operacion { get; set; } = string.Empty;

    public string PayloadJson { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; }

    public bool Sincronizado { get; set; }

    public int SucursalId { get; set; } = 1;

}



public class ClienteHistorial

{

    public int TotalCitas { get; set; }

    public decimal TotalGastado { get; set; }

    public List<Cita> CitasAnteriores { get; set; } = [];

    public List<(string Servicio, int Veces)> ServiciosFavoritos { get; set; } = [];

}



public class ReporteComisionItem

{

    public string Barbero { get; set; } = string.Empty;

    public decimal TotalCobrado { get; set; }

    public decimal Comision { get; set; }

    public int Cantidad { get; set; }

}



public class BackupResumen

{

    public int Clientes { get; set; }

    public int Barberos { get; set; }

    public int Servicios { get; set; }

    public int Citas { get; set; }

    public int Cobros { get; set; }

    public int Productos { get; set; }

    public DateTime ExportadoEn { get; set; }

}


