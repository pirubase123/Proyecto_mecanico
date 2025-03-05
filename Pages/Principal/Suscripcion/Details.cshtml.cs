using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using mecanico_plus.Data;
using mecanico_plus.Pages.Backend.constantes;
using mecanico_plus.Pages.Backend.logicaNegocio;
using mecanico_plus.Pages.Backend.menus;

namespace mecanico_plus.Pages.Principal.Suscripcion
{
    public class DetailsModel : PageModel
    {
        private readonly local _context;

        public DetailsModel(local context)
        {
            _context = context;
        }

        public t017_gestion_cliente GestionCliente { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            try
            {
                if (HttpContext.Session.GetString("SessionUser") != null)
                {
                    if (id == null)
                    {
                        return NotFound();
                    }


                    PermisoDomain permisos = new PermisoDomain();
                    if (await permisos.usuarioTienePermisoMenu(nombresMenus.PERMISO_SUSCRIPCIONES,
                                                               HttpContext.Session.GetString(Costantes.SESION_USUARIO),
                                                               Costantes.PERMISO_DETALLE))
                    {
                        if (id == null)
                        {
                            return NotFound();
                        }

                        GestionCliente = await _context.t017_gestion_cliente
                            .Include(g => g.vObjEmpresa)
                            .FirstOrDefaultAsync(m => m.f017_rowid == id);

                        if (GestionCliente == null)
                        {
                            return NotFound();
                        }
                        return Page();
                    }
                    else
                    {
                        // Mostrar mensaje de error
                        TempData["ErrorMessage"] = "No tienes permiso para ver detalles de especialidades.";
                        return RedirectToPage("./Index");
                    }


                }
                else
                {
                    HttpContext.Session.SetString("ExpiredSession", "true");
                    return RedirectToPage("../../Login/Login");
                };
            }
            catch (Exception ex)
            {
                HttpContext.Session.SetString("ExpiredSession", "true");
                return RedirectToPage("../../Login/Login");
            }
           
        }
    }
}
