using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using mecanico_plus.Data;
using mecanico_plus.Pages.Backend.constantes;
using mecanico_plus.Pages.Backend.logicaNegocio;
using mecanico_plus.Pages.Backend.menus;

namespace mecanico_plus.Pages.Principal.Suscripcion
{
    public class DeleteModel : PageModel
    {
        private readonly local _context;

        public DeleteModel(local context)
        {
            _context = context;
        }

        [BindProperty]
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
                                                               Costantes.PERMISO_ELIMINAR))
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
                        TempData["ErrorMessage"] = "No tienes permiso para eliminar suscripciones.";
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
                HttpContext.Session.SetString("ExpiredSession", "true");
                return RedirectToPage("../../Login/Index");
            }


           
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            GestionCliente = await _context.t017_gestion_cliente.FindAsync(id);

            if (GestionCliente != null)
            {
                _context.t017_gestion_cliente.Remove(GestionCliente);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./List");
        }
    }
}
