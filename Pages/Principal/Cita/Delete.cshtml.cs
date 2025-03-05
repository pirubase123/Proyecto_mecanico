﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using mecanico_plus.Data;
using mecanico_plus.Pages.Backend.accesoDatos;
using mecanico_plus.Pages.Backend.ModeloConDatos;
using Microsoft.AspNetCore.Mvc.Rendering;
using mecanico_plus.Pages.Backend.constantes;
using mecanico_plus.Pages.Backend.logicaNegocio;
using mecanico_plus.Pages.Backend.menus;
using Microsoft.AspNetCore.Authorization;

namespace mecanico_plus.Pages.Principal.Cita
{
    [Authorize]
    public class DeleteModel : PageModel
    {
        private readonly mecanico_plus.Data.local _context;

        public DeleteModel(mecanico_plus.Data.local context)
        {
            _context = context;
        }

        [BindProperty]
        public t009_cita t009_cita { get; set; }

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
                    if (await permisos.usuarioTienePermisoMenu(nombresMenus.PERMISO_CITAS,
                                                               HttpContext.Session.GetString(Costantes.SESION_USUARIO),
                                                               Costantes.PERMISO_ELIMINAR))
                    {
                        if (id == null)
                        {
                            return NotFound();
                        }

                        t009_cita = await _context.t009_cita
                            .Include(t => t.vObjMecanico)
                            .Include(t => t.vObjEmpresa)
                            .Include(t => t.vObjCliente)
                            .Include(t => t.vObjEspecialidad)
                              .Include(t => t.vObjServicio)
                            .FirstOrDefaultAsync(m => m.f009_rowid == id);

                        if (t009_cita == null)
                        {
                            return NotFound();
                        }
                        return Page();
                    }
                    else
                    {
                        // Mostrar mensaje de error
                        TempData["ErrorMessage"] = "No tienes permiso para eliminar citas.";
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
            try
            {
                var cita = _context.t009_cita
                     .Include(t => t.vObjEmpresa)
                      .Include(t => t.vObjMecanico)
                       .Include(t => t.vObjCliente)
                        .Include(t => t.vObjEspecialidad)
                            .Include(t => t.vObjServicio)
                    .FirstOrDefault(p => p.f009_rowid == id);


     //           var paciente = _context.t007_paciente
     //.FirstOrDefault(p => p.f007_rowid == cita.f009_rowid_paciente);

     //           var doctor = _context.t006_doctor
     //.FirstOrDefault(p => p.f006_rowid == cita.f009_rowid_doctor);

                var datosCita = new mecanico_plus.APIs.DatosCitaDTO
                {
                    
                    f009_hora = cita.f009_hora,
                    NombreTipoServicio = cita.vObjServicio.f014_nombre,
                    f009_observacion = "cita.f009_observacion",
                    PacienteCorreo = cita.vObjCliente.f007_correo,
                    PacienteNombre = cita.vObjCliente.f007_nombre + " " + cita.vObjCliente.f007_apellido,
                    DoctorNombre = cita.vObjMecanico.f006_nombre + " " + cita.vObjMecanico.f006_apellido,
                    NombreEspecializacion = cita.vObjEspecialidad.f010_nombre
                };
                // Preparar cliente HTTP para llamar a la APIgenérica

                var apiGenerica = new mecanico_plus.APIs.APIGenerica();
                var response = await apiGenerica.eliminarCitaConCorreo(datosCita);

                
                

                

                if (response is OkObjectResult)
                {
                    if (id == null)
                    {
                        return NotFound();
                    }

                    t009_cita = await _context.t009_cita.FindAsync(id);

                    if (t009_cita != null)
                    {
                        _context.t009_cita.Remove(t009_cita);
                        await _context.SaveChangesAsync();
                    }

                    

                    // llamado t068_doc_enviados_ne
                    t011_historial_medico t011_historial_medico = new t011_historial_medico
                    {
                        f011_ts = DateTime.UtcNow,
                        f011_hora = t009_cita.f009_hora,
                        f011_tipo_cita = t009_cita.vObjServicio.f014_nombre,
                        f011_epecializacion = t009_cita.vObjEspecialidad.f010_nombre,
                        f011_observacion = "t009_cita.f009_observacion",
                        f011_estado = "cancelada",
                        f011_documento_paciente = t009_cita.vObjCliente.f007_id,
                        f011_nombre_paciente = t009_cita.vObjCliente.f007_nombre + " " + t009_cita.vObjCliente.f007_apellido,
                        f011_nombre_doctor = t009_cita.vObjMecanico.f006_nombre + " " + t009_cita.vObjMecanico.f006_apellido,
                        f011_rowid_empresa_o_persona_natural = t009_cita.f009_rowid_empresa_o_persona_natural

                    };

                    HistorialMec HistorialMec = new HistorialMec();
                    await HistorialMec.adicionRegistroHistorialMedico(t011_historial_medico);

                    TempData["SuccessMessage"] = "Cita cancelada y correo enviado correctamente.";
                    return RedirectToPage("./Index");
                }
                else
                {


                    ConsultarItemsForaneos();

                    TempData["ErrorMessage"] = "Error al cancelar la cita, intenta nuevamente.";

                    return Page();
                }
            }
            catch (Exception ex)
            {
                ConsultarItemsForaneos();
                TempData["SuccessMessage"] = "Error al cancelar la cita, intenta nuevamente.";
                return Page();
            }
           
        }
        private void ConsultarItemsForaneos()
        {
            
        }
    }
}
