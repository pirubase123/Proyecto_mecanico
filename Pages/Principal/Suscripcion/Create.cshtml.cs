using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using mecanico_plus.Data;
using Microsoft.EntityFrameworkCore;
using mecanico_plus.Pages.Backend.constantes;
using mecanico_plus.Pages.Backend.logicaNegocio;
using mecanico_plus.Pages.Backend.menus;

namespace mecanico_plus.Pages.Principal.Suscripcion
{
    public class CreateModel : PageModel
    {
        private readonly local _context;

        public CreateModel(local context)
        {
            _context = context;
        }

        [BindProperty]
        public t017_gestion_cliente GestionCliente { get; set; } = new t017_gestion_cliente();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                if (HttpContext.Session.GetString("SessionUser") != null)
                {

                    PermisoDomain permisos = new PermisoDomain();
                    if (await permisos.usuarioTienePermisoMenu(nombresMenus.PERMISO_SUSCRIPCIONES,
                                                               HttpContext.Session.GetString(Costantes.SESION_USUARIO),
                                                               Costantes.PERMISO_CREAR))
                    {
                        // Obtener el ID de la empresa asociada al usuario logueado
                        string sessionUser = HttpContext.Session.GetString("SessionUser");
                        if (string.IsNullOrEmpty(sessionUser))
                        {
                            throw new Exception("Usuario no encontrado en la sesión.");
                        }

                        int empresaId = await(from use in _context.t001_usuario
                                              where use.f001_correo_electronico == sessionUser
                                              select use.f001_rowid_empresa_o_persona_natural).FirstAsync();

                        

                        // Inicializar valores por defecto
                        GestionCliente.f017_ts = DateTime.Now;
                        GestionCliente.f017_suscripcion_mensual_pagada = false;

                        // Cargar lista de empresas para el dropdown
                        ViewData["f017_rowid_empresa_o_persona_natural"] = new SelectList(
                            _context.t002_empresa_o_persona_natural.Where(e => e.f002_rowid == empresaId),
                            "f002_rowid",
                            "f002_razon_social"
                        );

                        return Page();
                    }
                    else
                    {

                        // Mostrar mensaje de error
                        TempData["ErrorMessage"] = "No tienes permiso para crear suscripcion.";
                        return RedirectToPage("./Index");
                    }

                }
                else
                {
                    HttpContext.Session.SetString("ExpiredSession", "true");
                    return RedirectToPage("../../Login/Index");
                };
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "No tienes permiso para crear permisos.";
                return RedirectToPage("../../Login/Index");
            }

           
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (GestionCliente.f017_rowid_empresa_o_persona_natural == 0)
            {
                ModelState.AddModelError("GestionCliente.f017_rowid_empresa_o_persona_natural", 
                    "Debe seleccionar una empresa o persona natural");
                CargarEmpresas();
                return Page();
            }

            //if (!ModelState.IsValid)
            //{
            //    CargarEmpresas();
            //    return Page();
            //}

            // Verificar si la empresa ya tiene una suscripción
            var suscripcionExistente = await _context.t017_gestion_cliente
                .AnyAsync(g => g.f017_rowid_empresa_o_persona_natural == GestionCliente.f017_rowid_empresa_o_persona_natural);

            if (suscripcionExistente)
            {
                ModelState.AddModelError("GestionCliente.f017_rowid_empresa_o_persona_natural", 
                    "Esta empresa ya tiene una suscripción activa.");
                CargarEmpresas();
                return Page();
            }

            // Asegurar que la fecha de creación sea la actual
            GestionCliente.f017_ts = DateTime.Now;

            // Validar que al menos un plan esté seleccionado
            if (!GestionCliente.f017_plan_basic && 
                !GestionCliente.f017_plan_estandar && 
                !GestionCliente.f017_plan_pro && 
                !GestionCliente.f017_plan_enterprise)
            {
                ModelState.AddModelError(string.Empty, "Debe seleccionar al menos un plan");
                CargarEmpresas();
                return Page();
            }

            // Validar número de usuarios según el plan
            if (!ValidarNumeroUsuarios())
            {
                string mensaje = ObtenerMensajeValidacionUsuarios();
                ModelState.AddModelError("GestionCliente.f017_numero_usuarios", mensaje);
                CargarEmpresas();
                return Page();
            }

            _context.t017_gestion_cliente.Add(GestionCliente);
            await _context.SaveChangesAsync();

            return RedirectToPage("./List");
        }

        private bool ValidarNumeroUsuarios()
        {
            if (!GestionCliente.f017_numero_usuarios.HasValue)
                return false;

            if (GestionCliente.f017_plan_basic && GestionCliente.f017_numero_usuarios > 2)
                return false;

            if (GestionCliente.f017_plan_estandar && GestionCliente.f017_numero_usuarios > 5)
                return false;

            if (GestionCliente.f017_plan_pro && GestionCliente.f017_numero_usuarios > 10)
                return false;

            return true;
        }

        private string ObtenerMensajeValidacionUsuarios()
        {
            if (GestionCliente.f017_plan_basic && GestionCliente.f017_numero_usuarios > 2)
                return "El plan Básico permite máximo 2 usuarios.";
            if (GestionCliente.f017_plan_estandar && GestionCliente.f017_numero_usuarios > 5)
                return "El plan Estándar permite máximo 5 usuarios.";
            if (GestionCliente.f017_plan_pro && GestionCliente.f017_numero_usuarios > 10)
                return "El plan Pro permite máximo 10 usuarios.";
            return "El número de usuarios no es válido para el plan seleccionado.";
        }

        private void CargarEmpresas()
        {
            ViewData["f017_rowid_empresa_o_persona_natural"] = new SelectList(
                _context.t002_empresa_o_persona_natural,
                "f002_rowid",
                "f002_razon_social"
            );
        }
    }
}
