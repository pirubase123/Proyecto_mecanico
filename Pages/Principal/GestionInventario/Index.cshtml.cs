using mecanico_plus.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using mecanico_plus.Pages.Backend.logicaNegocio;
using mecanico_plus.Pages.Backend.menus;
using mecanico_plus.Pages.Backend.constantes;

namespace mecanico_plus.Pages.Principal.GestionInventario
{
    public class IndexModel : PageModel
    {
    
        public List<t015_inventario> Inventario { get; set; }
        public List<string> LowStockMessage { get; set; } = new List<string>();

 private readonly mecanico_plus.Data.local _context;
       
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
                    if (await permisos.usuarioTienePermisoMenu(nombresMenus.PERMISO_GESTION_INVENTARIOS,
                                                               HttpContext.Session.GetString(Costantes.SESION_USUARIO),
                                                               Costantes.PERMISO_CONSULTAR))
                    {
                        // Obtén la empresa seleccionada
                        int currentEmpresaId = await ObtenerEmpresaSeleccionada();



                    

            Inventario = await _context.t015_inventario
            .Where(t => t.f015_rowid_empresa_o_persona_natural == currentEmpresaId)
            .ToListAsync();
            CheckLowStock();
                        return null;
                    }
                    else
                    {
                        // Mostrar mensaje de error
                        TempData["ErrorMessage"] = "No tienes permiso para consultar clientes.";
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
        public async Task OnPostAsync()
        {
            Inventario = await _context.t015_inventario.ToListAsync();

            foreach (var item in Inventario)
            {
                var entrada = Request.Form[$"entrada_{item.f015_rowid}"];
                var salida = Request.Form[$"salida_{item.f015_rowid}"];

                if (int.TryParse(entrada, out int cantEntrar) && cantEntrar >= 0)
                {
                    item.f015_cantidad_disponible += cantEntrar;
                }
                if (int.TryParse(salida, out int cantSalir) && cantSalir >= 0)
                {
                    item.f015_cantidad_disponible = item.f015_cantidad_disponible - cantSalir < 0
                        ? 0
                        : item.f015_cantidad_disponible - cantSalir;
                }
            }

            await _context.SaveChangesAsync();
            CheckLowStock();
        }

        private void CheckLowStock()
        {
            // Ejemplo: alerta si queda menos de 5
            foreach (var item in Inventario.Where(i => i.f015_cantidad_disponible < 5))
            {
                LowStockMessage.Add($"{item.f015_nombre} casi agotado (quedan {item.f015_cantidad_disponible}).");
            }
        }
    }
}
