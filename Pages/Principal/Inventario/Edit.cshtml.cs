﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using mecanico_plus.Data;
using mecanico_plus.Pages.Backend.constantes;
using mecanico_plus.Pages.Backend.logicaNegocio;
using mecanico_plus.Pages.Backend.menus;

namespace mecanico_plus.Pages.Principal.Inventario
{
    public class EditModel : PageModel
    {
        private readonly mecanico_plus.Data.local _context;

        public EditModel(mecanico_plus.Data.local context)
        {
            _context = context;
        }

        [BindProperty]
        public t015_inventario t015_inventario { get; set; } = default!;

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
                    if (await permisos.usuarioTienePermisoMenu(nombresMenus.PERMISO_INVENTARIOS,
                                                               HttpContext.Session.GetString(Costantes.SESION_USUARIO),
                                                               Costantes.PERMISO_EDITAR))
                    {
                        if (id == null)
                        {
                            return NotFound();
                        }
                        // Obtener el ID de la empresa asociada al usuario logueado
                        string sessionUser = HttpContext.Session.GetString("SessionUser");
                        if (string.IsNullOrEmpty(sessionUser))
                        {
                            throw new Exception("Usuario no encontrado en la sesión.");
                        }

                        int empresaId = await (from use in _context.t001_usuario
                                               where use.f001_correo_electronico == sessionUser
                                               select use.f001_rowid_empresa_o_persona_natural).FirstAsync();


                        var inventarioLocal = await _context.t015_inventario.FirstOrDefaultAsync(m => m.f015_rowid == id);
                        if (inventarioLocal == null)
                        {
                            return NotFound();
                        }

                        this.t015_inventario = inventarioLocal;
                        ViewData["f015_rowid_empresa_o_persona_natural"] = new SelectList(_context.t002_empresa_o_persona_natural.Where(e => e.f002_rowid == empresaId), "f002_rowid", "f002_alcance");
                        return Page();

                        
                    }
                    else
                    {
                        // Mostrar mensaje de error
                        TempData["ErrorMessage"] = "No tienes permiso para editar inventarios.";
                        return RedirectToPage("./Index");
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

        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            //if (!ModelState.IsValid)
            //{
            //    return Page();
            //}

            _context.Attach(t015_inventario).State = EntityState.Modified;

            try
            {

                t015_inventario.f015_ts = DateTime.Now;
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!t015_inventarioExists(t015_inventario.f015_rowid))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Index");
        }

        private bool t015_inventarioExists(int id)
        {
            return _context.t015_inventario.Any(e => e.f015_rowid == id);
        }
    }
}
