using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace mecanico_plus.Data
{
    public class t017_gestion_cliente
    {
        [Key]
        public int f017_rowid {  get; set; }

        [Display(Name = "Fecha creacion")]
        public DateTime f017_ts { get; set; }

        [Display(Name = "Plan basic")]
        public bool f017_plan_basic { get; set; }

        [Display(Name = "Plan estandar")]
        public bool f017_plan_estandar { get; set; }

        [Display(Name = "Plan pro")]
        public bool f017_plan_pro {  get; set; }

        [Display(Name = "Plan eterprise")]
        public bool f017_plan_enterprise { get; set; }

        [Display(Name = "Suscripcion mensual pagada")]
        public bool f017_suscripcion_mensual_pagada { get; set; }


        [Display(Name = "Mensaje aviso suspencion")]
        public string? f017_mensaje_cliente_aviso_suspencion { get; set; }

        [Display(Name = "Mensaje aviso cliente")]
        public string? f017_mensaje_aviso_cliente {  get; set; }

        [Display(Name = "Numero usuarios")]
        public int? f017_numero_usuarios { get; set; }

        [Display(Name = "Empresa")]
        [Required(ErrorMessage = "Debe seleccionar una empresa o persona natural")]
        public int f017_rowid_empresa_o_persona_natural { get; set; }

        [ForeignKey("f017_rowid_empresa_o_persona_natural")]
        [Required(ErrorMessage = "Debe seleccionar una empresa o persona natural")]
        public t002_empresa_o_persona_natural vObjEmpresa { get; set; }
    }
}
