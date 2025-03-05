﻿using System;
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
using Microsoft.AspNetCore.Authorization;

namespace mecanico_plus.Pages.Principal.Especialidad
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly mecanico_plus.Data.local _context;

public IList<t010_vehiculo> t010_vehiculo { get;set; }

 private readonly DbContextOptions<local> _contextOptions;

        public IndexModel(mecanico_plus.Data.local context, DbContextOptions<local> contextOptions)
        {
            _context = context;
            _contextOptions = contextOptions;
        }


  

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
                    if (await permisos.usuarioTienePermisoMenu(nombresMenus.PERMISO_VEHICULOS,
                                                               HttpContext.Session.GetString(Costantes.SESION_USUARIO),
                                                               Costantes.PERMISO_CONSULTAR))
                    {


  // Obtén la empresa seleccionada
                        int currentEmpresaId = await ObtenerEmpresaSeleccionada();

                        t010_vehiculo = await _context.t010_vehiculo
                 .Include(t => t.vObjEmpresa)
                   .Include(t => t.vObjCliente).Where(t => t.f010_rowid_empresa_o_persona_natural == currentEmpresaId)
                 .ToListAsync();
                        return null;
                    }
                    else
                    {
                        // Mostrar mensaje de error
                        TempData["ErrorMessage"] = "No tienes permiso para consultar especialidades.";
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

        public IActionResult OnPostIrACrear()
        {
            try
            {
                return RedirectToPage("Create");
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
