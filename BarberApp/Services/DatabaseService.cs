using BarberApp.Core;
using BarberApp.Models;
using SQLite;
using System.Text.Json;

namespace BarberApp.Services;

public class DatabaseService
{
    public static string DatabasePath { get; } =
        Path.Combine(FileSystem.AppDataDirectory, "barberapp.db3");

    private SQLiteAsyncConnection? _db;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    public int SucursalActivaId =>
        Preferences.Get(PreferenceKeys.SucursalActivaId, 1);

    public async Task InitAsync()
    {
        if (_initialized) return;

        await _initLock.WaitAsync();
        try
        {
            if (_initialized) return;

            _db = new SQLiteAsyncConnection(
                DatabasePath,
                SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);

            await _db.CreateTableAsync<Cliente>();
            await _db.CreateTableAsync<Barbero>();
            await _db.CreateTableAsync<Servicio>();
            await _db.CreateTableAsync<Cita>();
            await _db.CreateTableAsync<Cobro>();
            await _db.CreateTableAsync<Sucursal>();
            await _db.CreateTableAsync<BloqueoHorario>();
            await _db.CreateTableAsync<Producto>();
            await _db.CreateTableAsync<VentaProducto>();
            await _db.CreateTableAsync<AuditoriaLog>();
            await _db.CreateTableAsync<SyncQueueItem>();

            await TryAddColumnAsync("Servicio", "DuracionMinutos", "INTEGER NOT NULL DEFAULT 30");
            await TryAddColumnAsync("Cita", "DuracionMinutos", "INTEGER NOT NULL DEFAULT 30");
            await TryAddColumnAsync("Cita", "RecurrenciaTipo", "TEXT NOT NULL DEFAULT 'ninguna'");
            await TryAddColumnAsync("Cita", "RecurrenciaGrupoId", "TEXT NOT NULL DEFAULT ''");
            await TryAddColumnAsync("Cita", "SucursalId", "INTEGER NOT NULL DEFAULT 1");
            await TryAddColumnAsync("Cita", "NotificacionProgramada", "INTEGER NOT NULL DEFAULT 0");
            await TryAddColumnAsync("Barbero", "ComisionPorcentaje", "REAL NOT NULL DEFAULT 50");
            await TryAddColumnAsync("Barbero", "SucursalId", "INTEGER NOT NULL DEFAULT 1");
            await TryAddColumnAsync("Cliente", "SucursalId", "INTEGER NOT NULL DEFAULT 1");
            await TryAddColumnAsync("Servicio", "SucursalId", "INTEGER NOT NULL DEFAULT 1");
            await TryAddColumnAsync("Cobro", "BarberoId", "INTEGER NOT NULL DEFAULT 0");
            await TryAddColumnAsync("Cobro", "ComisionMonto", "REAL NOT NULL DEFAULT 0");
            await TryAddColumnAsync("Cobro", "SucursalId", "INTEGER NOT NULL DEFAULT 1");

            if (await Db.Table<Sucursal>().CountAsync() == 0)
            {
                await Db.InsertAsync(new Sucursal { Nombre = "Principal", Direccion = "", Activa = true });
                Preferences.Set(PreferenceKeys.SucursalActivaId, 1);
            }

            if (string.IsNullOrEmpty(Preferences.Get(PreferenceKeys.HoraApertura, "")))
                Preferences.Set(PreferenceKeys.HoraApertura, "09:00");
            if (string.IsNullOrEmpty(Preferences.Get(PreferenceKeys.HoraCierre, "")))
                Preferences.Set(PreferenceKeys.HoraCierre, "20:00");
            if (!Preferences.ContainsKey(PreferenceKeys.TimeoutMinutos))
                Preferences.Set(PreferenceKeys.TimeoutMinutos, 30);

            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task TryAddColumnAsync(string tabla, string columna, string definicion)
    {
        try { await Db.ExecuteAsync($"ALTER TABLE {tabla} ADD COLUMN {columna} {definicion}"); }
        catch { /* ya existe */ }
    }

    // ── AUDITORÍA ─────────────────────────────────────────────
    public async Task RegistrarAuditoriaAsync(string accion, string detalle)
    {
        await Db.InsertAsync(new AuditoriaLog
        {
            Timestamp = DateTime.Now,
            UsuarioId = Preferences.Get(PreferenceKeys.UsuarioLogueadoId, 0),
            UsuarioNombre = Preferences.Get(PreferenceKeys.UsuarioLogueadoNombre, "Sistema"),
            Accion = accion,
            Detalle = detalle,
            SucursalId = SucursalActivaId
        });
    }

    public Task<List<AuditoriaLog>> GetAuditoriaAsync(int limite = 100) =>
        Db.Table<AuditoriaLog>()
            .Where(a => a.SucursalId == SucursalActivaId)
            .OrderByDescending(a => a.Timestamp)
            .Take(limite)
            .ToListAsync();

    // ── SUCURSALES ────────────────────────────────────────────
    public Task<List<Sucursal>> GetSucursalesAsync() =>
        Db.Table<Sucursal>().Where(s => s.Activa).OrderBy(s => s.Nombre).ToListAsync();

    public Task<int> SaveSucursalAsync(Sucursal s) =>
        s.Id == 0 ? Db.InsertAsync(s) : Db.UpdateAsync(s);

    public void CambiarSucursalActiva(int id) =>
        Preferences.Set(PreferenceKeys.SucursalActivaId, id);

    // ── NEGOCIO (Preferences) ─────────────────────────────────
    public void GuardarDatosNegocio(string nombre, string dueno, string telefono, string apertura, string cierre)
    {
        Preferences.Set(PreferenceKeys.NombreLocal, nombre);
        Preferences.Set(PreferenceKeys.Dueno, dueno);
        Preferences.Set(PreferenceKeys.TelefonoNegocio, telefono);
        Preferences.Set(PreferenceKeys.HoraApertura, apertura);
        Preferences.Set(PreferenceKeys.HoraCierre, cierre);
    }

    public (string Nombre, string Dueno, string Telefono, string Apertura, string Cierre) ObtenerDatosNegocio() => (
        Preferences.Get(PreferenceKeys.NombreLocal, "Mi negocio"),
        Preferences.Get(PreferenceKeys.Dueno, ""),
        Preferences.Get(PreferenceKeys.TelefonoNegocio, ""),
        Preferences.Get(PreferenceKeys.HoraApertura, "09:00"),
        Preferences.Get(PreferenceKeys.HoraCierre, "20:00"));

    // ── BARBEROS ──────────────────────────────────────────────
    public Task<int> GetBarberosCountAsync() =>
        Db.Table<Barbero>().CountAsync();

    public Task<List<Barbero>> GetBarberosAsync() =>
        Db.Table<Barbero>().Where(b => b.Activo && b.SucursalId == SucursalActivaId).ToListAsync();

    public Task<List<Barbero>> GetAllBarberosAsync() =>
        Db.Table<Barbero>().Where(b => b.SucursalId == SucursalActivaId).OrderBy(b => b.Nombre).ToListAsync();

    public async Task<Barbero?> GetBarberoByIdAsync(int id) =>
        await Db.Table<Barbero>().Where(b => b.Id == id).FirstOrDefaultAsync();

    public async Task<bool> UsuarioExisteAsync(string usuario, int excluirId = 0)
    {
        var count = await Db.Table<Barbero>()
            .Where(b => b.Usuario == usuario && b.Id != excluirId).CountAsync();
        return count > 0;
    }

    public async Task<bool> CambiarPasswordAdminAsync(string passwordActual, string passwordNueva)
    {
        var adminId = Preferences.Get(PreferenceKeys.UsuarioLogueadoId, 0);
        var admin = await GetBarberoByIdAsync(adminId);
        if (admin == null || admin.Rol != Roles.Admin) return false;
        if (!PasswordHelper.Verify(passwordActual, admin.PasswordHash)) return false;
        admin.PasswordHash = PasswordHelper.Hash(passwordNueva);
        await Db.UpdateAsync(admin);
        await RegistrarAuditoriaAsync("Cambio contraseña", $"Admin {admin.Usuario}");
        return true;
    }

    public Task<int> SaveBarberoAsync(Barbero b)
    {
        b.SucursalId = SucursalActivaId;
        return b.Id == 0 ? Db.InsertAsync(b) : Db.UpdateAsync(b);
    }

    public async Task<Barbero?> LoginAsync(string usuario, string password)
    {
        var barbero = await Db.Table<Barbero>()
            .Where(b => b.Usuario == usuario && b.Activo).FirstOrDefaultAsync();
        if (barbero == null) return null;

        if (PasswordHelper.Verify(password, barbero.PasswordHash))
        {
            if (!PasswordHelper.EsHash(barbero.PasswordHash))
            {
                barbero.PasswordHash = PasswordHelper.Hash(password);
                await Db.UpdateAsync(barbero);
            }
            return barbero;
        }
        return null;
    }

    public async Task DesactivarBarberoAsync(Barbero barbero)
    {
        barbero.Activo = false;
        await Db.UpdateAsync(barbero);
        await RegistrarAuditoriaAsync("Desactivar barbero", barbero.Nombre);
    }

    // ── CLIENTES ──────────────────────────────────────────────
    public Task<List<Cliente>> GetClientesAsync() =>
        Db.Table<Cliente>().Where(c => c.SucursalId == SucursalActivaId).OrderBy(c => c.Nombre).ToListAsync();

    public async Task<List<Cliente>> BuscarClientesAsync(string texto)
    {
        var todos = await GetClientesAsync();
        if (string.IsNullOrWhiteSpace(texto)) return todos;
        var t = texto.Trim().ToLowerInvariant();
        return todos.Where(c => c.Nombre.ToLowerInvariant().Contains(t) ||
                                c.Telefono.Contains(t, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public async Task<Cliente?> GetClienteByIdAsync(int id) =>
        await Db.Table<Cliente>().Where(c => c.Id == id).FirstOrDefaultAsync();

    public Task<int> SaveClienteAsync(Cliente c)
    {
        c.SucursalId = SucursalActivaId;
        return c.Id == 0 ? Db.InsertAsync(c) : Db.UpdateAsync(c);
    }

    public Task<int> DeleteClienteAsync(Cliente c) => Db.DeleteAsync(c);

    public Task<int> ContarCitasFuturasClienteAsync(int clienteId)
    {
        var hoy = DateTime.Today;
        return Db.Table<Cita>()
            .Where(c => c.ClienteId == clienteId && c.Fecha >= hoy &&
                        c.Estado == CitaEstados.Pendiente && c.SucursalId == SucursalActivaId)
            .CountAsync();
    }

    public async Task<ClienteHistorial> GetHistorialClienteAsync(int clienteId)
    {
        var citas = await Db.Table<Cita>()
            .Where(c => c.ClienteId == clienteId && c.SucursalId == SucursalActivaId)
            .OrderByDescending(c => c.Fecha).ToListAsync();

        var cobradas = citas.Where(c => c.Estado == CitaEstados.Cobrada).ToList();
        var favoritos = cobradas.GroupBy(c => c.NombreServicio)
            .Select(g => (g.Key, g.Count()))
            .OrderByDescending(x => x.Item2).Take(5).ToList();

        return new ClienteHistorial
        {
            TotalCitas = citas.Count,
            TotalGastado = cobradas.Sum(c => c.Monto),
            CitasAnteriores = citas.Take(20).ToList(),
            ServiciosFavoritos = favoritos
        };
    }

    // ── SERVICIOS ─────────────────────────────────────────────
    public Task<List<Servicio>> GetServiciosAsync() =>
        Db.Table<Servicio>().Where(s => s.Activo && s.SucursalId == SucursalActivaId).OrderBy(s => s.Nombre).ToListAsync();

    public Task<List<Servicio>> GetAllServiciosAsync() =>
        Db.Table<Servicio>().Where(s => s.SucursalId == SucursalActivaId).OrderBy(s => s.Nombre).ToListAsync();

    public async Task<Servicio?> GetServicioByIdAsync(int id) =>
        await Db.Table<Servicio>().Where(s => s.Id == id).FirstOrDefaultAsync();

    public Task<int> SaveServicioAsync(Servicio s)
    {
        s.SucursalId = SucursalActivaId;
        return s.Id == 0 ? Db.InsertAsync(s) : Db.UpdateAsync(s);
    }

    public async Task DeleteServicioAsync(Servicio s)
    {
        var pendientes = await Db.Table<Cita>()
            .Where(c => c.ServicioId == s.Id && c.Estado == CitaEstados.Pendiente).CountAsync();
        if (pendientes > 0)
            throw new InvalidOperationException($"Hay {pendientes} cita(s) pendiente(s) con este servicio.");
        s.Activo = false;
        await Db.UpdateAsync(s);
    }

    // ── PRODUCTOS ─────────────────────────────────────────────
    public Task<List<Producto>> GetProductosAsync() =>
        Db.Table<Producto>().Where(p => p.Activo && p.SucursalId == SucursalActivaId).OrderBy(p => p.Nombre).ToListAsync();

    public Task<List<Producto>> GetAllProductosAsync() =>
        Db.Table<Producto>().Where(p => p.SucursalId == SucursalActivaId).OrderBy(p => p.Nombre).ToListAsync();

    public Task<int> SaveProductoAsync(Producto p)
    {
        p.SucursalId = SucursalActivaId;
        return p.Id == 0 ? Db.InsertAsync(p) : Db.UpdateAsync(p);
    }

    public async Task RegistrarVentaProductoAsync(Producto producto, int cantidad, string tipoPago, string vendedor)
    {
        if (producto.Stock < cantidad)
            throw new InvalidOperationException("Stock insuficiente.");
        producto.Stock -= cantidad;
        await Db.UpdateAsync(producto);
        var monto = producto.Precio * cantidad;
        await Db.InsertAsync(new VentaProducto
        {
            ProductoId = producto.Id,
            NombreProducto = producto.Nombre,
            Cantidad = cantidad,
            Monto = monto,
            TipoPago = tipoPago,
            Timestamp = DateTime.Now,
            NombreVendedor = vendedor,
            SucursalId = SucursalActivaId
        });
        await RegistrarAuditoriaAsync("Venta producto", $"{producto.Nombre} x{cantidad} ${monto:N0}");
    }

    // ── BLOQUEOS ──────────────────────────────────────────────
    public Task<List<BloqueoHorario>> GetBloqueosAsync() =>
        Db.Table<BloqueoHorario>().Where(b => b.SucursalId == SucursalActivaId).ToListAsync();

    public Task<int> SaveBloqueoAsync(BloqueoHorario b)
    {
        b.SucursalId = SucursalActivaId;
        return b.Id == 0 ? Db.InsertAsync(b) : Db.UpdateAsync(b);
    }

    public Task<int> DeleteBloqueoAsync(BloqueoHorario b) => Db.DeleteAsync(b);

    public async Task<bool> EstaBloqueadoAsync(int barberoId, DateTime fecha, string hora, int duracionMinutos)
    {
        if (!CitaHorarioHelper.ParseHora(hora, out var inicio)) return false;
        var fin = inicio.Add(TimeSpan.FromMinutes(duracionMinutos));
        var bloqueos = await GetBloqueosAsync();
        var diaSemana = (int)fecha.DayOfWeek;

        foreach (var b in bloqueos)
        {
            if (b.BarberoId != 0 && b.BarberoId != barberoId) continue;
            if (b.FechaEspecifica.HasValue && b.FechaEspecifica.Value.Date != fecha.Date) continue;
            if (!b.FechaEspecifica.HasValue && b.DiaSemana >= 0 && b.DiaSemana != diaSemana) continue;
            if (!CitaHorarioHelper.ParseHora(b.HoraInicio, out var bi)) continue;
            if (!CitaHorarioHelper.ParseHora(b.HoraFin, out var bf)) continue;
            if (inicio < bf && bi < fin) return true;
        }
        return false;
    }

    // ── CITAS ─────────────────────────────────────────────────
    public Task<List<Cita>> GetAllCitasAsync() =>
        Db.Table<Cita>().Where(c => c.SucursalId == SucursalActivaId).OrderByDescending(c => c.Fecha).ToListAsync();

    public Task<List<Cita>> GetCitasPorFechaAsync(DateTime fecha)
    {
        var inicio = fecha.Date;
        var fin = inicio.AddDays(1);
        return Db.Table<Cita>()
            .Where(c => c.Fecha >= inicio && c.Fecha < fin &&
                        c.Estado != CitaEstados.Cancelada && c.SucursalId == SucursalActivaId)
            .OrderBy(c => c.Hora).ToListAsync();
    }

    public Task<List<Cita>> GetCitasPorRangoAsync(DateTime desde, DateTime hasta)
    {
        var fin = hasta.Date.AddDays(1);
        return Db.Table<Cita>()
            .Where(c => c.Fecha >= desde.Date && c.Fecha < fin &&
                        c.Estado != CitaEstados.Cancelada && c.SucursalId == SucursalActivaId)
            .OrderBy(c => c.Fecha).ThenBy(c => c.Hora).ToListAsync();
    }

    public Task<List<Cita>> GetCitasPorFechaYBarberoAsync(DateTime fecha, int barberoId)
    {
        var inicio = fecha.Date;
        var fin = inicio.AddDays(1);
        return Db.Table<Cita>()
            .Where(c => c.Fecha >= inicio && c.Fecha < fin &&
                        c.BarberoId == barberoId &&
                        c.Estado != CitaEstados.Cancelada && c.SucursalId == SucursalActivaId)
            .OrderBy(c => c.Hora).ToListAsync();
    }

    public Task<List<Cita>> GetCitasPendientesCobroAsync(DateTime fecha)
    {
        var inicio = fecha.Date;
        var fin = inicio.AddDays(1);
        return Db.Table<Cita>()
            .Where(c => c.Fecha >= inicio && c.Fecha < fin &&
                        c.Estado == CitaEstados.Pendiente && c.SucursalId == SucursalActivaId)
            .OrderBy(c => c.Hora).ToListAsync();
    }

    public Task<List<Cita>> GetCitasPendientesHoyAsync(int? barberoId = null)
    {
        var inicio = DateTime.Today;
        var fin = inicio.AddDays(1);
        if (barberoId.HasValue)
            return Db.Table<Cita>()
                .Where(c => c.Fecha >= inicio && c.Fecha < fin &&
                            c.BarberoId == barberoId.Value &&
                            c.Estado == CitaEstados.Pendiente && c.SucursalId == SucursalActivaId)
                .OrderBy(c => c.Hora).ToListAsync();

        return Db.Table<Cita>()
            .Where(c => c.Fecha >= inicio && c.Fecha < fin &&
                        c.Estado == CitaEstados.Pendiente && c.SucursalId == SucursalActivaId)
            .OrderBy(c => c.Hora).ToListAsync();
    }

    public Task<List<Cita>> GetCitasPendientesConTelefonoAsync(DateTime fecha)
    {
        var inicio = fecha.Date;
        var fin = inicio.AddDays(1);
        return Db.Table<Cita>()
            .Where(c => c.Fecha >= inicio && c.Fecha < fin &&
                        c.Estado == CitaEstados.Pendiente && c.SucursalId == SucursalActivaId)
            .OrderBy(c => c.Hora).ToListAsync();
    }

    public async Task<Cita?> GetProximaCitaAsync(int? barberoId = null)
    {
        var ahora = DateTime.Now;
        var hoy = ahora.Date;
        var manana = hoy.AddDays(1);
        List<Cita> citas = barberoId.HasValue
            ? await Db.Table<Cita>().Where(c => c.Fecha >= hoy && c.Fecha < manana &&
                c.BarberoId == barberoId.Value && c.Estado == CitaEstados.Pendiente &&
                c.SucursalId == SucursalActivaId).OrderBy(c => c.Hora).ToListAsync()
            : await Db.Table<Cita>().Where(c => c.Fecha >= hoy && c.Fecha < manana &&
                c.Estado == CitaEstados.Pendiente && c.SucursalId == SucursalActivaId)
                .OrderBy(c => c.Hora).ToListAsync();

        foreach (var cita in citas)
        {
            if (CitaHorarioHelper.ParseHora(cita.Hora, out var hora))
            {
                if (cita.Fecha.Date.Add(hora) >= ahora) return cita;
            }
        }
        return citas.FirstOrDefault();
    }

    public async Task<List<Cita>> GetCitasProximaHoraAsync(int? barberoId = null)
    {
        var inicio = DateTime.Now;
        var fin = inicio.AddHours(1);
        var citas = await GetCitasPendientesHoyAsync(barberoId);
        return citas.Where(c =>
        {
            if (!CitaHorarioHelper.ParseHora(c.Hora, out var hora)) return false;
            var dt = c.Fecha.Date.Add(hora);
            return dt >= inicio && dt <= fin;
        }).ToList();
    }

    public async Task<List<Cita>> GetCitasParaNotificarAsync()
    {
        var ahora = DateTime.Now;
        var ventanaFin = ahora.AddHours(1).AddMinutes(5);
        var citas = await GetCitasPendientesHoyAsync();
        return citas.Where(c =>
        {
            if (c.NotificacionProgramada) return false;
            if (!CitaHorarioHelper.ParseHora(c.Hora, out var hora)) return false;
            var dt = c.Fecha.Date.Add(hora);
            return dt >= ahora && dt <= ventanaFin;
        }).ToList();
    }

    public async Task<Cita?> GetCitaByIdAsync(int id) =>
        await Db.Table<Cita>().Where(c => c.Id == id).FirstOrDefaultAsync();

    public async Task<bool> HayConflictoCitaAsync(
        int barberoId, DateTime fecha, string hora, int duracionMinutos, int excluirCitaId = 0)
    {
        if (await EstaBloqueadoAsync(barberoId, fecha, hora, duracionMinutos)) return true;
        if (!CitaHorarioHelper.ParseHora(hora, out var inicioNuevo)) return false;

        var inicioDia = fecha.Date;
        var finDia = inicioDia.AddDays(1);

        var citas = await Db.Table<Cita>()
            .Where(c => c.BarberoId == barberoId &&
                        c.Fecha >= inicioDia && c.Fecha < finDia &&
                        c.Estado == CitaEstados.Pendiente && c.Id != excluirCitaId &&
                        c.SucursalId == SucursalActivaId).ToListAsync();

        foreach (var c in citas)
        {
            if (!CitaHorarioHelper.ParseHora(c.Hora, out var inicioExistente)) continue;
            var duracion = c.DuracionMinutos > 0 ? c.DuracionMinutos : 30;
            if (CitaHorarioHelper.HaySolapamiento(inicioNuevo, duracionMinutos, inicioExistente, duracion))
                return true;
        }
        return false;
    }

    public async Task<int> SaveCitaAsync(Cita c, bool generarRecurrencia = true)
    {
        c.SucursalId = SucursalActivaId;
        var id = c.Id == 0 ? await Db.InsertAsync(c) : await Db.UpdateAsync(c);
        if (c.Id == 0) c.Id = id;

        if (generarRecurrencia && c.RecurrenciaTipo != RecurrenciaTipos.Ninguna && !string.IsNullOrEmpty(c.RecurrenciaGrupoId))
            await GenerarCitasRecurrentesAsync(c);

        await EncolarSyncAsync("Cita", c.Id, "upsert", c);
        return c.Id;
    }

    private async Task GenerarCitasRecurrentesAsync(Cita baseCita)
    {
        var existentes = await Db.Table<Cita>()
            .Where(c => c.RecurrenciaGrupoId == baseCita.RecurrenciaGrupoId && c.Id != baseCita.Id).CountAsync();
        if (existentes > 0) return;

        var intervalo = baseCita.RecurrenciaTipo switch
        {
            RecurrenciaTipos.Quincenal => 15,
            RecurrenciaTipos.Mensual => 30,
            _ => 0
        };
        if (intervalo == 0) return;

        for (int i = 1; i <= 5; i++)
        {
            var fecha = baseCita.Fecha.AddDays(intervalo * i);
            if (await HayConflictoCitaAsync(baseCita.BarberoId, fecha, baseCita.Hora, baseCita.DuracionMinutos))
                continue;

            var copia = new Cita
            {
                ClienteId = baseCita.ClienteId,
                BarberoId = baseCita.BarberoId,
                ServicioId = baseCita.ServicioId,
                Fecha = fecha,
                Hora = baseCita.Hora,
                Estado = CitaEstados.Pendiente,
                NombreCliente = baseCita.NombreCliente,
                NombreBarbero = baseCita.NombreBarbero,
                NombreServicio = baseCita.NombreServicio,
                Monto = baseCita.Monto,
                DuracionMinutos = baseCita.DuracionMinutos,
                RecurrenciaTipo = RecurrenciaTipos.Ninguna,
                RecurrenciaGrupoId = baseCita.RecurrenciaGrupoId,
                SucursalId = baseCita.SucursalId
            };
            await Db.InsertAsync(copia);
        }
    }

    public async Task CancelarCitaAsync(Cita c)
    {
        c.Estado = CitaEstados.Cancelada;
        await Db.UpdateAsync(c);
        await RegistrarAuditoriaAsync("Cancelar cita", $"{c.NombreCliente} {c.Fecha:dd/MM} {c.Hora}");
    }

    public Task<int> DeleteCitaAsync(Cita c) => Db.DeleteAsync(c);

    public async Task MarcarNotificacionEnviadaAsync(int citaId)
    {
        var cita = await GetCitaByIdAsync(citaId);
        if (cita == null) return;
        cita.NotificacionProgramada = true;
        await Db.UpdateAsync(cita);
    }

    public async Task<Dictionary<DateTime, decimal>> GetIngresosSemanaAsync()
    {
        var resultado = new Dictionary<DateTime, decimal>();
        for (int i = 6; i >= 0; i--)
        {
            var dia = DateTime.Today.AddDays(-i);
            var cobros = await GetCobrosPorFechaAsync(dia, dia.AddDays(1).AddSeconds(-1));
            resultado[dia] = cobros.Sum(c => c.Monto);
        }
        return resultado;
    }

    public async Task<(decimal EstaSemana, decimal SemanaAnterior)> GetComparativaSemanasAsync()
    {
        var hoy = DateTime.Today;
        var inicioEsta = hoy.AddDays(-6);
        var inicioAnterior = hoy.AddDays(-13);
        var finAnterior = hoy.AddDays(-7).AddDays(1).AddSeconds(-1);
        var esta = (await GetCobrosPorFechaAsync(inicioEsta, hoy.AddDays(1).AddSeconds(-1))).Sum(c => c.Monto);
        var anterior = (await GetCobrosPorFechaAsync(inicioAnterior, finAnterior)).Sum(c => c.Monto);
        return (esta, anterior);
    }

    // ── COBROS ────────────────────────────────────────────────
    public Task<List<Cobro>> GetAllCobrosAsync() =>
        Db.Table<Cobro>().Where(c => c.SucursalId == SucursalActivaId)
            .OrderByDescending(c => c.Timestamp).ToListAsync();

    public Task<List<Cobro>> GetCobrosPorFechaAsync(DateTime desde, DateTime hasta) =>
        Db.Table<Cobro>()
            .Where(c => c.Timestamp >= desde && c.Timestamp <= hasta && c.SucursalId == SucursalActivaId)
            .OrderByDescending(c => c.Timestamp).ToListAsync();

    public async Task RegistrarCobroAsync(Cita cita, string tipoPago)
    {
        var barbero = await GetBarberoByIdAsync(cita.BarberoId);
        var comisionPct = barbero?.ComisionPorcentaje ?? 0;
        var comision = Math.Round(cita.Monto * comisionPct / 100m, 2);

        var cobro = new Cobro
        {
            CitaId = cita.Id,
            BarberoId = cita.BarberoId,
            Monto = cita.Monto,
            ComisionMonto = comision,
            Tipo = tipoPago,
            Timestamp = DateTime.Now,
            NombreBarbero = cita.NombreBarbero,
            NombreServicio = cita.NombreServicio,
            SucursalId = SucursalActivaId
        };

        cita.Estado = CitaEstados.Cobrada;
        await Db.InsertAsync(cobro);
        await Db.UpdateAsync(cita);
        await RegistrarAuditoriaAsync("Cobro", $"{cita.NombreCliente} ${cita.Monto:N0} · {tipoPago}");
        await EncolarSyncAsync("Cobro", cobro.Id, "insert", cobro);
    }

    public async Task RegistrarCobroWalkInAsync(string descripcion, decimal monto, string tipoPago, string nombreBarbero, int barberoId = 0)
    {
        var barbero = barberoId > 0 ? await GetBarberoByIdAsync(barberoId) : null;
        var comision = barbero != null ? Math.Round(monto * barbero.ComisionPorcentaje / 100m, 2) : 0;

        var cobro = new Cobro
        {
            CitaId = 0,
            BarberoId = barberoId,
            Monto = monto,
            ComisionMonto = comision,
            Tipo = tipoPago,
            Timestamp = DateTime.Now,
            NombreBarbero = nombreBarbero,
            NombreServicio = descripcion,
            SucursalId = SucursalActivaId
        };
        await Db.InsertAsync(cobro);
        await RegistrarAuditoriaAsync("Walk-in", $"{descripcion} ${monto:N0}");
    }

    public async Task<Dictionary<string, decimal>> GetDesglosePagosAsync(DateTime desde, DateTime hasta)
    {
        var cobros = await GetCobrosPorFechaAsync(desde, hasta);
        var desglose = MetodosPago.Todos.ToDictionary(m => m, _ => 0m);
        foreach (var c in cobros)
        {
            if (desglose.ContainsKey(c.Tipo)) desglose[c.Tipo] += c.Monto;
            else desglose[MetodosPago.Efectivo] += c.Monto;
        }
        return desglose;
    }

    public async Task<Dictionary<string, decimal>> GetIngresosPorBarberoAsync(DateTime desde, DateTime hasta)
    {
        var cobros = await GetCobrosPorFechaAsync(desde, hasta);
        return cobros.GroupBy(c => c.NombreBarbero)
            .ToDictionary(g => g.Key, g => g.Sum(c => c.Monto));
    }

    public async Task<Dictionary<string, decimal>> GetIngresosPorServicioAsync(DateTime desde, DateTime hasta)
    {
        var cobros = await GetCobrosPorFechaAsync(desde, hasta);
        return cobros.GroupBy(c => c.NombreServicio)
            .ToDictionary(g => g.Key, g => g.Sum(c => c.Monto));
    }

    public Task<int> RestaurarCobroAsync(Cobro cobro) => Db.InsertAsync(cobro);

    public async Task<List<ReporteComisionItem>> GetReporteComisionesAsync(DateTime desde, DateTime hasta)
    {
        var cobros = await GetCobrosPorFechaAsync(desde, hasta);
        return cobros.GroupBy(c => c.NombreBarbero)
            .Select(g => new ReporteComisionItem
            {
                Barbero = g.Key,
                TotalCobrado = g.Sum(c => c.Monto),
                Comision = g.Sum(c => c.ComisionMonto),
                Cantidad = g.Count()
            }).OrderByDescending(r => r.TotalCobrado).ToList();
    }

    // ── SYNC ──────────────────────────────────────────────────
    public async Task EncolarSyncAsync(string entidad, int entidadId, string operacion, object payload)
    {
        await Db.InsertAsync(new SyncQueueItem
        {
            Entidad = entidad,
            EntidadId = entidadId,
            Operacion = operacion,
            PayloadJson = JsonSerializer.Serialize(payload),
            Timestamp = DateTime.Now,
            SucursalId = SucursalActivaId
        });
    }

    public Task<List<SyncQueueItem>> GetPendientesSyncAsync() =>
        Db.Table<SyncQueueItem>().Where(s => !s.Sincronizado).OrderBy(s => s.Timestamp).ToListAsync();

    public async Task MarcarSincronizadoAsync(IEnumerable<int> ids)
    {
        foreach (var id in ids)
        {
            var item = await Db.Table<SyncQueueItem>().Where(s => s.Id == id).FirstOrDefaultAsync();
            if (item != null)
            {
                item.Sincronizado = true;
                await Db.UpdateAsync(item);
            }
        }
    }

    // ── LIMPIEZA TOTAL ────────────────────────────────────────
    public async Task LimpiarDatosSucursalAsync()
    {
        var sid = SucursalActivaId;
        await Db.ExecuteAsync("DELETE FROM Cliente WHERE SucursalId = ?", sid);
        await Db.ExecuteAsync("DELETE FROM Barbero WHERE SucursalId = ? AND Rol != ?", sid, Roles.Admin);
        await Db.ExecuteAsync("DELETE FROM Servicio WHERE SucursalId = ?", sid);
        await Db.ExecuteAsync("DELETE FROM Cita WHERE SucursalId = ?", sid);
        await Db.ExecuteAsync("DELETE FROM Cobro WHERE SucursalId = ?", sid);
        await Db.ExecuteAsync("DELETE FROM Producto WHERE SucursalId = ?", sid);
        await Db.ExecuteAsync("DELETE FROM VentaProducto WHERE SucursalId = ?", sid);
        await Db.ExecuteAsync("DELETE FROM BloqueoHorario WHERE SucursalId = ?", sid);
    }

    private SQLiteAsyncConnection Db =>
        _db ?? throw new InvalidOperationException("Llama a InitAsync() primero.");
}
