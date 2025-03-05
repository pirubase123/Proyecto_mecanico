using mecanico_plus.Pages.Backend.ModeloConDatos;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace mecanico_plus.Data
{
    public class t011_historial_medico
    {
        [Key]
        public int f011_rowid { get; set; }

        [Display(Name = "Fecha Creación")]
        public DateTime f011_ts { get; set; }

        [Display(Name = "Hora")]
        [Required(ErrorMessage = "La hora es requerida.")]
        public DateTime f011_hora
        {
            get;
            set;
        }

        private DateTime _hora;

        [Display(Name = "Servicio")]
        [StringLength(maximumLength: 80)]
        public string f011_tipo_cita { get; set; }

        //[NotMapped]
        //public string NombreTipoServicio => TipoCita.ObtenerNombrePorId(f009_tipo_cita);

        [Display(Name = "Vehiculo")]
        [StringLength(maximumLength: 80)]
        public string f011_epecializacion { get; set; }

        //[NotMapped]
        //public string NombreEspecializacion => Especializacion.ObtenerNombrePorId(f009_epecializacion);

        [Display(Name = "Observacion")]
        [StringLength(maximumLength: 500)]
        public string? f011_observacion { get; set; }

        [Display(Name = "Estado")]
        [StringLength(maximumLength: 255)]
        public string f011_estado { get; set; }

        [Display(Name = "Nombre cliente")]
        [StringLength(maximumLength: 255)]
        public string f011_nombre_paciente { get; set; }

        [Display(Name = "Documento cliente")]
        public int f011_documento_paciente { get; set; }

        [Display(Name = "Nombre mecanico")]
        [StringLength(maximumLength: 255)]
        public string f011_nombre_doctor { get; set; }

    

       

        [Display(Name = "Empresa")]
        public int f011_rowid_empresa_o_persona_natural { get; set; }


        [Display(Name = "Empresa")]
        [ForeignKey("f011_rowid_empresa_o_persona_natural")]
        public t002_empresa_o_persona_natural vObjEmpresa { get; set; }


        [Display(Name = "Documentos")]
        public virtual ICollection<t013_documento> Documentos { get; set; }

        public t011_historial_medico()
        {
            Documentos = new List<t013_documento>();
        }

    }
}
