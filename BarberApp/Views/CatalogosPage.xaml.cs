using BarberApp.Helpers;
using BarberApp.Models;
using BarberApp.Services;
using System.Collections.ObjectModel;

namespace BarberApp.Views;

public partial class CatalogosPage : ContentPage
{
    private readonly DatabaseService _db;
    private readonly bool _esAdmin;

    private enum Tab { Servicios, Clientes, Barberos, Productos }

    private Tab _tabActual = Tab.Servicios;
    private int _editandoId;
    private object? _editandoEntidad;
    private string _textoBusqueda = "";

    public ObservableCollection<ItemCatalogo> Items { get; } = new();

    public CatalogosPage(DatabaseService databaseService)
    {
        InitializeComponent();
        _db = databaseService;
        _esAdmin = Preferences.Get(PreferenceKeys.UsuarioLogueadoRol, "") == Roles.Admin;
        ListaCatalogo.ItemsSource = Items;
        BtnTabBarberos.IsVisible = _esAdmin;
        BtnTabProductos.IsVisible = _esAdmin;

        if (!_esAdmin)
            _tabActual = Tab.Clientes;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!await SessionGuard.VerificarAsync(this)) return;
#if WINDOWS
        MauiProgram.ResizarVentanaActual(900, 750);
#endif
        if (!_esAdmin)
        {
            BtnTabServicios.IsVisible = false;
            BtnTabProductos.IsVisible = false;
            ActualizarTabs();
        }
        await CargarListaAsync();
    }

    private async Task CargarListaAsync()
    {
        Items.Clear();

        switch (_tabActual)
        {
            case Tab.Servicios:
                var servicios = _esAdmin
                    ? await _db.GetAllServiciosAsync()
                    : await _db.GetServiciosAsync();
                foreach (var s in servicios.Where(s => FiltrarTexto(s.Nombre)))
                {
                    Items.Add(new ItemCatalogo
                    {
                        Id = s.Id,
                        Linea1 = s.Nombre + (s.Activo ? "" : " (inactivo)"),
                        Linea2 = $"${s.Precio:N0} · {s.DuracionMinutos} min",
                        Entidad = s
                    });
                }
                break;

            case Tab.Clientes:
                var clientes = string.IsNullOrWhiteSpace(_textoBusqueda)
                    ? await _db.GetClientesAsync()
                    : await _db.BuscarClientesAsync(_textoBusqueda);
                foreach (var c in clientes)
                {
                    Items.Add(new ItemCatalogo
                    {
                        Id = c.Id,
                        Linea1 = c.Nombre,
                        Linea2 = string.IsNullOrWhiteSpace(c.Telefono) ? "Sin teléfono" : c.Telefono,
                        Entidad = c
                    });
                }
                break;

            case Tab.Barberos:
                foreach (var b in await _db.GetAllBarberosAsync())
                {
                    if (!FiltrarTexto(b.Nombre) && !FiltrarTexto(b.Usuario)) continue;
                    Items.Add(new ItemCatalogo
                    {
                        Id = b.Id,
                        Linea1 = b.Nombre,
                        Linea2 = $"@{b.Usuario} · {b.ComisionPorcentaje:N0}% · {(b.Activo ? b.Rol : b.Rol + " · inactivo")}",
                        Entidad = b
                    });
                }
                break;

            case Tab.Productos:
                foreach (var p in await _db.GetAllProductosAsync())
                {
                    if (!FiltrarTexto(p.Nombre)) continue;
                    Items.Add(new ItemCatalogo
                    {
                        Id = p.Id,
                        Linea1 = p.Nombre + (p.Activo ? "" : " (inactivo)"),
                        Linea2 = $"${p.Precio:N0} · Stock: {p.Stock}",
                        Entidad = p
                    });
                }
                break;
        }
    }

    private bool FiltrarTexto(string texto) =>
        string.IsNullOrWhiteSpace(_textoBusqueda) ||
        texto.Contains(_textoBusqueda, StringComparison.OrdinalIgnoreCase);

    private void ActualizarTabs()
    {
        var activeBg = ThemeService.GetColor("CardBackground");
        var inactiveBg = ThemeService.GetColor("CardMuted");
        var activeText = ThemeService.GetColor("TextPrimary");
        var inactiveText = ThemeService.GetColor("TextSecondary");

        BtnTabServicios.BackgroundColor = _tabActual == Tab.Servicios ? activeBg : inactiveBg;
        BtnTabServicios.TextColor = _tabActual == Tab.Servicios ? activeText : inactiveText;
        BtnTabClientes.BackgroundColor = _tabActual == Tab.Clientes ? activeBg : inactiveBg;
        BtnTabClientes.TextColor = _tabActual == Tab.Clientes ? activeText : inactiveText;
        BtnTabBarberos.BackgroundColor = _tabActual == Tab.Barberos ? activeBg : inactiveBg;
        BtnTabBarberos.TextColor = _tabActual == Tab.Barberos ? activeText : inactiveText;
        BtnTabProductos.BackgroundColor = _tabActual == Tab.Productos ? activeBg : inactiveBg;
        BtnTabProductos.TextColor = _tabActual == Tab.Productos ? activeText : inactiveText;
    }

    private async void OnBuscarTextChanged(object? sender, TextChangedEventArgs e)
    {
        _textoBusqueda = e.NewTextValue ?? "";
        await CargarListaAsync();
    }

    private async void OnTabServiciosClicked(object? sender, EventArgs e)
    {
        if (!_esAdmin) return;
        _tabActual = Tab.Servicios;
        ActualizarTabs();
        OcultarFormulario();
        await CargarListaAsync();
    }

    private async void OnTabClientesClicked(object? sender, EventArgs e)
    {
        _tabActual = Tab.Clientes;
        ActualizarTabs();
        OcultarFormulario();
        await CargarListaAsync();
    }

    private async void OnTabBarberosClicked(object? sender, EventArgs e)
    {
        if (!_esAdmin) return;
        _tabActual = Tab.Barberos;
        ActualizarTabs();
        OcultarFormulario();
        await CargarListaAsync();
    }

    private void ConfigurarFormulario(string titulo, bool mostrarEliminar, params (string placeholder, bool visible, bool isPassword)[] campos)
    {
        LblFormTitulo.Text = titulo;
        BtnEliminar.IsVisible = mostrarEliminar && _editandoId > 0;
        Entry[] entries = [EntryCampo1, EntryCampo2, EntryCampo3, EntryCampo4];
        for (int i = 0; i < entries.Length; i++)
        {
            if (i < campos.Length && campos[i].visible)
            {
                entries[i].IsVisible = true;
                entries[i].Placeholder = campos[i].placeholder;
                entries[i].IsPassword = campos[i].isPassword;
                entries[i].Text = string.Empty;
            }
            else
            {
                entries[i].IsVisible = false;
                entries[i].Text = string.Empty;
            }
        }
        PanelFormulario.IsVisible = true;
    }

    private void OcultarFormulario()
    {
        PanelFormulario.IsVisible = false;
        _editandoId = 0;
        _editandoEntidad = null;
        BtnEliminar.IsVisible = false;
    }

    private void OnAgregarClicked(object? sender, EventArgs e)
    {
        if (_tabActual == Tab.Servicios && !_esAdmin)
        {
            DisplayAlert("Acceso", "Solo el administrador puede crear servicios.", "OK");
            return;
        }
        if (_tabActual == Tab.Productos && !_esAdmin) return;
        if (_tabActual == Tab.Barberos && !_esAdmin) return;

        _editandoId = 0;
        _editandoEntidad = null;

        switch (_tabActual)
        {
            case Tab.Servicios:
                ConfigurarFormulario("Nuevo servicio", false,
                    ("Nombre del servicio", true, false),
                    ("Precio", true, false),
                    ("Duración (minutos)", true, false));
                EntryCampo2.Keyboard = Keyboard.Numeric;
                EntryCampo3.Keyboard = Keyboard.Numeric;
                break;
            case Tab.Clientes:
                ConfigurarFormulario("Nuevo cliente", false,
                    ("Nombre completo", true, false),
                    ("Teléfono (10 dígitos)", true, false),
                    ("Notas (opcional)", true, false));
                EntryCampo2.Keyboard = Keyboard.Telephone;
                break;
            case Tab.Barberos:
                ConfigurarFormulario("Nuevo personal", false,
                    ("Nombre completo", true, false),
                    ("Usuario (sin espacios)", true, false),
                    ("Contraseña", true, true),
                    ("Rol: admin o barbero", true, false));
                EntryCampo4.Text = Roles.Barbero;
                break;
            case Tab.Productos:
                ConfigurarFormulario("Nuevo producto", false,
                    ("Nombre", true, false),
                    ("Precio", true, false),
                    ("Stock inicial", true, false));
                EntryCampo2.Keyboard = Keyboard.Numeric;
                EntryCampo3.Keyboard = Keyboard.Numeric;
                break;
        }
    }

    private async void OnTabProductosClicked(object? sender, EventArgs e)
    {
        if (!_esAdmin) return;
        _tabActual = Tab.Productos;
        ActualizarTabs();
        OcultarFormulario();
        await CargarListaAsync();
    }

    private async void OnItemSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not ItemCatalogo item) return;
        if (_tabActual == Tab.Servicios && !_esAdmin) return;

        ListaCatalogo.SelectedItem = null;

        if (_tabActual == Tab.Clientes && item.Entidad is Cliente clienteHist)
        {
            var accion = await DisplayActionSheet(clienteHist.Nombre, "Cerrar", null, "Ver historial", "Editar");
            if (accion == "Ver historial") { await MostrarHistorialAsync(clienteHist.Id); return; }
            if (accion != "Editar") return;
        }
        else if (_tabActual == Tab.Productos && item.Entidad is Producto prodVenta && prodVenta.Activo && prodVenta.Stock > 0)
        {
            var accion = await DisplayActionSheet(prodVenta.Nombre, "Cerrar", null, "Vender", "Editar");
            if (accion == "Vender") { await VenderProductoAsync(prodVenta); return; }
            if (accion != "Editar") return;
        }

        _editandoId = item.Id;
        _editandoEntidad = item.Entidad;
        AbrirFormularioEdicion(item);
    }

    private async Task MostrarHistorialAsync(int clienteId)
    {
        var h = await _db.GetHistorialClienteAsync(clienteId);
        var fav = h.ServiciosFavoritos.Count > 0
            ? string.Join(", ", h.ServiciosFavoritos.Select(f => $"{f.Servicio} ({f.Veces})"))
            : "—";
        var citas = h.CitasAnteriores.Count > 0
            ? string.Join("\n", h.CitasAnteriores.Take(8).Select(c => $"• {c.Fecha:dd/MM} {c.Hora} {c.NombreServicio} [{c.Estado}]"))
            : "Sin citas";
        await DisplayAlert("Historial del cliente",
            $"Total citas: {h.TotalCitas}\nTotal gastado: ${h.TotalGastado:N0}\n\nFavoritos: {fav}\n\n{citas}", "OK");
    }

    private async Task VenderProductoAsync(Producto producto)
    {
        var cantStr = await DisplayPromptAsync("Venta", "Cantidad:", "OK", "Cancelar", keyboard: Keyboard.Numeric, initialValue: "1");
        if (!int.TryParse(cantStr, out var cant) || cant <= 0) return;
        var tipo = await DisplayActionSheet("Pago", "Cancelar", null, MetodosPago.Todos);
        if (tipo == null || tipo == "Cancelar") return;
        try
        {
            var vendedor = Preferences.Get(PreferenceKeys.UsuarioLogueadoNombre, "Admin");
            await _db.RegistrarVentaProductoAsync(producto, cant, tipo, vendedor);
            await DisplayAlert("Venta", $"{producto.Nombre} x{cant} registrada.", "OK");
            await CargarListaAsync();
        }
        catch (Exception ex) { await DisplayAlert("Error", ex.Message, "OK"); }
    }

    private void AbrirFormularioEdicion(ItemCatalogo item)
    {
        switch (_tabActual)
        {
            case Tab.Servicios when item.Entidad is Servicio s:
                ConfigurarFormulario("Editar servicio", _esAdmin,
                    ("Nombre del servicio", true, false),
                    ("Precio", true, false),
                    ("Duración (minutos)", true, false));
                EntryCampo1.Text = s.Nombre;
                EntryCampo2.Text = s.Precio.ToString("0");
                EntryCampo3.Text = s.DuracionMinutos.ToString();
                break;

            case Tab.Clientes when item.Entidad is Cliente c:
                ConfigurarFormulario("Editar cliente", true,
                    ("Nombre completo", true, false),
                    ("Teléfono (10 dígitos)", true, false),
                    ("Notas (opcional)", true, false));
                EntryCampo1.Text = c.Nombre;
                EntryCampo2.Text = c.Telefono;
                EntryCampo3.Text = c.Notas;
                break;

            case Tab.Barberos when item.Entidad is Barbero b:
                ConfigurarFormulario("Editar personal", _esAdmin,
                    ("Nombre completo", true, false),
                    ("Usuario (sin espacios)", true, false),
                    ("Comisión %", true, false),
                    ("Rol: admin o barbero", true, false));
                EntryCampo1.Text = b.Nombre;
                EntryCampo2.Text = b.Usuario;
                EntryCampo3.Text = b.ComisionPorcentaje.ToString("0");
                EntryCampo4.Text = b.Rol;
                EntryCampo3.IsPassword = false;
                EntryCampo3.Keyboard = Keyboard.Numeric;
                break;

            case Tab.Productos when item.Entidad is Producto p:
                ConfigurarFormulario("Editar producto", _esAdmin,
                    ("Nombre", true, false),
                    ("Precio", true, false),
                    ("Stock", true, false));
                EntryCampo1.Text = p.Nombre;
                EntryCampo2.Text = p.Precio.ToString("0");
                EntryCampo3.Text = p.Stock.ToString();
                EntryCampo2.Keyboard = Keyboard.Numeric;
                EntryCampo3.Keyboard = Keyboard.Numeric;
                break;
        }
    }

    private async void OnGuardarClicked(object? sender, EventArgs e)
    {
        try
        {
            switch (_tabActual)
            {
                case Tab.Servicios: await GuardarServicioAsync(); break;
                case Tab.Clientes: await GuardarClienteAsync(); break;
                case Tab.Barberos: await GuardarBarberoAsync(); break;
                case Tab.Productos: await GuardarProductoAsync(); break;
            }
            OcultarFormulario();
            await CargarListaAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async Task GuardarServicioAsync()
    {
        var nombre = EntryCampo1.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(nombre))
            throw new InvalidOperationException("El nombre es obligatorio");

        if (!ValidationHelper.EsPrecioValido(EntryCampo2.Text ?? "", out var precio))
            throw new InvalidOperationException("Precio inválido");

        if (!int.TryParse(EntryCampo3.Text, out var duracion) || duracion <= 0)
            duracion = 30;

        var servicio = _editandoEntidad as Servicio ?? new Servicio { Activo = true };
        servicio.Nombre = nombre;
        servicio.Precio = precio;
        servicio.DuracionMinutos = duracion;
        servicio.Activo = true;
        await _db.SaveServicioAsync(servicio);
    }

    private async Task GuardarClienteAsync()
    {
        var nombre = EntryCampo1.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(nombre))
            throw new InvalidOperationException("El nombre es obligatorio");

        var telefono = EntryCampo2.Text?.Trim() ?? "";
        if (!ValidationHelper.EsTelefonoValido(telefono))
            throw new InvalidOperationException("Teléfono inválido (10-15 dígitos)");

        var cliente = _editandoEntidad as Cliente ?? new Cliente();
        cliente.Nombre = nombre;
        cliente.Telefono = ValidationHelper.NormalizarTelefono(telefono);
        cliente.Notas = EntryCampo3.Text?.Trim() ?? "";
        await _db.SaveClienteAsync(cliente);
    }

    private async Task GuardarBarberoAsync()
    {
        var nombre = EntryCampo1.Text?.Trim() ?? "";
        var usuario = EntryCampo2.Text?.Trim() ?? "";
        var password = EntryCampo3.Text ?? "";
        var rol = EntryCampo4.Text?.Trim().ToLower() ?? Roles.Barbero;
        decimal comision = 50;

        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(usuario))
            throw new InvalidOperationException("Nombre y usuario son obligatorios");

        if (_editandoId > 0 && _editandoEntidad is Barbero bb)
        {
            if (!ValidationHelper.EsPorcentajeValido(EntryCampo3.Text ?? "50", out comision))
                comision = bb.ComisionPorcentaje;
        }

        if (usuario.Contains(' '))
            throw new InvalidOperationException("El usuario no puede tener espacios");

        if (rol != Roles.Admin && rol != Roles.Barbero)
            throw new InvalidOperationException("El rol debe ser admin o barbero");

        if (await _db.UsuarioExisteAsync(usuario, _editandoId))
            throw new InvalidOperationException("Ese usuario ya existe");

        var barbero = _editandoEntidad as Barbero ?? new Barbero { Activo = true };
        if (_editandoId == 0 && string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("La contraseña es obligatoria para un barbero nuevo");

        barbero.Nombre = nombre;
        barbero.Usuario = usuario;
        barbero.Rol = rol;
        barbero.ComisionPorcentaje = comision;
        barbero.Activo = true;
        if (_editandoId == 0)
        {
            if (string.IsNullOrWhiteSpace(EntryCampo3.Text))
                throw new InvalidOperationException("La contraseña es obligatoria");
            barbero.PasswordHash = PasswordHelper.Hash(EntryCampo3.Text);
        }
        else
        {
            var passField = await DisplayPromptAsync("Contraseña", "Nueva contraseña (vacío = sin cambio):", "OK", "Cancelar");
            if (!string.IsNullOrWhiteSpace(passField))
                barbero.PasswordHash = PasswordHelper.Hash(passField);
        }

        await _db.SaveBarberoAsync(barbero);
    }

    private async Task GuardarProductoAsync()
    {
        var nombre = EntryCampo1.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(nombre))
            throw new InvalidOperationException("El nombre es obligatorio");
        if (!ValidationHelper.EsPrecioValido(EntryCampo2.Text ?? "", out var precio))
            throw new InvalidOperationException("Precio inválido");
        if (!int.TryParse(EntryCampo3.Text, out var stock) || stock < 0)
            stock = 0;

        var producto = _editandoEntidad as Producto ?? new Producto { Activo = true };
        producto.Nombre = nombre;
        producto.Precio = precio;
        producto.Stock = stock;
        producto.Activo = true;
        await _db.SaveProductoAsync(producto);
    }

    private async void OnEliminarClicked(object? sender, EventArgs e)
    {
        var confirmar = await DisplayAlert("Confirmar", "¿Eliminar o desactivar este registro?", "Sí", "No");
        if (!confirmar) return;

        try
        {
            switch (_tabActual)
            {
                case Tab.Servicios when _editandoEntidad is Servicio s:
                    await _db.DeleteServicioAsync(s);
                    break;
                case Tab.Clientes when _editandoEntidad is Cliente c:
                    var futuras = await _db.ContarCitasFuturasClienteAsync(c.Id);
                    if (futuras > 0)
                        throw new InvalidOperationException($"El cliente tiene {futuras} cita(s) pendiente(s).");
                    await _db.DeleteClienteAsync(c);
                    break;
                case Tab.Barberos when _editandoEntidad is Barbero b:
                    await _db.DesactivarBarberoAsync(b);
                    break;
                case Tab.Productos when _editandoEntidad is Producto p:
                    p.Activo = false;
                    await _db.SaveProductoAsync(p);
                    break;
            }
            OcultarFormulario();
            await CargarListaAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private void OnCancelarFormClicked(object? sender, EventArgs e) => OcultarFormulario();

    private async void OnVolverClicked(object? sender, EventArgs e) =>
        await Navigation.PopAsync();

    public class ItemCatalogo
    {
        public int Id { get; set; }
        public string Linea1 { get; set; } = "";
        public string Linea2 { get; set; } = "";
        public object? Entidad { get; set; }
    }
}
