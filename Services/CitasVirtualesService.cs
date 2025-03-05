using mecanico_plus.Data;
using Microsoft.EntityFrameworkCore;
using mecanico_plus.APIs; // Asegúrate de tener esta línea para usar TokenGenerator

public class CitasVirtualesService : IHostedService, IDisposable
{
    private Timer _timer;
    private readonly IServiceScopeFactory _scopeFactory;

    public CitasVirtualesService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(CheckCitasVirtuales, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
        return Task.CompletedTask;
    }

    private async void CheckCitasVirtuales(object state)
    {
        try 
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<local>();
            var apiGenerica = new mecanico_plus.APIs.APIGenerica(); // Cambiado para crear una nueva instancia

            var now = DateTime.Now;
            var citasProximas = await context.t009_cita
                .Include(c => c.vObjCliente)
                .Include(c => c.vObjMecanico)
                .Include(c => c.vObjEspecialidad)
                .Where(c => c.f009_rowid_servicio == 2 // Virtual
                        && c.f009_estado == "Aprobada"
                        && c.f009_hora > now
                        && c.f009_hora <= now.AddMinutes(15)
                        && !context.t012_token.Any(t => t.f012_rowid_cita == c.f009_rowid))
                .ToListAsync();

            foreach (var cita in citasProximas)
            {
                // Generar token y sala virtual
                var token = new t012_token
                {
                    f012_ts = DateTime.Now,
                    f012_token = TokenGenerator.GenerateTokenId(), // Usar TokenGenerator
                    f012_rowid_empresa_o_persona_natural = cita.f009_rowid_empresa_o_persona_natural,
                    f012_expiracion = cita.f009_hora.AddHours(1),
                    f012_rowid_cita = cita.f009_rowid // Asegurarnos de que esto se establece
                };

                await context.t012_token.AddAsync(token);
                await context.SaveChangesAsync();

                // Enviar correo con el token y enlace directos
                var enlaceDirecto = $"https://{GetHostUrl(context)}/Principal/CitaVirtual/Index?token={token.f012_token}";
                
                var datosCita = new mecanico_plus.APIs.DatosCitaDTO
                {
                    f009_hora = cita.f009_hora,
                    NombreTipoServicio = "Virtual",
                    Token = token.f012_token, // Asegurarnos de que el token se está asignando
                    PacienteCorreo = cita.vObjCliente.f007_correo,
                    PacienteNombre = $"{cita.vObjCliente.f007_nombre} {cita.vObjCliente.f007_apellido}",
                    DoctorNombre = $"{cita.vObjMecanico.f006_nombre} {cita.vObjMecanico.f006_apellido}",
                    NombreEspecializacion = cita.vObjEspecialidad.f010_nombre,
                    EnlaceSala = enlaceDirecto
                };

                await apiGenerica.EnviarNotificacionCitaVirtual(datosCita);
            }
        }
        catch (Exception ex)
        {
            // Log the error
            Console.Error.WriteLine($"Error en CheckCitasVirtuales: {ex.Message}");
        }
    }

    private string GetHostUrl(local context)
    {
        // Modificar para usar la URL base configurada en la aplicación
        return "localhost:7026"; // Ajusta esto según tu configuración
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
