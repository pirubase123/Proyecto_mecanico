using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using mecanico_plus.Data;
using mecanico_plus.Pages.Backend.constantes;
using mecanico_plus.Pages.Backend.logicaNegocio;
using mecanico_plus.Pages.Backend.menus;
using mecanico_plus.Pages.Backend.Enums;
using Microsoft.AspNetCore.Authorization;

namespace mecanico_plus.Pages.Principal.HistorialMedico
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly mecanico_plus.Data.local _context;
       
private readonly DbContextOptions<local> _contextOptions;

       public IndexModel(mecanico_plus.Data.local context, DbContextOptions<local> contextOptions)
       {
           _context = context;
           _contextOptions = contextOptions;
       }


        public IList<t011_historial_medico> t011_historial_medico { get;set; }
        
        [BindProperty(SupportsGet = true)]
        public string SelectedState { get; set; }

        [BindProperty(SupportsGet = true)]
        public EstadoHistorial Estado { get; set; } = EstadoHistorial.Todos;

        public async Task<IActionResult> OnGetAsync()
        {
            var email = HttpContext.Session.GetString("SessionUser");
            var currentSessionId = HttpContext.Session.GetString("SessionID");

            using (var context = new local(_contextOptions))
            {
                var user = await context.t001_usuario
                    .FirstOrDefaultAsync(u => u.f001_correo_electronico == email);

                if (user == null || user.f001_sesion_id != currentSessionId)
                {
                    TempData["ErrorMessage"] = "La sesión ha caducado o el mismo usuario ingresó en otra sesión en otro navegador. Solo un usuario es permitido por sesión";
                    return RedirectToPage("/Login/Index");
                }

                // Verificar suscripción activa obteniendo el registro
                var suscripcion = await context.t017_gestion_cliente
                    .Where(s => s.f017_rowid_empresa_o_persona_natural == user.f001_rowid_empresa_o_persona_natural)
                    .OrderByDescending(s => s.f017_ts)
                    .FirstOrDefaultAsync();

                // Si no hay registro o la suscripción no está pagada
                if (suscripcion == null || !suscripcion.f017_suscripcion_mensual_pagada)
                {
                    var customMessage = suscripcion?.f017_mensaje_aviso_cliente;
                    TempData["ErrorMessage"] = !string.IsNullOrEmpty(customMessage)
                        ? customMessage
                        : "No tiene una suscripcion activa. Por favor, adquiera un plan para continuar usando el sistema.";
                }
            }
            try
            {
                if (HttpContext.Session.GetString("SessionUser") != null)
                {
                    PermisoDomain permisos = new PermisoDomain();
                    if (await permisos.usuarioTienePermisoMenu(nombresMenus.PERMISO_HISTORIALES,
                                                               HttpContext.Session.GetString(Costantes.SESION_USUARIO),
                                                               Costantes.PERMISO_CONSULTAR))
                    {
                         // Obtén la empresa seleccionada
                        int currentEmpresaId = await ObtenerEmpresaSeleccionada();

                        
                        // Check if user is a patient
                        var userType = HttpContext.Session.GetString("UserType");
                        if (userType == "Cliente")
                        {
                            // Get the current user's email
                            var userEmail = HttpContext.Session.GetString("SessionUser");
                            
                            // Find the user and their associated patient record
                            var user = await _context.t001_usuario
                                .Include(u => u.vObjCliente)
                                .FirstOrDefaultAsync(u => u.f001_correo_electronico == userEmail);

                            if (user?.vObjCliente != null)
                            {
                                // Only show medical history for this patient
                                t011_historial_medico = await _context.t011_historial_medico
                                    .Include(t => t.vObjEmpresa) .Where(t => t.f011_rowid_empresa_o_persona_natural == currentEmpresaId)
                                    .ToListAsync();
                            }
                        }
                        else
                        {
                            // Modified query with proper Include handling
                            var query = _context.t011_historial_medico
                                .Include(t => t.vObjEmpresa).AsQueryable();

                            if (Estado != EstadoHistorial.Todos)
                            {

                                string estadoString = Estado switch
                                {
                                    EstadoHistorial.Asignada => "asignada",
                                  EstadoHistorial.EnProceso => "en proceso",
                                    EstadoHistorial.Diagnostico => "diagnóstico",
                                    EstadoHistorial.Finalizada => "finalizada",
                                    EstadoHistorial.Cancelada => "cancelada",
                                    _ => "todos"
                                };

                                // Evitar StringComparison en LINQ:
                                query = query.Where(h => h.f011_estado.ToLower() == estadoString.ToLower());
                            }

                            t011_historial_medico = await query
                             .Where(t => t.f011_rowid_empresa_o_persona_natural == currentEmpresaId).ToListAsync();
                        }
                        return Page();
                    }
                    else
                    {
                        // Mostrar mensaje de error
                        TempData["ErrorMessage"] = "No tienes permiso para consultar historial medico.";
                        return RedirectToPage("../../Principal/Base/Index");
                    }
                }
                else
                {
                    HttpContext.Session.SetString("ExpiredSession", "true");
                    return RedirectToPage("../../Login/Index");
                }
            }
            catch (Exception ex)
            {
                HttpContext.Session.SetString("ExpiredSession", "true");
                return RedirectToPage("../../Login/Index");
            }
           
        }
 public async Task<int> ObtenerEmpresaSeleccionada()
        {
            try
            {
                // Verifica que el correo electrónico del usuario esté disponible en la sesión
                string sessionUser = HttpContext.Session.GetString("SessionUser");
                if (string.IsNullOrEmpty(sessionUser))
                {
                    throw new Exception("Usuario no encontrado en la sesión.");
                }

                // Obtén el rowId de la empresa asociada al usuario logueado
                int rowIdEmpresaSeleccionada = await (from use in _context.t001_usuario
                                                      where use.f001_correo_electronico == sessionUser
                                                      select use.f001_rowid_empresa_o_persona_natural).FirstAsync();

                return rowIdEmpresaSeleccionada;
            }
            catch (Exception ex)
            {
                // Manejo de excepciones (puedes personalizar el mensaje o hacer un registro de errores)
                throw new Exception("Error al obtener la empresa seleccionada.", ex);
            }
        }
        public async Task<IActionResult> OnPostEnviarCorreo(int? id)
        {
            try
            {
                var registro = _context.t011_historial_medico
                    .FirstOrDefault(p => p.f011_rowid == id);

                var paciente = _context.t007_cliente
                    .FirstOrDefault();

                var datosCita = new mecanico_plus.APIs.DatosCitaEnviarDTO
                {
                    f009_hora = registro.f011_hora,
                    NombreTipoServicio = registro.f011_tipo_cita,
                    f009_observacion = registro.f011_observacion,
                    PacienteCorreo = paciente.f007_correo,
                    PacienteNombre = registro.f011_nombre_paciente,
                    DoctorNombre = registro.f011_nombre_doctor,
                    NombreEspecializacion = registro.f011_epecializacion,
                };

                var apiGenerica = new mecanico_plus.APIs.APIGenerica();
                var response = await apiGenerica.enviarHistoriaConCorreo(datosCita);

                if (response is OkObjectResult)
                {
                    t011_historial_medico = await _context.t011_historial_medico
                        .Include(t => t.vObjEmpresa).ToListAsync();

                    TempData["SuccessMessage"] = "historia clinica enviada correctamente.";
                    return Page();
                }
                else
                {
                    t011_historial_medico = await _context.t011_historial_medico
                        .Include(t => t.vObjEmpresa).ToListAsync();
                    TempData["ErrorMessage"] = "Error al crear la cita, intenta nuevamente.";

                    return Page();
                }
            }
            catch (Exception ex)
            {
                t011_historial_medico = await _context.t011_historial_medico
                    .Include(t => t.vObjEmpresa).ToListAsync();

                TempData["ErrorMessage"] = "Error al crear la cita, intenta nuevamente.";
                return Page();
            }
        }

        public async Task<IActionResult> OnGetCitaDetails(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cita = await _context.t009_cita
                .Include(c => c.vObjMecanico)
                .Include(c => c.vObjCliente)
                .Include(c => c.vObjEspecialidad)
                .FirstOrDefaultAsync(m => m.f009_rowid == id);

            if (cita == null)
            {
                return NotFound();
            }

            return new JsonResult(cita);
        }
    }
}
